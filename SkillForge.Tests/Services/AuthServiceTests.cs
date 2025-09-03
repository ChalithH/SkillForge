using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using SkillForge.Api.Data;
using SkillForge.Api.DTOs.Auth;
using SkillForge.Api.Models;
using SkillForge.Api.Services;
using System.IdentityModel.Tokens.Jwt;

namespace SkillForge.Tests.Services;

public class AuthTestBuilder
{
    private string _email = "test@example.com";
    private string _password = "password123";
    private string _name = "Test User";
    private string? _bio = null;
    private int _timeCredits = 5;
    private DateTime? _createdAt = null;

    public AuthTestBuilder WithEmail(string email)
    {
        _email = email;
        return this;
    }

    public AuthTestBuilder WithPassword(string password)
    {
        _password = password;
        return this;
    }

    public AuthTestBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public AuthTestBuilder WithBio(string bio)
    {
        _bio = bio;
        return this;
    }

    public AuthTestBuilder WithTimeCredits(int credits)
    {
        _timeCredits = credits;
        return this;
    }

    public AuthTestBuilder CreatedAt(DateTime createdAt)
    {
        _createdAt = createdAt;
        return this;
    }

    public async Task<User> BuildInDatabase(ApplicationDbContext context)
    {
        var user = new User
        {
            Email = _email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(_password),
            Name = _name,
            Bio = _bio,
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
            Email = _email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(_password),
            Name = _name,
            Bio = _bio,
            TimeCredits = _timeCredits,
            CreatedAt = _createdAt ?? DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public string GetPlainTextPassword() => _password;
    public string GetEmail() => _email;
    public string GetName() => _name;
}

public class AuthServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly AuthService _authService;
    private readonly IConfiguration _configuration;

    public AuthServiceTests()
    {
        // Create in-memory database for testing with transaction warnings ignored
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new ApplicationDbContext(options);
        
        // Create configuration for JWT settings
        var configurationData = new Dictionary<string, string>
        {
            ["JwtSettings:SecretKey"] = "test-secret-key-for-authentication-testing-purposes-must-be-long-enough",
            ["JwtSettings:ExpirationInHours"] = "24"
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationData!)
            .Build();
        
        _authService = new AuthService(_context, _configuration);
    }

    [Fact]
    public async Task RegisterAsync_ValidUser_ReturnsAuthResponse()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Email = "test@example.com",
            Password = "password123",
            Name = "Test User",
            Bio = "Test bio"
        };

        // Act
        var result = await _authService.RegisterAsync(registerDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(registerDto.Email, result.Email);
        Assert.Equal(registerDto.Name, result.Name);
        Assert.Equal(registerDto.Bio, result.Bio);
        Assert.Equal(5, result.TimeCredits); // New users start with 5 credits
        Assert.NotNull(result.Token);
        Assert.NotEmpty(result.Token);
        
        // Verify user was created in database
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == registerDto.Email);
        Assert.NotNull(user);
        Assert.True(BCrypt.Net.BCrypt.Verify(registerDto.Password, user.PasswordHash));
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_ReturnsNull()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Email = "duplicate@example.com",
            Password = "password123",
            Name = "Test User"
        };

        // First registration
        await _authService.RegisterAsync(registerDto);

        // Act - Try to register again with same email
        var result = await _authService.RegisterAsync(registerDto);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsAuthResponse()
    {
        // Arrange
        var builder = new AuthTestBuilder()
            .WithEmail("login@example.com")
            .WithPassword("password123")
            .WithName("Login User");

        await builder.BuildInDatabase(_context);

        var loginDto = new LoginDto
        {
            Email = builder.GetEmail(),
            Password = builder.GetPlainTextPassword()
        };

        // Act
        var result = await _authService.LoginAsync(loginDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(builder.GetEmail(), result.Email);
        Assert.Equal(builder.GetName(), result.Name);
        Assert.Equal(5, result.TimeCredits);
        Assert.NotNull(result.Token);
        Assert.NotEmpty(result.Token);
    }

    [Fact]
    public async Task LoginAsync_InvalidCredentials_ReturnsNull()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Email = "nonexistent@example.com",
            Password = "wrongpassword"
        };

        // Act
        var result = await _authService.LoginAsync(loginDto);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ReturnsNull()
    {
        // Arrange
        var builder = new AuthTestBuilder()
            .WithEmail("user@example.com")
            .WithPassword("correctpassword")
            .WithName("Test User");

        await builder.BuildInDatabase(_context);

        var loginDto = new LoginDto
        {
            Email = builder.GetEmail(),
            Password = "wrongpassword"
        };

        // Act
        var result = await _authService.LoginAsync(loginDto);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GenerateJwtToken_ValidUser_ReturnsValidToken()
    {
        // Arrange
        var user = new User
        {
            Id = 123,
            Email = "token@example.com",
            Name = "Token User",
            PasswordHash = "hash",
            TimeCredits = 10,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        var token = _authService.GenerateJwtToken(user);

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);

        // Verify token content
        var tokenHandler = new JwtSecurityTokenHandler();
        var jsonToken = tokenHandler.ReadJwtToken(token);

        var userIdClaim = jsonToken.Claims.FirstOrDefault(x => x.Type == "nameid");
        var emailClaim = jsonToken.Claims.FirstOrDefault(x => x.Type == "email");
        var nameClaim = jsonToken.Claims.FirstOrDefault(x => x.Type == "unique_name");

        Assert.NotNull(userIdClaim);
        Assert.NotNull(emailClaim);
        Assert.NotNull(nameClaim);
        Assert.Equal("123", userIdClaim.Value);
        Assert.Equal(user.Email, emailClaim.Value);
        Assert.Equal(user.Name, nameClaim.Value);

        // Verify expiration (24 hours)
        var expectedExpiry = DateTime.UtcNow.AddHours(24);
        var actualExpiry = jsonToken.ValidTo;
        Assert.True(Math.Abs((expectedExpiry - actualExpiry).TotalMinutes) < 1);
    }

    [Fact]
    public async Task UpdateProfileAsync_ValidUser_UpdatesAndReturnsUser()
    {
        // Arrange
        var builder = new AuthTestBuilder()
            .WithEmail("update@example.com")
            .WithPassword("password123")
            .WithName("Original Name")
            .WithBio("Original bio");

        var user = await builder.BuildInDatabase(_context);
        
        var updateDto = new UpdateProfileDto
        {
            Name = "Updated Name",
            Bio = "Updated bio"
        };

        // Act
        var result = await _authService.UpdateProfileAsync(user.Id, updateDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(updateDto.Name, result.Name);
        Assert.Equal(updateDto.Bio, result.Bio);
        Assert.Equal(builder.GetEmail(), result.Email); // Email should remain unchanged
        
        // Verify changes were persisted
        var userFromDb = await _context.Users.FindAsync(user.Id);
        Assert.NotNull(userFromDb);
        Assert.Equal(updateDto.Name, userFromDb.Name);
        Assert.Equal(updateDto.Bio, userFromDb.Bio);
    }

    [Fact]
    public async Task UpdateProfileAsync_NonexistentUser_ReturnsNull()
    {
        // Arrange
        var updateDto = new UpdateProfileDto
        {
            Name = "Updated Name",
            Bio = "Updated bio"
        };

        // Act
        var result = await _authService.UpdateProfileAsync(999, updateDto);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetUserByIdAsync_ExistingUser_ReturnsUser()
    {
        // Arrange
        var builder = new AuthTestBuilder()
            .WithEmail("getuser@example.com")
            .WithPassword("password123")
            .WithName("Get User");

        var user = await builder.BuildInDatabase(_context);

        // Act
        var result = await _authService.GetUserByIdAsync(user.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(builder.GetEmail(), result.Email);
        Assert.Equal(builder.GetName(), result.Name);
    }

    [Fact]
    public async Task GetUserByIdAsync_NonexistentUser_ReturnsNull()
    {
        // Act
        var result = await _authService.GetUserByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}