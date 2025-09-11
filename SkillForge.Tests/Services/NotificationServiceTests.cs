using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using SkillForge.Api.Hubs;
using SkillForge.Api.Models;
using SkillForge.Api.Services;

namespace SkillForge.Tests.Services;

public class NotificationTestBuilder
{
    private int _id = 1;
    private int _offererId = 1;
    private int _learnerId = 2;
    private int _skillId = 1;
    private DateTime _scheduledAt = DateTime.UtcNow.AddDays(1);
    private double _duration = 1.0;
    private ExchangeStatus _status = ExchangeStatus.Pending;
    private User? _offerer = null;
    private User? _learner = null;
    private Skill? _skill = null;

    public NotificationTestBuilder WithId(int id)
    {
        _id = id;
        return this;
    }

    public NotificationTestBuilder WithOffererId(int offererId)
    {
        _offererId = offererId;
        return this;
    }

    public NotificationTestBuilder WithLearnerId(int learnerId)
    {
        _learnerId = learnerId;
        return this;
    }

    public NotificationTestBuilder WithSkillId(int skillId)
    {
        _skillId = skillId;
        return this;
    }

    public NotificationTestBuilder WithStatus(ExchangeStatus status)
    {
        _status = status;
        return this;
    }

    public NotificationTestBuilder WithOfferer(string name, string email = "offerer@test.com")
    {
        _offerer = new User
        {
            Id = _offererId,
            Name = name,
            Email = email,
            PasswordHash = "hash",
            TimeCredits = 10,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        return this;
    }

    public NotificationTestBuilder WithLearner(string name, string email = "learner@test.com")
    {
        _learner = new User
        {
            Id = _learnerId,
            Name = name,
            Email = email,
            PasswordHash = "hash",
            TimeCredits = 5,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        return this;
    }

    public NotificationTestBuilder WithSkill(string name, string category = "Technology")
    {
        _skill = new Skill
        {
            Id = _skillId,
            Name = name,
            Category = category,
            Description = $"Learn {name} skills"
        };
        return this;
    }

    public SkillExchange BuildExchange()
    {
        return new SkillExchange
        {
            Id = _id,
            OffererId = _offererId,
            LearnerId = _learnerId,
            SkillId = _skillId,
            ScheduledAt = _scheduledAt,
            Duration = _duration,
            Status = _status,
            Offerer = _offerer,
            Learner = _learner,
            Skill = _skill,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}

public class NotificationServiceTests : IDisposable
{
    private readonly Mock<IHubContext<NotificationHub>> _mockHubContext;
    private readonly Mock<ILogger<NotificationService>> _mockLogger;
    private readonly Mock<IClientProxy> _mockClientProxy;
    private readonly NotificationService _notificationService;

    public NotificationServiceTests()
    {
        _mockHubContext = new Mock<IHubContext<NotificationHub>>();
        _mockLogger = new Mock<ILogger<NotificationService>>();
        _mockClientProxy = new Mock<IClientProxy>();

        // Setup the hub context to return our mock client proxy
        _mockHubContext.Setup(h => h.Clients.Group(It.IsAny<string>())).Returns(_mockClientProxy.Object);
        _mockHubContext.Setup(h => h.Clients.All).Returns(_mockClientProxy.Object);

        _notificationService = new NotificationService(_mockHubContext.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task SendExchangeRequestNotificationAsync_ValidExchange_CallsSignalR()
    {
        // Arrange
        var builder = new NotificationTestBuilder()
            .WithId(1)
            .WithOffererId(1)
            .WithLearnerId(2)
            .WithOfferer("Alice")
            .WithSkill("JavaScript", "Programming");

        var exchange = builder.BuildExchange();

        // Act
        await _notificationService.SendExchangeRequestNotificationAsync(exchange);

        // Assert - Verify the correct group was targeted
        _mockHubContext.Verify(h => h.Clients.Group("User_2"), Times.Once);
        
        // Verify SendCoreAsync was called (the actual method behind SendAsync extension)
        _mockClientProxy.Verify(cp => cp.SendCoreAsync(
            "ReceiveNotification", 
            It.Is<object[]>(args => args.Length == 1), 
            default), 
            Times.Once);
        
        // Verify logging occurred
        VerifyLogInformation("Sent exchange request notification to user {LearnerId} for exchange {ExchangeId}");
    }

    [Fact]
    public async Task SendExchangeRequestNotificationAsync_ExchangeWithNullOfferer_HandlesGracefully()
    {
        // Arrange
        var builder = new NotificationTestBuilder()
            .WithId(1)
            .WithOffererId(1)
            .WithLearnerId(2)
            .WithSkill("JavaScript");
        // Note: Not setting offerer, so it will be null

        var exchange = builder.BuildExchange();

        // Act & Assert - Should not throw
        await _notificationService.SendExchangeRequestNotificationAsync(exchange);

        // Verify the service handled null gracefully
        _mockHubContext.Verify(h => h.Clients.Group("User_2"), Times.Once);
        _mockClientProxy.Verify(cp => cp.SendCoreAsync(
            "ReceiveNotification", 
            It.Is<object[]>(args => args.Length == 1), 
            default), 
            Times.Once);
    }

    [Theory]
    [InlineData(ExchangeStatus.Accepted)]
    [InlineData(ExchangeStatus.Rejected)]
    [InlineData(ExchangeStatus.Cancelled)]
    [InlineData(ExchangeStatus.Completed)]
    [InlineData(ExchangeStatus.NoShow)]
    public async Task SendExchangeStatusUpdateNotificationAsync_DifferentStatuses_CallsSignalR(ExchangeStatus status)
    {
        // Arrange
        var builder = new NotificationTestBuilder()
            .WithId(1)
            .WithOffererId(1)
            .WithLearnerId(2)
            .WithStatus(status)
            .WithOfferer("Alice")
            .WithLearner("Bob")
            .WithSkill("JavaScript");

        var exchange = builder.BuildExchange();

        // Act
        await _notificationService.SendExchangeStatusUpdateNotificationAsync(exchange, ExchangeStatus.Pending);

        // Assert - Verify SignalR was called
        _mockClientProxy.Verify(cp => cp.SendCoreAsync(
            "ReceiveNotification", 
            It.Is<object[]>(args => args.Length == 1), 
            default), 
            Times.Once);
        
        // Verify appropriate logging
        VerifyLogInformation("Sent exchange status update notification to user {UserId} for exchange {ExchangeId}: {Status}");
    }

    [Fact]
    public async Task SendExchangeStatusUpdateNotificationAsync_AcceptedStatus_NotifiesOfferer()
    {
        // Arrange
        var builder = new NotificationTestBuilder()
            .WithId(1)
            .WithOffererId(1)
            .WithLearnerId(2)
            .WithStatus(ExchangeStatus.Accepted)
            .WithOfferer("Alice")
            .WithLearner("Bob")
            .WithSkill("JavaScript");

        var exchange = builder.BuildExchange();

        // Act
        await _notificationService.SendExchangeStatusUpdateNotificationAsync(exchange, ExchangeStatus.Pending);

        // Assert - Should notify offerer (user 1) for accepted status
        _mockHubContext.Verify(h => h.Clients.Group("User_1"), Times.Once);
    }

    [Fact]
    public async Task SendExchangeStatusUpdateNotificationAsync_CancelledStatus_NotifiesLearner()
    {
        // Arrange
        var builder = new NotificationTestBuilder()
            .WithId(1)
            .WithOffererId(1)
            .WithLearnerId(2)
            .WithStatus(ExchangeStatus.Cancelled)
            .WithOfferer("Alice")
            .WithLearner("Bob")
            .WithSkill("JavaScript");

        var exchange = builder.BuildExchange();

        // Act
        await _notificationService.SendExchangeStatusUpdateNotificationAsync(exchange, ExchangeStatus.Accepted);

        // Assert - Should notify learner (user 2) for cancelled status
        _mockHubContext.Verify(h => h.Clients.Group("User_2"), Times.Once);
    }

    [Fact]
    public async Task SendCreditTransferNotificationAsync_PositiveAmount_CallsSignalR()
    {
        // Arrange
        const int userId = 1;
        const int amount = 5;
        const string reason = "Exchange completion";

        // Act
        await _notificationService.SendCreditTransferNotificationAsync(userId, amount, reason);

        // Assert
        _mockHubContext.Verify(h => h.Clients.Group("User_1"), Times.Once);
        _mockClientProxy.Verify(cp => cp.SendCoreAsync(
            "ReceiveNotification", 
            It.Is<object[]>(args => args.Length == 1), 
            default), 
            Times.Once);
        
        VerifyLogInformation("Sent credit transfer notification to user {UserId}: {Amount} credits");
    }

    [Fact]
    public async Task SendCreditTransferNotificationAsync_NegativeAmount_CallsSignalR()
    {
        // Arrange
        const int userId = 1;
        const int amount = -3;
        const string reason = "Exchange payment";

        // Act
        await _notificationService.SendCreditTransferNotificationAsync(userId, amount, reason);

        // Assert
        _mockHubContext.Verify(h => h.Clients.Group("User_1"), Times.Once);
        _mockClientProxy.Verify(cp => cp.SendCoreAsync(
            "ReceiveNotification", 
            It.Is<object[]>(args => args.Length == 1), 
            default), 
            Times.Once);
    }

    [Fact]
    public async Task SendGeneralNotificationAsync_WithDefaultType_CallsSignalR()
    {
        // Arrange
        const int userId = 1;
        const string message = "Welcome to SkillForge!";

        // Act
        await _notificationService.SendGeneralNotificationAsync(userId, message);

        // Assert
        _mockHubContext.Verify(h => h.Clients.Group("User_1"), Times.Once);
        _mockClientProxy.Verify(cp => cp.SendCoreAsync(
            "ReceiveNotification", 
            It.Is<object[]>(args => args.Length == 1), 
            default), 
            Times.Once);
        
        VerifyLogInformation("Sent general notification to user {UserId}: {Message}");
    }

    [Fact]
    public async Task SendGeneralNotificationAsync_WithCustomType_CallsSignalR()
    {
        // Arrange
        const int userId = 1;
        const string message = "Your account has been verified";
        const string type = "success";

        // Act
        await _notificationService.SendGeneralNotificationAsync(userId, message, type);

        // Assert
        _mockHubContext.Verify(h => h.Clients.Group("User_1"), Times.Once);
        _mockClientProxy.Verify(cp => cp.SendCoreAsync(
            "ReceiveNotification", 
            It.Is<object[]>(args => args.Length == 1), 
            default), 
            Times.Once);
    }

    [Fact]
    public async Task BroadcastUserPresenceUpdateAsync_UserOnline_CallsSignalR()
    {
        // Arrange
        const int userId = 1;

        // Act
        await _notificationService.BroadcastUserPresenceUpdateAsync(userId, true);

        // Assert - Should broadcast to all clients
        _mockHubContext.Verify(h => h.Clients.All, Times.Once);
        _mockClientProxy.Verify(cp => cp.SendCoreAsync(
            "UserOnline", 
            It.Is<object[]>(args => args.Length == 1 && (int)args[0] == userId), 
            default), 
            Times.Once);
        
        VerifyLogInformation("Broadcasted user presence update: User {UserId} is {Status}");
    }

    [Fact]
    public async Task BroadcastUserPresenceUpdateAsync_UserOffline_CallsSignalR()
    {
        // Arrange
        const int userId = 1;

        // Act
        await _notificationService.BroadcastUserPresenceUpdateAsync(userId, false);

        // Assert
        _mockHubContext.Verify(h => h.Clients.All, Times.Once);
        _mockClientProxy.Verify(cp => cp.SendCoreAsync(
            "UserOffline", 
            It.Is<object[]>(args => args.Length == 1 && (int)args[0] == userId), 
            default), 
            Times.Once);
    }

    [Fact]
    public async Task SendToGroupAsync_ValidParameters_CallsSignalR()
    {
        // Arrange
        const string groupName = "Admins";
        const string method = "AdminAlert";
        var data = new { Message = "System maintenance in 5 minutes", Priority = "High" };

        // Act
        await _notificationService.SendToGroupAsync(groupName, method, data);

        // Assert
        _mockHubContext.Verify(h => h.Clients.Group(groupName), Times.Once);
        _mockClientProxy.Verify(cp => cp.SendCoreAsync(
            method, 
            It.Is<object[]>(args => args.Length == 1 && args[0].Equals(data)), 
            default), 
            Times.Once);
        
        VerifyLogInformation("Sent {Method} to group {GroupName}");
    }

    [Fact]
    public async Task SendToGroupAsync_NullData_CallsSignalR()
    {
        // Arrange
        const string groupName = "TestGroup";
        const string method = "TestMethod";

        // Act
        await _notificationService.SendToGroupAsync(groupName, method, null);

        // Assert
        _mockHubContext.Verify(h => h.Clients.Group(groupName), Times.Once);
        _mockClientProxy.Verify(cp => cp.SendCoreAsync(
            method, 
            It.Is<object[]>(args => args.Length == 1 && args[0] == null), 
            default), 
            Times.Once);
    }

    private void VerifyLogInformation(string messageTemplate)
    {
        var expectedText = messageTemplate.Split('{')[0].Trim();
        _mockLogger.Verify(
            logger => logger.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(expectedText)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.AtLeastOnce);
    }

    public void Dispose()
    {
        // No resources to dispose in this test class
    }
}