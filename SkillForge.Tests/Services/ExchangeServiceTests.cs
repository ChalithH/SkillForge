using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SkillForge.Api.Data;
using SkillForge.Api.DTOs.Exchange;
using SkillForge.Api.Models;
using SkillForge.Api.Services;

namespace SkillForge.Tests.Services;

public class ExchangeTestBuilder
{
    private int _offererId = 1;
    private int _learnerId = 2;
    private int _skillId = 1;
    private ExchangeStatus _status = ExchangeStatus.Pending;
    private DateTime _scheduledAt = DateTime.UtcNow.AddDays(1);
    private double _duration = 1.0;
    private string? _notes = null;
    private string? _meetingLink = null;
    private List<(ExchangeStatus? from, ExchangeStatus to, int changedBy, string reason)> _statusProgression = new();

    public ExchangeTestBuilder WithOfferer(int offererId)
    {
        _offererId = offererId;
        return this;
    }

    public ExchangeTestBuilder WithLearner(int learnerId)
    {
        _learnerId = learnerId;
        return this;
    }

    public ExchangeTestBuilder WithSkill(int skillId)
    {
        _skillId = skillId;
        return this;
    }

    public ExchangeTestBuilder ScheduledAt(DateTime scheduledAt)
    {
        _scheduledAt = scheduledAt;
        return this;
    }

    public ExchangeTestBuilder WithDuration(double duration)
    {
        _duration = duration;
        return this;
    }

    public ExchangeTestBuilder WithNotes(string notes)
    {
        _notes = notes;
        return this;
    }

    public ExchangeTestBuilder ThatIsPending()
    {
        _status = ExchangeStatus.Pending;
        _statusProgression = new List<(ExchangeStatus?, ExchangeStatus, int, string)>
        {
            (null, ExchangeStatus.Pending, _learnerId, "Exchange created")
        };
        return this;
    }

    public ExchangeTestBuilder ThatWasAccepted()
    {
        _status = ExchangeStatus.Accepted;
        _statusProgression = new List<(ExchangeStatus?, ExchangeStatus, int, string)>
        {
            (null, ExchangeStatus.Pending, _learnerId, "Exchange created"),
            (ExchangeStatus.Pending, ExchangeStatus.Accepted, _offererId, "Exchange accepted")
        };
        return this;
    }

    public ExchangeTestBuilder ThatWasCompleted()
    {
        _status = ExchangeStatus.Completed;
        _statusProgression = new List<(ExchangeStatus?, ExchangeStatus, int, string)>
        {
            (null, ExchangeStatus.Pending, _learnerId, "Exchange created"),
            (ExchangeStatus.Pending, ExchangeStatus.Accepted, _offererId, "Exchange accepted"),
            (ExchangeStatus.Accepted, ExchangeStatus.Completed, _offererId, "Exchange completed")
        };
        return this;
    }

    public ExchangeTestBuilder ThatWasRejected()
    {
        _status = ExchangeStatus.Rejected;
        _statusProgression = new List<(ExchangeStatus?, ExchangeStatus, int, string)>
        {
            (null, ExchangeStatus.Pending, _learnerId, "Exchange created"),
            (ExchangeStatus.Pending, ExchangeStatus.Rejected, _offererId, "Exchange rejected")
        };
        return this;
    }

    public ExchangeTestBuilder InThePast(int hoursAgo = 2)
    {
        _scheduledAt = DateTime.UtcNow.AddHours(-hoursAgo);
        return this;
    }

    public async Task<SkillExchange> BuildInDatabase(ApplicationDbContext context)
    {
        var exchange = new SkillExchange
        {
            OffererId = _offererId,
            LearnerId = _learnerId,
            SkillId = _skillId,
            ScheduledAt = _scheduledAt,
            Duration = _duration,
            Status = _status,
            Notes = _notes,
            MeetingLink = _meetingLink,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.SkillExchanges.Add(exchange);
        await context.SaveChangesAsync();

        // Create status history if we have a progression
        if (_statusProgression.Any())
        {
            var baseTime = DateTime.UtcNow.AddHours(-3);
            for (int i = 0; i < _statusProgression.Count; i++)
            {
                var (fromStatus, toStatus, changedBy, reason) = _statusProgression[i];
                var history = new ExchangeStatusHistory
                {
                    ExchangeId = exchange.Id,
                    FromStatus = fromStatus,
                    ToStatus = toStatus,
                    ChangedBy = changedBy,
                    ChangedAt = baseTime.AddMinutes(i * 30), // Spread history records over time
                    Reason = reason
                };
                context.ExchangeStatusHistories.Add(history);
            }
            await context.SaveChangesAsync();
        }

        return exchange;
    }

    public SkillExchange BuildInMemory()
    {
        return new SkillExchange
        {
            Id = 1, // Default ID for in-memory tests
            OffererId = _offererId,
            LearnerId = _learnerId,
            SkillId = _skillId,
            ScheduledAt = _scheduledAt,
            Duration = _duration,
            Status = _status,
            Notes = _notes,
            MeetingLink = _meetingLink,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public int ExpectedHistoryCount => _statusProgression.Count;
}

public class ExchangeServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly ExchangeService _exchangeService;
    private readonly Mock<ILogger<ExchangeService>> _mockLogger;

    public ExchangeServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _mockLogger = new Mock<ILogger<ExchangeService>>();
        _exchangeService = new ExchangeService(_context, _mockLogger.Object);

        SeedTestData();
    }

    private void SeedTestData()
    {
        // Add test users
        var users = new[]
        {
            new User { Id = 1, Name = "John Offerer", Email = "john@test.com", PasswordHash = "hash1", TimeCredits = 10, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new User { Id = 2, Name = "Jane Learner", Email = "jane@test.com", PasswordHash = "hash2", TimeCredits = 5, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        };
        _context.Users.AddRange(users);

        // Add test skill
        var skill = new Skill { Id = 1, Name = "Test Skill", Category = "Programming" };
        _context.Skills.Add(skill);

        // Add user skill - John offers Test Skill
        var userSkill = new UserSkill 
        { 
            Id = 1, 
            UserId = 1, 
            SkillId = 1, 
            IsOffering = true, 
            ProficiencyLevel = 3 
        };
        _context.UserSkills.Add(userSkill);

        _context.SaveChanges();
    }

    [Fact]
    public async Task CreateExchangeAsync_ValidExchange_ShouldCreateInitialStatusHistoryRecord()
    {
        // Arrange
        var createDto = new CreateExchangeDto
        {
            OffererId = 1,
            SkillId = 1,
            ScheduledAt = DateTime.UtcNow.AddDays(1),
            Duration = 1.0,
            Notes = "Test exchange"
        };

        // Act
        var result = await _exchangeService.CreateExchangeAsync(2, createDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ExchangeStatus.Pending, result.Status);

        // Verify status history was created
        var statusHistory = await _context.ExchangeStatusHistories
            .Where(h => h.ExchangeId == result.Id)
            .ToListAsync();

        Assert.Single(statusHistory);
        var historyRecord = statusHistory.First();
        Assert.Null(historyRecord.FromStatus); // Initial creation has no previous status
        Assert.Equal(ExchangeStatus.Pending, historyRecord.ToStatus);
        Assert.Equal(2, historyRecord.ChangedBy); // Learner created the exchange
        Assert.True(historyRecord.ChangedAt > DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public async Task AcceptExchangeAsync_PendingExchange_ShouldCreateStatusHistoryRecord()
    {
        // Arrange
        var exchange = await new ExchangeTestBuilder()
            .ThatIsPending()
            .BuildInDatabase(_context);

        // Act
        var result = await _exchangeService.AcceptExchangeAsync(exchange.Id, 1, "Accepted with notes");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ExchangeStatus.Accepted, result.Status);

        // Verify status history was created
        var statusHistory = await _context.ExchangeStatusHistories
            .Where(h => h.ExchangeId == exchange.Id)
            .OrderBy(h => h.ChangedAt)
            .ToListAsync();

        Assert.Equal(2, statusHistory.Count); // Creation + Accept
        var acceptRecord = statusHistory.Last();
        Assert.Equal(ExchangeStatus.Pending, acceptRecord.FromStatus);
        Assert.Equal(ExchangeStatus.Accepted, acceptRecord.ToStatus);
        Assert.Equal(1, acceptRecord.ChangedBy); // Offerer accepted
        Assert.Equal("Accepted with notes", acceptRecord.Reason);
    }

    [Fact]
    public async Task CompleteExchangeAsync_AcceptedExchange_ShouldCreateStatusHistoryWithCreditTransfer()
    {
        // Arrange
        var exchange = await new ExchangeTestBuilder()
            .ThatWasAccepted()
            .InThePast(2)
            .BuildInDatabase(_context);

        // Act
        var result = await _exchangeService.CompleteExchangeAsync(exchange.Id, 1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ExchangeStatus.Completed, result.Status);

        // Verify status history was created
        var statusHistory = await _context.ExchangeStatusHistories
            .Where(h => h.ExchangeId == exchange.Id)
            .OrderBy(h => h.ChangedAt)
            .ToListAsync();

        Assert.Equal(3, statusHistory.Count); // Creation + Accept + Complete
        var completeRecord = statusHistory.Last();
        Assert.Equal(ExchangeStatus.Accepted, completeRecord.FromStatus);
        Assert.Equal(ExchangeStatus.Completed, completeRecord.ToStatus);
        Assert.Equal(1, completeRecord.ChangedBy);
        Assert.Contains("credit transfer", completeRecord.Reason?.ToLower() ?? "");
    }

    [Fact]
    public async Task CancelExchangeAsync_AcceptedExchange_ShouldCreateStatusHistoryWithReason()
    {
        // Arrange
        var exchange = await new ExchangeTestBuilder()
            .ThatWasAccepted()
            .BuildInDatabase(_context);

        // Act
        var result = await _exchangeService.CancelExchangeAsync(exchange.Id, 2, "Had to cancel due to scheduling conflict");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ExchangeStatus.Cancelled, result.Status);

        // Verify status history
        var statusHistory = await _context.ExchangeStatusHistories
            .Where(h => h.ExchangeId == exchange.Id)
            .OrderBy(h => h.ChangedAt)
            .ToListAsync();

        Assert.Equal(3, statusHistory.Count);
        var cancelRecord = statusHistory.Last();
        Assert.Equal(ExchangeStatus.Accepted, cancelRecord.FromStatus);
        Assert.Equal(ExchangeStatus.Cancelled, cancelRecord.ToStatus);
        Assert.Equal(2, cancelRecord.ChangedBy); // Learner cancelled
        Assert.Equal("Had to cancel due to scheduling conflict", cancelRecord.Reason);
    }

    [Fact]
    public async Task GetExchangeStatusHistoryAsync_ExchangeWithHistory_ShouldReturnChronologicalHistory()
    {
        // Arrange - Create exchange with multiple status changes
        var exchange = new SkillExchange
        {
            OffererId = 1,
            LearnerId = 2,
            SkillId = 1,
            ScheduledAt = DateTime.UtcNow.AddDays(1),
            Duration = 1.0,
            Status = ExchangeStatus.Accepted,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.SkillExchanges.Add(exchange);
        await _context.SaveChangesAsync();

        // Manually add status history records to simulate progression
        var historyRecords = new[]
        {
            new ExchangeStatusHistory
            {
                ExchangeId = exchange.Id,
                FromStatus = null,
                ToStatus = ExchangeStatus.Pending,
                ChangedBy = 2,
                ChangedAt = DateTime.UtcNow.AddMinutes(-10),
                Reason = "Exchange created"
            },
            new ExchangeStatusHistory
            {
                ExchangeId = exchange.Id,
                FromStatus = ExchangeStatus.Pending,
                ToStatus = ExchangeStatus.Accepted,
                ChangedBy = 1,
                ChangedAt = DateTime.UtcNow.AddMinutes(-5),
                Reason = "Exchange accepted"
            }
        };
        _context.ExchangeStatusHistories.AddRange(historyRecords);
        await _context.SaveChangesAsync();

        // Act
        var history = await _exchangeService.GetExchangeStatusHistoryAsync(exchange.Id);

        // Assert
        Assert.Equal(2, history.Count());
        var historyList = history.ToList();
        
        // Verify chronological order (oldest first)
        Assert.Null(historyList[0].FromStatus);
        Assert.Equal(ExchangeStatus.Pending, historyList[0].ToStatus);
        Assert.Equal(ExchangeStatus.Pending, historyList[1].FromStatus);
        Assert.Equal(ExchangeStatus.Accepted, historyList[1].ToStatus);
    }

    [Fact]
    public async Task ChangeExchangeStatus_InvalidTransition_ShouldThrowException()
    {
        // Arrange
        var exchange = await new ExchangeTestBuilder()
            .ThatWasCompleted()
            .InThePast(2)
            .BuildInDatabase(_context);

        // Act & Assert - Try to accept a completed exchange
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _exchangeService.AcceptExchangeAsync(exchange.Id, 1));

        // Verify no status history was created for invalid operation
        var historyCount = await _context.ExchangeStatusHistories
            .CountAsync(h => h.ExchangeId == exchange.Id);
        Assert.Equal(3, historyCount); // Only the initial history records we created
    }

    [Fact]
    public async Task CompleteExchange_AlreadyCompleted_ShouldThrowException()
    {
        // Arrange - Create completed exchange
        var exchange = new SkillExchange
        {
            OffererId = 1,
            LearnerId = 2,
            SkillId = 1,
            ScheduledAt = DateTime.UtcNow.AddHours(-2),
            Duration = 1.0,
            Status = ExchangeStatus.Completed,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.SkillExchanges.Add(exchange);
        await _context.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _exchangeService.CompleteExchangeAsync(exchange.Id, 1));
    }

    [Fact]
    public async Task RejectExchangeAsync_PendingExchange_ShouldCreateStatusHistoryRecord()
    {
        // Arrange
        var exchange = await new ExchangeTestBuilder()
            .ThatIsPending()
            .BuildInDatabase(_context);

        // Act
        var result = await _exchangeService.RejectExchangeAsync(exchange.Id, 1, "Not available at that time");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ExchangeStatus.Rejected, result.Status);

        // Verify status history
        var statusHistory = await _context.ExchangeStatusHistories
            .Where(h => h.ExchangeId == exchange.Id)
            .OrderBy(h => h.ChangedAt)
            .ToListAsync();

        Assert.Equal(2, statusHistory.Count);
        var rejectRecord = statusHistory.Last();
        Assert.Equal(ExchangeStatus.Pending, rejectRecord.FromStatus);
        Assert.Equal(ExchangeStatus.Rejected, rejectRecord.ToStatus);
        Assert.Equal(1, rejectRecord.ChangedBy);
        Assert.Equal("Not available at that time", rejectRecord.Reason);
    }

    [Fact]
    public async Task MarkAsNoShowAsync_AcceptedExchange_ShouldCreateStatusHistoryRecord()
    {
        // Arrange
        var exchange = await new ExchangeTestBuilder()
            .ThatWasAccepted()
            .InThePast(2)
            .BuildInDatabase(_context);

        // Act
        var result = await _exchangeService.MarkAsNoShowAsync(exchange.Id, 1, "Other party didn't show up");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ExchangeStatus.NoShow, result.Status);

        // Verify status history
        var statusHistory = await _context.ExchangeStatusHistories
            .Where(h => h.ExchangeId == exchange.Id)
            .OrderBy(h => h.ChangedAt)
            .ToListAsync();

        Assert.Equal(3, statusHistory.Count);
        var noShowRecord = statusHistory.Last();
        Assert.Equal(ExchangeStatus.Accepted, noShowRecord.FromStatus);
        Assert.Equal(ExchangeStatus.NoShow, noShowRecord.ToStatus);
        Assert.Equal(1, noShowRecord.ChangedBy);
        Assert.Equal("Other party didn't show up", noShowRecord.Reason);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}