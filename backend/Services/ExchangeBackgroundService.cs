using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SkillForge.Api.Configuration;
using SkillForge.Api.Data;
using SkillForge.Api.HealthChecks;
using SkillForge.Api.Models;

namespace SkillForge.Api.Services
{
    public class ExchangeBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ExchangeBackgroundService> _logger;
        private readonly ExchangeBackgroundServiceOptions _options;

        public ExchangeBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<ExchangeBackgroundService> logger,
            IOptions<ExchangeBackgroundServiceOptions> options)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _options = options.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_options.Enabled)
            {
                _logger.LogInformation("Exchange Background Service is disabled via configuration.");
                return;
            }

            _logger.LogInformation($"Exchange Background Service is starting. Check interval: {_options.CheckIntervalMinutes} minutes, Grace period: {_options.GracePeriodHours} hours.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessPastExchangesAsync(stoppingToken);
                    ExchangeBackgroundServiceHealthCheck.ReportSuccess();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while processing past exchanges.");
                    ExchangeBackgroundServiceHealthCheck.ReportError(ex.Message);
                }

                await Task.Delay(TimeSpan.FromMinutes(_options.CheckIntervalMinutes), stoppingToken);
            }

            _logger.LogInformation("Exchange Background Service is stopping.");
        }

        private async Task ProcessPastExchangesAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            try
            {
                // Check if database tables exist by attempting to connect and verify schema
                var canConnect = await context.Database.CanConnectAsync(cancellationToken);
                if (!canConnect)
                {
                    _logger.LogDebug("Database connection not available, skipping background processing cycle.");
                    return;
                }

                var now = DateTime.UtcNow;
                
                // Find all accepted exchanges that should have completed
                var pastExchanges = await context.SkillExchanges
                    .Include(e => e.Offerer)
                    .Include(e => e.Learner)
                    .Include(e => e.Skill)
                    .Where(e => e.Status == ExchangeStatus.Accepted)
                    .Where(e => e.ScheduledAt.AddHours(e.Duration).AddHours(_options.GracePeriodHours) < now)
                    .Take(_options.BatchSize)
                    .ToListAsync(cancellationToken);

                if (!pastExchanges.Any())
                {
                    _logger.LogDebug("No past exchanges found to auto-complete.");
                    return;
                }

                _logger.LogInformation($"Found {pastExchanges.Count} exchanges to auto-complete.");

                foreach (var exchange in pastExchanges)
                {
                    try
                    {
                        await AutoCompleteExchangeAsync(context, exchange, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Failed to auto-complete exchange {exchange.Id}");
                    }
                }
            }
            catch (Exception ex) when (IsTableMissingException(ex))
            {
                _logger.LogDebug("Database tables not yet created, skipping background processing cycle.");
                return;
            }
        }

        private static bool IsTableMissingException(Exception ex)
        {
            // Handle different database providers' "table doesn't exist" messages
            var message = ex.Message?.ToLowerInvariant() ?? string.Empty;
            
            return message.Contains("does not exist") || 
                   message.Contains("doesn't exist") ||
                   message.Contains("no such table") ||
                   message.Contains("invalid object name") ||
                   ex.InnerException != null && IsTableMissingException(ex.InnerException);
        }


        private async Task AutoCompleteExchangeAsync(
            ApplicationDbContext context,
            SkillExchange exchange,
            CancellationToken cancellationToken)
        {
            using var transaction = await context.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted, cancellationToken);
            
            try
            {
                // Check if learner has enough credits
                var creditsToTransfer = (int)Math.Ceiling(exchange.Duration);
                
                var learner = await context.Users.FindAsync(new object[] { exchange.LearnerId }, cancellationToken);
                var offerer = await context.Users.FindAsync(new object[] { exchange.OffererId }, cancellationToken);
                
                if (learner == null || offerer == null)
                {
                    throw new InvalidOperationException("Users not found");
                }
                
                if (learner.TimeCredits < creditsToTransfer)
                {
                    // Mark as no-show if learner doesn't have credits
                    exchange.Status = ExchangeStatus.NoShow;
                    exchange.Notes = $"[Auto-completed] Learner had insufficient credits ({learner.TimeCredits}/{creditsToTransfer})";
                    exchange.UpdatedAt = DateTime.UtcNow;
                    
                    _logger.LogWarning($"Exchange {exchange.Id} marked as no-show due to insufficient learner credits.");
                }
                else
                {
                    // Complete the exchange and transfer credits
                    exchange.Status = ExchangeStatus.Completed;
                    exchange.Notes = string.IsNullOrEmpty(exchange.Notes) 
                        ? "[Auto-completed by system]" 
                        : exchange.Notes + " [Auto-completed by system]";
                    exchange.UpdatedAt = DateTime.UtcNow;

                    // Transfer credits directly
                    learner.TimeCredits -= creditsToTransfer;
                    offerer.TimeCredits += creditsToTransfer;
                    learner.UpdatedAt = DateTime.UtcNow;
                    offerer.UpdatedAt = DateTime.UtcNow;

                    // Record the credit transaction
                    var fromTransaction = new CreditTransaction
                    {
                        UserId = exchange.LearnerId,
                        Amount = -creditsToTransfer,
                        BalanceAfter = learner.TimeCredits,
                        TransactionType = "ExchangeComplete",
                        Reason = $"Auto-completed exchange for skill: {exchange.Skill?.Name ?? "Unknown"}",
                        RelatedUserId = exchange.OffererId,
                        ExchangeId = exchange.Id,
                        CreatedAt = DateTime.UtcNow
                    };

                    var toTransaction = new CreditTransaction
                    {
                        UserId = exchange.OffererId,
                        Amount = creditsToTransfer,
                        BalanceAfter = offerer.TimeCredits,
                        TransactionType = "ExchangeComplete",
                        Reason = $"Auto-completed exchange for skill: {exchange.Skill?.Name ?? "Unknown"}",
                        RelatedUserId = exchange.LearnerId,
                        ExchangeId = exchange.Id,
                        CreatedAt = DateTime.UtcNow
                    };

                    context.CreditTransactions.Add(fromTransaction);
                    context.CreditTransactions.Add(toTransaction);

                    _logger.LogInformation($"Exchange {exchange.Id} auto-completed. Transferred {creditsToTransfer} credits from learner {exchange.LearnerId} to offerer {exchange.OffererId}.");
                }

                await context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, $"Failed to auto-complete exchange {exchange.Id}");
                throw;
            }
        }
    }
}