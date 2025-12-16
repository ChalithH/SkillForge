using Microsoft.AspNetCore.SignalR;
using SkillForge.Api.Hubs;
using SkillForge.Api.Models;

namespace SkillForge.Api.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(IHubContext<NotificationHub> hubContext, ILogger<NotificationService> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task SendExchangeRequestNotificationAsync(SkillExchange exchange)
        {
            // Notification goes to the Offerer (skill provider) about an incoming request from the Learner
            var message = $"New skill exchange request from {exchange.Learner?.Name ?? "Unknown"} for {exchange.Skill?.Name ?? "Unknown skill"}";

            await _hubContext.Clients.Group($"User_{exchange.OffererId}")
                .SendAsync("ReceiveNotification", new
                {
                    Type = "exchange_request",
                    Message = message,
                    ExchangeId = exchange.Id,
                    SenderId = exchange.LearnerId,
                    SenderName = exchange.Learner?.Name,
                    SkillName = exchange.Skill?.Name,
                    ScheduledAt = exchange.ScheduledAt,
                    Duration = exchange.Duration,
                    Timestamp = DateTime.UtcNow
                });

            _logger.LogInformation("Sent exchange request notification to user {OffererId} for exchange {ExchangeId}",
                exchange.OffererId, exchange.Id);
        }

        public async Task SendExchangeStatusUpdateNotificationAsync(SkillExchange exchange, ExchangeStatus previousStatus)
        {
            var statusMessage = exchange.Status switch
            {
                ExchangeStatus.Accepted => "accepted your skill exchange request",
                ExchangeStatus.Rejected => "declined your skill exchange request",
                ExchangeStatus.Cancelled => "cancelled the skill exchange",
                ExchangeStatus.Completed => "marked the skill exchange as completed",
                ExchangeStatus.NoShow => "marked you as a no-show for the skill exchange",
                _ => $"updated the status of your skill exchange to {exchange.Status}"
            };

            // Offerer accepts/rejects requests → notify the Learner who sent the request
            // For other status changes, notify the Learner (simplification - ideally we'd know who triggered the action)
            var targetUserId = exchange.Status == ExchangeStatus.Accepted || exchange.Status == ExchangeStatus.Rejected
                ? exchange.LearnerId  // Notify the learner about offerer's response to their request
                : exchange.LearnerId; // Notify the learner about other status changes (e.g., offerer cancelled)

            var actorName = targetUserId == exchange.OffererId ? exchange.Learner?.Name : exchange.Offerer?.Name;
            var message = $"{actorName ?? "Someone"} {statusMessage}";

            await _hubContext.Clients.Group($"User_{targetUserId}")
                .SendAsync("ReceiveNotification", new
                {
                    Type = "exchange_status_update",
                    Message = message,
                    ExchangeId = exchange.Id,
                    Status = exchange.Status.ToString(),
                    PreviousStatus = previousStatus.ToString(),
                    SkillName = exchange.Skill?.Name,
                    ActorId = targetUserId == exchange.OffererId ? exchange.LearnerId : exchange.OffererId,
                    ActorName = actorName,
                    Timestamp = DateTime.UtcNow
                });

            _logger.LogInformation("Sent exchange status update notification to user {UserId} for exchange {ExchangeId}: {Status}", 
                targetUserId, exchange.Id, exchange.Status);
        }

        public async Task SendCreditTransferNotificationAsync(int userId, int amount, string reason)
        {
            var message = amount > 0 
                ? $"You received {amount} time credits: {reason}"
                : $"You spent {Math.Abs(amount)} time credits: {reason}";

            await _hubContext.Clients.Group($"User_{userId}")
                .SendAsync("ReceiveNotification", new
                {
                    Type = "credit_transfer",
                    Message = message,
                    Amount = amount,
                    Reason = reason,
                    Timestamp = DateTime.UtcNow
                });

            _logger.LogInformation("Sent credit transfer notification to user {UserId}: {Amount} credits", userId, amount);
        }

        public async Task SendGeneralNotificationAsync(int userId, string message, string type = "info")
        {
            await _hubContext.Clients.Group($"User_{userId}")
                .SendAsync("ReceiveNotification", new
                {
                    Type = type,
                    Message = message,
                    Timestamp = DateTime.UtcNow
                });

            _logger.LogInformation("Sent general notification to user {UserId}: {Message}", userId, message);
        }

        public async Task BroadcastUserPresenceUpdateAsync(int userId, bool isOnline)
        {
            var eventName = isOnline ? "UserOnline" : "UserOffline";
            
            await _hubContext.Clients.All.SendAsync(eventName, userId);
            
            _logger.LogInformation("Broadcasted user presence update: User {UserId} is {Status}", 
                userId, isOnline ? "online" : "offline");
        }

        public async Task SendToGroupAsync(string groupName, string method, object? data)
        {
            await _hubContext.Clients.Group(groupName).SendAsync(method, data);
            
            _logger.LogInformation("Sent {Method} to group {GroupName}", method, groupName);
        }
    }
}