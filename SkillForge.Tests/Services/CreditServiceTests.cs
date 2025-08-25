using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using SkillForge.Api.Data;
using SkillForge.Api.Models;
using SkillForge.Api.Services;

namespace SkillForge.Tests.Services;

public class CreditServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly CreditService _creditService;
    private readonly ILogger<CreditService> _logger;

    public CreditServiceTests()
    {
        // Create in-memory database for testing with transaction warnings ignored
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new ApplicationDbContext(options);
        
        // Create a mock logger
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<CreditService>();
        
        _creditService = new CreditService(_context, _logger);
    }

    [Fact]
    public async Task TransferCreditsAsync_ValidTransfer_UpdatesBalancesCorrectly()
    {
        // Arrange
        var fromUser = new User 
        { 
            Name = "Alice", 
            Email = "alice@test.com", 
            PasswordHash = "hash",
            TimeCredits = 10,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var toUser = new User 
        { 
            Name = "Bob", 
            Email = "bob@test.com", 
            PasswordHash = "hash",
            TimeCredits = 5,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Users.AddRange(fromUser, toUser);
        await _context.SaveChangesAsync();

        // Act
        var result = await _creditService.TransferCreditsAsync(fromUser.Id, toUser.Id, 3, "Test transfer");

        // Assert
        Assert.True(result);
        
        var updatedFromUser = await _context.Users.FindAsync(fromUser.Id);
        var updatedToUser = await _context.Users.FindAsync(toUser.Id);

        Assert.Equal(7, updatedFromUser!.TimeCredits); // 10 - 3 = 7
        Assert.Equal(8, updatedToUser!.TimeCredits);   // 5 + 3 = 8
    }

    [Fact]
    public async Task TransferCreditsAsync_InsufficientFunds_ThrowsException()
    {
        // Arrange
        var fromUser = new User 
        { 
            Name = "Alice", 
            Email = "alice@test.com", 
            PasswordHash = "hash",
            TimeCredits = 2, // Only has 2 credits
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var toUser = new User 
        { 
            Name = "Bob", 
            Email = "bob@test.com", 
            PasswordHash = "hash",
            TimeCredits = 5,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Users.AddRange(fromUser, toUser);
        await _context.SaveChangesAsync();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _creditService.TransferCreditsAsync(fromUser.Id, toUser.Id, 5, "Test transfer"));

        Assert.Contains("Insufficient credits", exception.Message);
    }

    [Fact]
    public async Task TransferCreditsAsync_NegativeAmount_ThrowsArgumentException()
    {
        // Arrange
        var fromUser = new User 
        { 
            Name = "Alice", 
            Email = "alice@test.com", 
            PasswordHash = "hash",
            TimeCredits = 10,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var toUser = new User 
        { 
            Name = "Bob", 
            Email = "bob@test.com", 
            PasswordHash = "hash",
            TimeCredits = 5,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Users.AddRange(fromUser, toUser);
        await _context.SaveChangesAsync();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _creditService.TransferCreditsAsync(fromUser.Id, toUser.Id, -5, "Test transfer"));

        Assert.Equal("Amount must be positive (Parameter 'amount')", exception.Message);
    }

    [Fact]
    public async Task AddCreditsAsync_ValidAmount_IncreasesBalance()
    {
        // Arrange
        var user = new User 
        { 
            Name = "Alice", 
            Email = "alice@test.com", 
            PasswordHash = "hash",
            TimeCredits = 10,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _creditService.AddCreditsAsync(user.Id, 5, "Bonus credits");

        // Assert
        Assert.True(result);
        
        var updatedUser = await _context.Users.FindAsync(user.Id);
        Assert.Equal(15, updatedUser!.TimeCredits); // 10 + 5 = 15
    }

    [Fact]
    public async Task GetUserCreditsAsync_ExistingUser_ReturnsCorrectBalance()
    {
        // Arrange
        var user = new User 
        { 
            Name = "Alice", 
            Email = "alice@test.com", 
            PasswordHash = "hash",
            TimeCredits = 42,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var credits = await _creditService.GetUserCreditsAsync(user.Id);

        // Assert
        Assert.Equal(42, credits);
    }

    [Fact]
    public async Task GetUserCreditsAsync_NonExistentUser_ReturnsZero()
    {
        // Act
        var credits = await _creditService.GetUserCreditsAsync(999);

        // Assert
        Assert.Equal(0, credits);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}