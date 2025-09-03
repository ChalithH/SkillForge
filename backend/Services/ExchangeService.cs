using Microsoft.EntityFrameworkCore;
using SkillForge.Api.Data;
using SkillForge.Api.DTOs.Exchange;
using SkillForge.Api.Models;

namespace SkillForge.Api.Services
{
    public interface IExchangeService
    {
        Task<ExchangeDto?> CreateExchangeAsync(int learnerId, CreateExchangeDto dto);
        Task<ExchangeDto?> GetExchangeByIdAsync(int id, int userId);
        Task<IEnumerable<ExchangeDto>> GetUserExchangesAsync(int userId, ExchangeStatus? status = null);
        Task<ExchangeDto?> AcceptExchangeAsync(int exchangeId, int userId, string? notes = null);
        Task<ExchangeDto?> RejectExchangeAsync(int exchangeId, int userId, string? notes = null);
        Task<ExchangeDto?> CancelExchangeAsync(int exchangeId, int userId, string? notes = null);
        Task<ExchangeDto?> CompleteExchangeAsync(int exchangeId, int userId);
        Task<ExchangeDto?> MarkAsNoShowAsync(int exchangeId, int userId, string? notes = null);
        Task<ExchangeDto?> UpdateExchangeAsync(int exchangeId, int userId, UpdateExchangeDto dto);
        Task<bool> CanUserModifyExchangeAsync(int exchangeId, int userId);
        Task<IEnumerable<ExchangeStatusHistory>> GetExchangeStatusHistoryAsync(int exchangeId);
    }

    public class ExchangeService : IExchangeService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ExchangeService> _logger;

        public ExchangeService(ApplicationDbContext context, ILogger<ExchangeService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ExchangeDto?> CreateExchangeAsync(int learnerId, CreateExchangeDto dto)
        {
            // Validate that learner exists
            var learner = await _context.Users.FindAsync(learnerId);
            if (learner == null)
            {
                return null;
            }

            // Validate that offerer exists and is not the same as learner
            if (dto.OffererId == learnerId)
            {
                throw new InvalidOperationException("Cannot create an exchange with yourself");
            }

            var offerer = await _context.Users.FindAsync(dto.OffererId);
            if (offerer == null)
            {
                return null;
            }

            // Validate that offerer has the skill
            var offererHasSkill = await _context.UserSkills
                .AnyAsync(us => us.UserId == dto.OffererId && us.SkillId == dto.SkillId && us.IsOffering);
            if (!offererHasSkill)
            {
                throw new InvalidOperationException("The offerer does not offer this skill");
            }

            // Validate scheduled time is in the future
            if (dto.ScheduledAt <= DateTime.UtcNow)
            {
                throw new InvalidOperationException("Scheduled time must be in the future");
            }

            // Create the exchange
            var exchange = new SkillExchange
            {
                OffererId = dto.OffererId,
                LearnerId = learnerId,
                SkillId = dto.SkillId,
                ScheduledAt = dto.ScheduledAt,
                Duration = dto.Duration,
                Status = ExchangeStatus.Pending,
                MeetingLink = dto.MeetingLink,
                Notes = dto.Notes,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.SkillExchanges.Add(exchange);
            await _context.SaveChangesAsync();

            // Create initial status history record
            await CreateStatusHistoryAsync(exchange.Id, null, ExchangeStatus.Pending, learnerId, "Exchange created");

            return await GetExchangeDtoAsync(exchange.Id);
        }

        public async Task<ExchangeDto?> GetExchangeByIdAsync(int id, int userId)
        {
            var exchange = await _context.SkillExchanges
                .Include(e => e.Offerer)
                .Include(e => e.Learner)
                .Include(e => e.Skill)
                .Include(e => e.Reviews)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (exchange == null || (exchange.OffererId != userId && exchange.LearnerId != userId))
            {
                return null;
            }

            return MapToDto(exchange, userId);
        }

        public async Task<IEnumerable<ExchangeDto>> GetUserExchangesAsync(int userId, ExchangeStatus? status = null)
        {
            var query = _context.SkillExchanges
                .Include(e => e.Offerer)
                .Include(e => e.Learner)
                .Include(e => e.Skill)
                .Include(e => e.Reviews)
                .Where(e => e.OffererId == userId || e.LearnerId == userId);

            if (status.HasValue)
            {
                query = query.Where(e => e.Status == status.Value);
            }

            var exchanges = await query
                .OrderByDescending(e => e.ScheduledAt)
                .ToListAsync();

            return exchanges.Select(e => MapToDto(e, userId));
        }

        public async Task<ExchangeDto?> AcceptExchangeAsync(int exchangeId, int userId, string? notes = null)
        {
            var exchange = await GetExchangeForUpdateAsync(exchangeId);
            if (exchange == null)
            {
                return null;
            }

            // Only offerer can accept
            if (exchange.OffererId != userId)
            {
                throw new InvalidOperationException("Only the offerer can accept an exchange");
            }

            // Can only accept pending exchanges
            if (exchange.Status != ExchangeStatus.Pending)
            {
                throw new InvalidOperationException($"Cannot accept exchange with status {exchange.Status}");
            }

            exchange.Status = ExchangeStatus.Accepted;
            if (!string.IsNullOrEmpty(notes))
            {
                exchange.Notes = notes;
            }
            exchange.UpdatedAt = DateTime.UtcNow;

            // Create status history record
            await CreateStatusHistoryAsync(exchangeId, ExchangeStatus.Pending, ExchangeStatus.Accepted, userId, notes ?? "Exchange accepted");

            _logger.LogInformation($"Exchange {exchangeId} accepted by user {userId}");

            return await GetExchangeDtoAsync(exchangeId);
        }

        public async Task<ExchangeDto?> RejectExchangeAsync(int exchangeId, int userId, string? notes = null)
        {
            var exchange = await GetExchangeForUpdateAsync(exchangeId);
            if (exchange == null)
            {
                return null;
            }

            // Only offerer can reject
            if (exchange.OffererId != userId)
            {
                throw new InvalidOperationException("Only the offerer can reject an exchange");
            }

            // Can only reject pending exchanges
            if (exchange.Status != ExchangeStatus.Pending)
            {
                throw new InvalidOperationException($"Cannot reject exchange with status {exchange.Status}");
            }

            exchange.Status = ExchangeStatus.Rejected;
            if (!string.IsNullOrEmpty(notes))
            {
                exchange.Notes = notes;
            }
            exchange.UpdatedAt = DateTime.UtcNow;

            // Create status history record
            await CreateStatusHistoryAsync(exchangeId, ExchangeStatus.Pending, ExchangeStatus.Rejected, userId, notes ?? "Exchange rejected");

            _logger.LogInformation($"Exchange {exchangeId} rejected by user {userId}");

            return await GetExchangeDtoAsync(exchangeId);
        }

        public async Task<ExchangeDto?> CancelExchangeAsync(int exchangeId, int userId, string? notes = null)
        {
            // Check if we're using in-memory database (for testing)
            bool isInMemoryDb = _context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory";
            
            if (isInMemoryDb)
            {
                // In-memory database doesn't support transactions
                return await CancelExchangeInternalAsync(exchangeId, userId, notes);
            }
            else
            {
                using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted);
                try
                {
                    var result = await CancelExchangeInternalAsync(exchangeId, userId, notes);
                    await transaction.CommitAsync();
                    return result;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, $"Error cancelling exchange {exchangeId}");
                    throw;
                }
            }
        }

        private async Task<ExchangeDto?> CancelExchangeInternalAsync(int exchangeId, int userId, string? notes = null)
        {
                var exchange = await _context.SkillExchanges
                    .Include(e => e.Offerer)
                    .Include(e => e.Learner)
                    .FirstOrDefaultAsync(e => e.Id == exchangeId);
                    
                if (exchange == null)
                {
                    return null;
                }

                // Either party can cancel
                if (exchange.OffererId != userId && exchange.LearnerId != userId)
                {
                    throw new InvalidOperationException("You are not part of this exchange");
                }

                // Can only cancel pending or accepted exchanges
                if (exchange.Status != ExchangeStatus.Pending && exchange.Status != ExchangeStatus.Accepted)
                {
                    throw new InvalidOperationException($"Cannot cancel exchange with status {exchange.Status}");
                }

                // If exchange was accepted and we're within 24 hours of the scheduled time,
                // maybe to implement a penalty system in the future
                var hoursUntilExchange = (exchange.ScheduledAt - DateTime.UtcNow).TotalHours;
                if (exchange.Status == ExchangeStatus.Accepted && hoursUntilExchange < 24 && hoursUntilExchange > 0)
                {
                    _logger.LogWarning($"Exchange {exchangeId} cancelled within 24 hours of scheduled time by user {userId}");
                }

                var fromStatus = exchange.Status; // Capture the current status before changing
                exchange.Status = ExchangeStatus.Cancelled;
                if (!string.IsNullOrEmpty(notes))
                {
                    exchange.Notes = notes;
                }
                exchange.UpdatedAt = DateTime.UtcNow;

                // Create status history record
                await CreateStatusHistoryAsync(exchangeId, fromStatus, ExchangeStatus.Cancelled, userId, notes ?? "Exchange cancelled");
                
                _logger.LogInformation($"Exchange {exchangeId} cancelled by user {userId}");

                return await GetExchangeDtoAsync(exchangeId);
        }

        public async Task<ExchangeDto?> CompleteExchangeAsync(int exchangeId, int userId)
        {
            // Check if we're using in-memory database (for testing)
            bool isInMemoryDb = _context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory";
            
            if (isInMemoryDb)
            {
                // In-memory database doesn't support transactions
                return await CompleteExchangeInternalAsync(exchangeId, userId);
            }
            else
            {
                using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted);
                try
                {
                    var result = await CompleteExchangeInternalAsync(exchangeId, userId);
                    await transaction.CommitAsync();
                    return result;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, $"Error completing exchange {exchangeId}");
                    throw;
                }
            }
        }

        private async Task<ExchangeDto?> CompleteExchangeInternalAsync(int exchangeId, int userId)
        {
                // Use SELECT FOR UPDATE pattern by tracking the entity
                var exchange = await _context.SkillExchanges
                    .Include(e => e.Offerer)
                    .Include(e => e.Learner)
                    .Include(e => e.Skill)
                    .FirstOrDefaultAsync(e => e.Id == exchangeId);
                    
                if (exchange == null)
                {
                    return null;
                }

                // Either party can mark as complete
                if (exchange.OffererId != userId && exchange.LearnerId != userId)
                {
                    throw new InvalidOperationException("You are not part of this exchange");
                }

                // Can only complete accepted exchanges that have passed their scheduled time
                if (exchange.Status != ExchangeStatus.Accepted)
                {
                    throw new InvalidOperationException($"Cannot complete exchange with status {exchange.Status}");
                }

                if (exchange.ScheduledAt.AddHours(exchange.Duration) > DateTime.UtcNow)
                {
                    throw new InvalidOperationException("Cannot complete exchange before it has ended");
                }

                // Update exchange status
                exchange.Status = ExchangeStatus.Completed;
                exchange.UpdatedAt = DateTime.UtcNow;

                // Get users with tracking to ensure consistency
                var offerer = exchange.Offerer ?? await _context.Users.FindAsync(exchange.OffererId);
                var learner = exchange.Learner ?? await _context.Users.FindAsync(exchange.LearnerId);

                if (offerer == null || learner == null)
                {
                    throw new InvalidOperationException("Users not found");
                }

                // Ensure users are tracked by the context
                _context.Attach(offerer);
                _context.Attach(learner);

                // Check if learner has enough credits
                var creditsToTransfer = (int)Math.Ceiling(exchange.Duration);
                if (learner.TimeCredits < creditsToTransfer)
                {
                    throw new InvalidOperationException("Learner does not have enough credits");
                }

                // Transfer credits
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
                    Reason = $"Completed exchange for skill: {exchange.Skill?.Name ?? "Unknown"}",
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
                    Reason = $"Completed exchange for skill: {exchange.Skill?.Name ?? "Unknown"}",
                    RelatedUserId = exchange.LearnerId,
                    ExchangeId = exchange.Id,
                    CreatedAt = DateTime.UtcNow
                };

                _context.CreditTransactions.Add(fromTransaction);
                _context.CreditTransactions.Add(toTransaction);

                // Create status history record
                await CreateStatusHistoryAsync(exchangeId, ExchangeStatus.Accepted, ExchangeStatus.Completed, userId, 
                    $"Exchange completed with credit transfer of {creditsToTransfer} time credits");

                _logger.LogInformation($"Exchange {exchangeId} completed. Transferred {creditsToTransfer} credits from user {learner.Id} to user {offerer.Id}");

                return await GetExchangeDtoAsync(exchangeId);
        }

        public async Task<ExchangeDto?> MarkAsNoShowAsync(int exchangeId, int userId, string? notes = null)
        {
            var exchange = await GetExchangeForUpdateAsync(exchangeId);
            if (exchange == null)
            {
                return null;
            }

            // Either party can mark as no-show
            if (exchange.OffererId != userId && exchange.LearnerId != userId)
            {
                throw new InvalidOperationException("You are not part of this exchange");
            }

            // Can only mark accepted exchanges as no-show
            if (exchange.Status != ExchangeStatus.Accepted)
            {
                throw new InvalidOperationException($"Cannot mark exchange with status {exchange.Status} as no-show");
            }

            // Should be past the scheduled time
            if (exchange.ScheduledAt.AddHours(exchange.Duration) > DateTime.UtcNow)
            {
                throw new InvalidOperationException("Cannot mark as no-show before the scheduled time has passed");
            }

            exchange.Status = ExchangeStatus.NoShow;
            if (!string.IsNullOrEmpty(notes))
            {
                exchange.Notes = notes;
            }
            exchange.UpdatedAt = DateTime.UtcNow;

            // Create status history record
            await CreateStatusHistoryAsync(exchangeId, ExchangeStatus.Accepted, ExchangeStatus.NoShow, userId, notes ?? "Exchange marked as no-show");

            _logger.LogInformation($"Exchange {exchangeId} marked as no-show by user {userId}");

            return await GetExchangeDtoAsync(exchangeId);
        }

        public async Task<ExchangeDto?> UpdateExchangeAsync(int exchangeId, int userId, UpdateExchangeDto dto)
        {
            var exchange = await GetExchangeForUpdateAsync(exchangeId);
            if (exchange == null)
            {
                return null;
            }

            // Either party can update pending exchanges, only offerer can update accepted
            if (exchange.Status == ExchangeStatus.Pending)
            {
                if (exchange.OffererId != userId && exchange.LearnerId != userId)
                {
                    throw new InvalidOperationException("You are not part of this exchange");
                }
            }
            else if (exchange.Status == ExchangeStatus.Accepted)
            {
                if (exchange.OffererId != userId)
                {
                    throw new InvalidOperationException("Only the offerer can update an accepted exchange");
                }
            }
            else
            {
                throw new InvalidOperationException($"Cannot update exchange with status {exchange.Status}");
            }

            // Update fields if provided
            if (dto.ScheduledAt.HasValue)
            {
                if (dto.ScheduledAt.Value <= DateTime.UtcNow)
                {
                    throw new InvalidOperationException("Scheduled time must be in the future");
                }
                exchange.ScheduledAt = dto.ScheduledAt.Value;
            }

            if (dto.Duration.HasValue)
            {
                exchange.Duration = dto.Duration.Value;
            }

            if (dto.MeetingLink != null)
            {
                exchange.MeetingLink = dto.MeetingLink;
            }

            if (dto.Notes != null)
            {
                exchange.Notes = dto.Notes;
            }

            exchange.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            _logger.LogInformation($"Exchange {exchangeId} updated by user {userId}");

            return await GetExchangeDtoAsync(exchangeId);
        }

        public async Task<bool> CanUserModifyExchangeAsync(int exchangeId, int userId)
        {
            var exchange = await _context.SkillExchanges
                .FirstOrDefaultAsync(e => e.Id == exchangeId);

            if (exchange == null)
            {
                return false;
            }

            // Check if user is part of the exchange
            if (exchange.OffererId != userId && exchange.LearnerId != userId)
            {
                return false;
            }

            // Can modify if pending or accepted (but not past exchanges)
            return exchange.Status == ExchangeStatus.Pending || 
                   (exchange.Status == ExchangeStatus.Accepted && exchange.ScheduledAt > DateTime.UtcNow);
        }

        private async Task<SkillExchange?> GetExchangeForUpdateAsync(int exchangeId)
        {
            return await _context.SkillExchanges
                .Include(e => e.Offerer)
                .Include(e => e.Learner)
                .FirstOrDefaultAsync(e => e.Id == exchangeId);
        }

        private async Task<ExchangeDto?> GetExchangeDtoAsync(int exchangeId)
        {
            var exchange = await _context.SkillExchanges
                .Include(e => e.Offerer)
                .Include(e => e.Learner)
                .Include(e => e.Skill)
                .Include(e => e.Reviews)
                .FirstOrDefaultAsync(e => e.Id == exchangeId);

            return exchange != null ? MapToDto(exchange, exchange.OffererId) : null;
        }

        private ExchangeDto MapToDto(SkillExchange exchange, int currentUserId)
        {
            var hasReviewed = exchange.Reviews.Any(r => r.ReviewerId == currentUserId);
            var canReview = exchange.Status == ExchangeStatus.Completed && 
                           (exchange.OffererId == currentUserId || exchange.LearnerId == currentUserId) &&
                           !hasReviewed;

            return new ExchangeDto
            {
                Id = exchange.Id,
                OffererId = exchange.OffererId,
                OffererName = exchange.Offerer?.Name ?? "",
                OffererProfileImageUrl = exchange.Offerer?.ProfileImageUrl,
                LearnerId = exchange.LearnerId,
                LearnerName = exchange.Learner?.Name ?? "",
                LearnerProfileImageUrl = exchange.Learner?.ProfileImageUrl,
                SkillId = exchange.SkillId,
                SkillName = exchange.Skill?.Name ?? "",
                SkillCategory = exchange.Skill?.Category ?? "",
                ScheduledAt = exchange.ScheduledAt,
                Duration = exchange.Duration,
                Status = exchange.Status,
                MeetingLink = exchange.MeetingLink,
                Notes = exchange.Notes,
                CreatedAt = exchange.CreatedAt,
                UpdatedAt = exchange.UpdatedAt,
                CanReview = canReview,
                HasReviewed = hasReviewed
            };
        }

        public async Task<IEnumerable<ExchangeStatusHistory>> GetExchangeStatusHistoryAsync(int exchangeId)
        {
            return await _context.ExchangeStatusHistories
                .Where(h => h.ExchangeId == exchangeId)
                .Include(h => h.ChangedByUser)
                .OrderBy(h => h.ChangedAt)
                .ToListAsync();
        }

        private async Task CreateStatusHistoryAsync(int exchangeId, ExchangeStatus? fromStatus, ExchangeStatus toStatus, int changedBy, string? reason = null)
        {
            var history = new ExchangeStatusHistory
            {
                ExchangeId = exchangeId,
                FromStatus = fromStatus,
                ToStatus = toStatus,
                ChangedBy = changedBy,
                ChangedAt = DateTime.UtcNow,
                Reason = reason
            };

            _context.ExchangeStatusHistories.Add(history);
            await _context.SaveChangesAsync();
        }
    }
}