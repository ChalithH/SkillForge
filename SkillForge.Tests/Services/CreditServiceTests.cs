using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using SkillForge.Api.Data;
using SkillForge.Api.Models;
using SkillForge.Api.Services;

namespace SkillForge.Tests.Services;

public class CreditTestBuilder
{
    private string _name = "Test User";
    private string _email = "test@example.com";
    private string _passwordHash = "hash";
    private int _timeCredits = 5;
    private DateTime? _createdAt = null;

    public CreditTestBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public CreditTestBuilder WithEmail(string email)
    {
        _email = email;
        return this;
    }

    public CreditTestBuilder WithTimeCredits(int credits)
    {
        _timeCredits = credits;
        return this;
    }

    public CreditTestBuilder CreatedAt(DateTime createdAt)
    {
        _createdAt = createdAt;
        return this;
    }

    public async Task<User> BuildInDatabase(ApplicationDbContext context)
    {
        var user = new User
        {
            Name = _name,
            Email = _email,
            PasswordHash = _passwordHash,
            TimeCredits = _timeCredits,
            CreatedAt = _createdAt ?? DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    public User BuildInMemory()
    {
        return new User
        {
            Id = 1,
            Name = _name,
            Email = _email,
            PasswordHash = _passwordHash,
            TimeCredits = _timeCredits,
            CreatedAt = _createdAt ?? DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public string GetName() => _name;
    public string GetEmail() => _email;
    public int GetTimeCredits() => _timeCredits;
}

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
        var fromBuilder = new CreditTestBuilder()
            .WithName("Alice")
            .WithEmail("alice@test.com")
            .WithTimeCredits(10);
        var toBuilder = new CreditTestBuilder()
            .WithName("Bob")
            .WithEmail("bob@test.com")
            .WithTimeCredits(5);

        var fromUser = await fromBuilder.BuildInDatabase(_context);
        var toUser = await toBuilder.BuildInDatabase(_context);

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

    [Fact]
    public async Task TransferCreditsAsync_SameUser_ThrowsArgumentException()
    {
        // Arrange
        var builder = new CreditTestBuilder()
            .WithName("Alice")
            .WithEmail("alice@test.com")
            .WithTimeCredits(10);

        var user = await builder.BuildInDatabase(_context);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _creditService.TransferCreditsAsync(user.Id, user.Id, 5, "Self transfer"));

        Assert.Contains("Cannot transfer credits to yourself", exception.Message);
    }

    [Fact]
    public async Task TransferCreditsAsync_ZeroAmount_ThrowsArgumentException()
    {
        // Arrange
        var fromBuilder = new CreditTestBuilder()
            .WithName("Alice")
            .WithEmail("alice@test.com")
            .WithTimeCredits(10);
        var toBuilder = new CreditTestBuilder()
            .WithName("Bob")
            .WithEmail("bob@test.com")
            .WithTimeCredits(5);

        var fromUser = await fromBuilder.BuildInDatabase(_context);
        var toUser = await toBuilder.BuildInDatabase(_context);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _creditService.TransferCreditsAsync(fromUser.Id, toUser.Id, 0, "Zero transfer"));

        Assert.Equal("Amount must be positive (Parameter 'amount')", exception.Message);
    }

    [Fact]
    public async Task TransferCreditsAsync_NonExistentFromUser_ThrowsInvalidOperationException()
    {
        // Arrange
        var toBuilder = new CreditTestBuilder()
            .WithName("Bob")
            .WithEmail("bob@test.com")
            .WithTimeCredits(5);

        var toUser = await toBuilder.BuildInDatabase(_context);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _creditService.TransferCreditsAsync(999, toUser.Id, 5, "Transfer from non-existent user"));

        Assert.Contains("One or both users not found", exception.Message);
    }

    [Fact]
    public async Task TransferCreditsAsync_NonExistentToUser_ThrowsInvalidOperationException()
    {
        // Arrange
        var fromBuilder = new CreditTestBuilder()
            .WithName("Alice")
            .WithEmail("alice@test.com")
            .WithTimeCredits(10);

        var fromUser = await fromBuilder.BuildInDatabase(_context);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _creditService.TransferCreditsAsync(fromUser.Id, 999, 5, "Transfer to non-existent user"));

        Assert.Contains("One or both users not found", exception.Message);
    }

    [Fact]
    public async Task TransferCreditsAsync_WithExchangeId_RecordsExchangeId()
    {
        // Arrange
        var fromBuilder = new CreditTestBuilder()
            .WithName("Alice")
            .WithEmail("alice@test.com")
            .WithTimeCredits(10);
        var toBuilder = new CreditTestBuilder()
            .WithName("Bob")
            .WithEmail("bob@test.com")
            .WithTimeCredits(5);

        var fromUser = await fromBuilder.BuildInDatabase(_context);
        var toUser = await toBuilder.BuildInDatabase(_context);

        const int exchangeId = 42;

        // Act
        var result = await _creditService.TransferCreditsAsync(fromUser.Id, toUser.Id, 3, "Exchange completion", exchangeId);

        // Assert
        Assert.True(result);

        var transactions = await _context.CreditTransactions
            .Where(t => t.ExchangeId == exchangeId)
            .ToListAsync();

        Assert.Equal(2, transactions.Count); // Should have both debit and credit transactions
        Assert.Contains(transactions, t => t.UserId == fromUser.Id && t.Amount == -3);
        Assert.Contains(transactions, t => t.UserId == toUser.Id && t.Amount == 3);
    }

    [Fact]
    public async Task TransferCreditsAsync_CreatesTransactionRecords()
    {
        // Arrange
        var fromBuilder = new CreditTestBuilder()
            .WithName("Alice")
            .WithEmail("alice@test.com")
            .WithTimeCredits(10);
        var toBuilder = new CreditTestBuilder()
            .WithName("Bob")
            .WithEmail("bob@test.com")
            .WithTimeCredits(5);

        var fromUser = await fromBuilder.BuildInDatabase(_context);
        var toUser = await toBuilder.BuildInDatabase(_context);

        // Act
        await _creditService.TransferCreditsAsync(fromUser.Id, toUser.Id, 3, "Test transfer");

        // Assert
        var fromTransaction = await _context.CreditTransactions
            .FirstOrDefaultAsync(t => t.UserId == fromUser.Id);
        var toTransaction = await _context.CreditTransactions
            .FirstOrDefaultAsync(t => t.UserId == toUser.Id);

        Assert.NotNull(fromTransaction);
        Assert.NotNull(toTransaction);

        // Verify from transaction
        Assert.Equal(-3, fromTransaction.Amount);
        Assert.Equal(7, fromTransaction.BalanceAfter); // 10 - 3 = 7
        Assert.Equal("ExchangeComplete", fromTransaction.TransactionType);
        Assert.Equal("Test transfer", fromTransaction.Reason);
        Assert.Equal(toUser.Id, fromTransaction.RelatedUserId);

        // Verify to transaction
        Assert.Equal(3, toTransaction.Amount);
        Assert.Equal(8, toTransaction.BalanceAfter); // 5 + 3 = 8
        Assert.Equal("ExchangeComplete", toTransaction.TransactionType);
        Assert.Equal("Test transfer", toTransaction.Reason);
        Assert.Equal(fromUser.Id, toTransaction.RelatedUserId);
    }

    [Fact]
    public async Task AddCreditsAsync_NegativeAmount_ThrowsArgumentException()
    {
        // Arrange
        var builder = new CreditTestBuilder()
            .WithName("Alice")
            .WithEmail("alice@test.com")
            .WithTimeCredits(10);

        var user = await builder.BuildInDatabase(_context);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _creditService.AddCreditsAsync(user.Id, -5, "Negative credit addition"));

        Assert.Equal("Amount must be positive (Parameter 'amount')", exception.Message);
    }

    [Fact]
    public async Task AddCreditsAsync_NonExistentUser_ThrowsInvalidOperationException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _creditService.AddCreditsAsync(999, 5, "Add to non-existent user"));

        Assert.Contains("User not found", exception.Message);
    }

    [Fact]
    public async Task AddCreditsAsync_CreatesTransactionRecord()
    {
        // Arrange
        var builder = new CreditTestBuilder()
            .WithName("Alice")
            .WithEmail("alice@test.com")
            .WithTimeCredits(10);

        var user = await builder.BuildInDatabase(_context);

        // Act
        await _creditService.AddCreditsAsync(user.Id, 5, "Bonus credits");

        // Assert
        var transaction = await _context.CreditTransactions
            .FirstOrDefaultAsync(t => t.UserId == user.Id);

        Assert.NotNull(transaction);
        Assert.Equal(5, transaction.Amount);
        Assert.Equal(15, transaction.BalanceAfter); // 10 + 5 = 15
        Assert.Equal("AdminAdjustment", transaction.TransactionType);
        Assert.Equal("Bonus credits", transaction.Reason);
        Assert.Null(transaction.RelatedUserId);
        Assert.Null(transaction.ExchangeId);
    }

    [Fact]
    public async Task DeductCreditsAsync_ValidAmount_DecreasesBalance()
    {
        // Arrange
        var builder = new CreditTestBuilder()
            .WithName("Alice")
            .WithEmail("alice@test.com")
            .WithTimeCredits(10);

        var user = await builder.BuildInDatabase(_context);

        // Act
        var result = await _creditService.DeductCreditsAsync(user.Id, 3, "Penalty deduction");

        // Assert
        Assert.True(result);
        
        var updatedUser = await _context.Users.FindAsync(user.Id);
        Assert.Equal(7, updatedUser!.TimeCredits); // 10 - 3 = 7
    }

    [Fact]
    public async Task DeductCreditsAsync_InsufficientFunds_ThrowsInvalidOperationException()
    {
        // Arrange
        var builder = new CreditTestBuilder()
            .WithName("Alice")
            .WithEmail("alice@test.com")
            .WithTimeCredits(2);

        var user = await builder.BuildInDatabase(_context);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _creditService.DeductCreditsAsync(user.Id, 5, "Overdraft attempt"));

        Assert.Contains("Insufficient credits", exception.Message);
    }

    [Fact]
    public async Task DeductCreditsAsync_NegativeAmount_ThrowsArgumentException()
    {
        // Arrange
        var builder = new CreditTestBuilder()
            .WithName("Alice")
            .WithEmail("alice@test.com")
            .WithTimeCredits(10);

        var user = await builder.BuildInDatabase(_context);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _creditService.DeductCreditsAsync(user.Id, -3, "Negative deduction"));

        Assert.Equal("Amount must be positive (Parameter 'amount')", exception.Message);
    }

    [Fact]
    public async Task DeductCreditsAsync_NonExistentUser_ThrowsInvalidOperationException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _creditService.DeductCreditsAsync(999, 5, "Deduct from non-existent user"));

        Assert.Contains("User not found", exception.Message);
    }

    [Fact]
    public async Task DeductCreditsAsync_CreatesTransactionRecord()
    {
        // Arrange
        var builder = new CreditTestBuilder()
            .WithName("Alice")
            .WithEmail("alice@test.com")
            .WithTimeCredits(10);

        var user = await builder.BuildInDatabase(_context);

        // Act
        await _creditService.DeductCreditsAsync(user.Id, 3, "Penalty deduction");

        // Assert
        var transaction = await _context.CreditTransactions
            .FirstOrDefaultAsync(t => t.UserId == user.Id);

        Assert.NotNull(transaction);
        Assert.Equal(-3, transaction.Amount);
        Assert.Equal(7, transaction.BalanceAfter); // 10 - 3 = 7
        Assert.Equal("AdminAdjustment", transaction.TransactionType);
        Assert.Equal("Penalty deduction", transaction.Reason);
        Assert.Null(transaction.RelatedUserId);
        Assert.Null(transaction.ExchangeId);
    }

    [Fact]
    public async Task GetUserCreditHistoryAsync_WithTransactions_ReturnsOrderedHistory()
    {
        // Arrange
        var builder = new CreditTestBuilder()
            .WithName("Alice")
            .WithEmail("alice@test.com")
            .WithTimeCredits(10);

        var user = await builder.BuildInDatabase(_context);

        // Create some transactions
        await _creditService.AddCreditsAsync(user.Id, 5, "First addition");
        await Task.Delay(10); // Ensure different timestamps
        await _creditService.DeductCreditsAsync(user.Id, 3, "First deduction");
        await Task.Delay(10);
        await _creditService.AddCreditsAsync(user.Id, 2, "Second addition");

        // Act
        var history = await _creditService.GetUserCreditHistoryAsync(user.Id);

        // Assert
        var transactions = history.ToList();
        Assert.Equal(3, transactions.Count);

        // Should be ordered by CreatedAt descending (most recent first)
        Assert.Equal("Second addition", transactions[0].Reason);
        Assert.Equal("First deduction", transactions[1].Reason);
        Assert.Equal("First addition", transactions[2].Reason);
    }

    [Fact]
    public async Task GetUserCreditHistoryAsync_WithLimit_RespectsLimit()
    {
        // Arrange
        var builder = new CreditTestBuilder()
            .WithName("Alice")
            .WithEmail("alice@test.com")
            .WithTimeCredits(100);

        var user = await builder.BuildInDatabase(_context);

        // Create 5 transactions
        for (int i = 0; i < 5; i++)
        {
            await _creditService.AddCreditsAsync(user.Id, 1, $"Transaction {i}");
            await Task.Delay(5); // Ensure different timestamps
        }

        // Act
        var history = await _creditService.GetUserCreditHistoryAsync(user.Id, limit: 3);

        // Assert
        Assert.Equal(3, history.Count());
    }

    [Fact]
    public async Task GetUserCreditHistoryAsync_NonExistentUser_ReturnsEmpty()
    {
        // Act
        var history = await _creditService.GetUserCreditHistoryAsync(999);

        // Assert
        Assert.Empty(history);
    }

    [Fact]
    public async Task TransferCreditsAsync_ExactBalance_WorksCorrectly()
    {
        // Arrange
        var fromBuilder = new CreditTestBuilder()
            .WithName("Alice")
            .WithEmail("alice@test.com")
            .WithTimeCredits(5); // Exact amount to transfer
        var toBuilder = new CreditTestBuilder()
            .WithName("Bob")
            .WithEmail("bob@test.com")
            .WithTimeCredits(0);

        var fromUser = await fromBuilder.BuildInDatabase(_context);
        var toUser = await toBuilder.BuildInDatabase(_context);

        // Act
        var result = await _creditService.TransferCreditsAsync(fromUser.Id, toUser.Id, 5, "Transfer all credits");

        // Assert
        Assert.True(result);
        
        var updatedFromUser = await _context.Users.FindAsync(fromUser.Id);
        var updatedToUser = await _context.Users.FindAsync(toUser.Id);

        Assert.Equal(0, updatedFromUser!.TimeCredits); // Should be exactly 0
        Assert.Equal(5, updatedToUser!.TimeCredits);   // Should receive all 5
    }

    [Fact]
    public async Task DeductCreditsAsync_ExactBalance_WorksCorrectly()
    {
        // Arrange
        var builder = new CreditTestBuilder()
            .WithName("Alice")
            .WithEmail("alice@test.com")
            .WithTimeCredits(5);

        var user = await builder.BuildInDatabase(_context);

        // Act
        var result = await _creditService.DeductCreditsAsync(user.Id, 5, "Deduct all credits");

        // Assert
        Assert.True(result);
        
        var updatedUser = await _context.Users.FindAsync(user.Id);
        Assert.Equal(0, updatedUser!.TimeCredits); // Should be exactly 0
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}