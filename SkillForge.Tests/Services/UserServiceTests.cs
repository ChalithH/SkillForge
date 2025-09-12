using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SkillForge.Api.Data;
using SkillForge.Api.Models;
using SkillForge.Api.Services;
using Xunit;

namespace SkillForge.Tests.Services;

public class UserServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<ILogger<UserService>> _mockLogger;
    private readonly UserService _userService;

    public UserServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _mockLogger = new Mock<ILogger<UserService>>();
        _userService = new UserService(_context, _mockLogger.Object);

        SeedTestData();
    }

    private void SeedTestData()
    {
        var users = new List<User>
        {
            new User
            {
                Id = 1,
                Email = "user1@test.com",
                Name = "User One",
                PasswordHash = "hash1",
                TimeCredits = 10,
                Bio = "Bio for user 1",
                ProfileImageUrl = "http://example.com/user1.jpg",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new User
            {
                Id = 2,
                Email = "user2@test.com",
                Name = "User Two",
                PasswordHash = "hash2",
                TimeCredits = 5,
                Bio = "Bio for user 2",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        var skills = new List<Skill>
        {
            new Skill { Id = 1, Name = "JavaScript", Category = "Programming", Description = "JS programming" },
            new Skill { Id = 2, Name = "Python", Category = "Programming", Description = "Python programming" }
        };

        var userSkills = new List<UserSkill>
        {
            new UserSkill { Id = 1, UserId = 1, SkillId = 1, ProficiencyLevel = 4, IsOffering = true, Description = "Expert in JS" },
            new UserSkill { Id = 2, UserId = 1, SkillId = 2, ProficiencyLevel = 2, IsOffering = false, Description = "Learning Python" }
        };

        var reviews = new List<Review>
        {
            new Review
            {
                Id = 1,
                ReviewerId = 2,
                ReviewedUserId = 1,
                ExchangeId = 1,
                Rating = 5,
                Comment = "Great teacher!",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        _context.Users.AddRange(users);
        _context.Skills.AddRange(skills);
        _context.UserSkills.AddRange(userSkills);
        _context.Reviews.AddRange(reviews);
        _context.SaveChanges();
    }

    [Fact]
    public async Task GetUserByIdAsync_ExistingUser_ReturnsUserWithRelatedData()
    {
        // Act
        var result = await _userService.GetUserByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("User One", result.Name);
        Assert.Equal("user1@test.com", result.Email);
        Assert.NotNull(result.UserSkills);
        Assert.Equal(2, result.UserSkills.Count);
        Assert.NotNull(result.ReviewsReceived);
        Assert.Single(result.ReviewsReceived);
    }

    [Fact]
    public async Task GetUserByIdAsync_NonExistingUser_ReturnsNull()
    {
        // Act
        var result = await _userService.GetUserByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetUserByEmailAsync_ExistingEmail_ReturnsUser()
    {
        // Act
        var result = await _userService.GetUserByEmailAsync("user1@test.com");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("User One", result.Name);
        Assert.Equal(1, result.Id);
    }

    [Fact]
    public async Task GetUserByEmailAsync_NonExistingEmail_ReturnsNull()
    {
        // Act
        var result = await _userService.GetUserByEmailAsync("nonexistent@test.com");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllUsersAsync_ReturnsAllUsersOrderedByName()
    {
        // Act
        var result = await _userService.GetAllUsersAsync();

        // Assert
        var users = result.ToList();
        Assert.Equal(2, users.Count);
        Assert.Equal("User One", users[0].Name);
        Assert.Equal("User Two", users[1].Name);
    }

    [Fact]
    public async Task UpdateUserAsync_ExistingUser_UpdatesAllowedFieldsOnly()
    {
        // Arrange
        var updatedUser = new User
        {
            Name = "Updated Name",
            Bio = "Updated Bio",
            ProfileImageUrl = "http://example.com/updated.jpg",
            Email = "should.not.update@test.com", // Should not update
            PasswordHash = "should_not_update", // Should not update
            TimeCredits = 999 // Should not update
        };

        // Act
        var result = await _userService.UpdateUserAsync(1, updatedUser);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated Name", result.Name);
        Assert.Equal("Updated Bio", result.Bio);
        Assert.Equal("http://example.com/updated.jpg", result.ProfileImageUrl);
        Assert.Equal("user1@test.com", result.Email); // Should not change
        Assert.Equal("hash1", result.PasswordHash); // Should not change
        Assert.Equal(10, result.TimeCredits); // Should not change
        Assert.True(result.UpdatedAt > result.CreatedAt);
    }

    [Fact]
    public async Task UpdateUserAsync_NonExistingUser_ReturnsNull()
    {
        // Arrange
        var updatedUser = new User
        {
            Name = "Updated Name",
            Bio = "Updated Bio"
        };

        // Act
        var result = await _userService.UpdateUserAsync(999, updatedUser);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteUserAsync_ExistingUser_DeletesAndReturnsTrue()
    {
        // Act
        var result = await _userService.DeleteUserAsync(2);

        // Assert
        Assert.True(result);
        
        var userInDb = await _context.Users.FindAsync(2);
        Assert.Null(userInDb);
    }

    [Fact]
    public async Task DeleteUserAsync_NonExistingUser_ReturnsFalse()
    {
        // Act
        var result = await _userService.DeleteUserAsync(999);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetUserSkillsAsync_ExistingUser_ReturnsUserSkills()
    {
        // Act
        var result = await _userService.GetUserSkillsAsync(1);

        // Assert
        var userSkills = result.ToList();
        Assert.Equal(2, userSkills.Count);
        Assert.All(userSkills, us => Assert.NotNull(us.Skill));
    }

    [Fact]
    public async Task GetUserSkillsAsync_UserWithNoSkills_ReturnsEmptyList()
    {
        // Act
        var result = await _userService.GetUserSkillsAsync(2);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task AddUserSkillAsync_ValidUserAndSkill_AddsAndReturnsUserSkill()
    {
        // Arrange
        var newUserSkill = new UserSkill
        {
            SkillId = 2,
            ProficiencyLevel = 3,
            IsOffering = true,
            Description = "New skill"
        };

        // Act
        var result = await _userService.AddUserSkillAsync(2, newUserSkill);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.UserId);
        Assert.Equal(2, result.SkillId);
        Assert.Equal(3, result.ProficiencyLevel);
        Assert.True(result.IsOffering);
        Assert.NotNull(result.Skill);
        Assert.Equal("Python", result.Skill.Name);
    }

    [Fact]
    public async Task AddUserSkillAsync_UserNotFound_ReturnsNull()
    {
        // Arrange
        var newUserSkill = new UserSkill
        {
            SkillId = 1,
            ProficiencyLevel = 3,
            IsOffering = true
        };

        // Act
        var result = await _userService.AddUserSkillAsync(999, newUserSkill);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task AddUserSkillAsync_SkillNotFound_ReturnsNull()
    {
        // Arrange
        var newUserSkill = new UserSkill
        {
            SkillId = 999,
            ProficiencyLevel = 3,
            IsOffering = true
        };

        // Act
        var result = await _userService.AddUserSkillAsync(1, newUserSkill);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task AddUserSkillAsync_DuplicateSkill_UpdatesExisting()
    {
        // Arrange
        var duplicateUserSkill = new UserSkill
        {
            SkillId = 1, // User 1 already has skill 1
            ProficiencyLevel = 5,
            IsOffering = false,
            Description = "Updated description"
        };

        // Act
        var result = await _userService.AddUserSkillAsync(1, duplicateUserSkill);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id); // Should be the same ID
        Assert.Equal(5, result.ProficiencyLevel); // Should be updated
        Assert.False(result.IsOffering); // Should be updated
        Assert.Equal("Updated description", result.Description); // Should be updated
    }

    [Fact]
    public async Task UpdateUserSkillAsync_ExistingUserSkill_UpdatesAndReturns()
    {
        // Arrange
        var updatedUserSkill = new UserSkill
        {
            ProficiencyLevel = 5,
            IsOffering = false,
            Description = "Now an expert"
        };

        // Act
        var result = await _userService.UpdateUserSkillAsync(1, 1, updatedUserSkill);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.ProficiencyLevel);
        Assert.False(result.IsOffering);
        Assert.Equal("Now an expert", result.Description);
        Assert.NotNull(result.Skill);
    }

    [Fact]
    public async Task UpdateUserSkillAsync_NonExistingUserSkill_ReturnsNull()
    {
        // Arrange
        var updatedUserSkill = new UserSkill
        {
            ProficiencyLevel = 5,
            IsOffering = false
        };

        // Act
        var result = await _userService.UpdateUserSkillAsync(1, 999, updatedUserSkill);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateUserSkillAsync_WrongUser_ReturnsNull()
    {
        // Arrange
        var updatedUserSkill = new UserSkill
        {
            ProficiencyLevel = 5,
            IsOffering = false
        };

        // Act - Try to update user 1's skill with user 2's ID
        var result = await _userService.UpdateUserSkillAsync(2, 1, updatedUserSkill);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteUserSkillAsync_ExistingUserSkill_DeletesAndReturnsTrue()
    {
        // Act
        var result = await _userService.DeleteUserSkillAsync(1, 1);

        // Assert
        Assert.True(result);
        
        var userSkillInDb = await _context.UserSkills.FindAsync(1);
        Assert.Null(userSkillInDb);
    }

    [Fact]
    public async Task DeleteUserSkillAsync_NonExistingUserSkill_ReturnsFalse()
    {
        // Act
        var result = await _userService.DeleteUserSkillAsync(1, 999);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteUserSkillAsync_WrongUser_ReturnsFalse()
    {
        // Act - Try to delete user 1's skill with user 2's ID
        var result = await _userService.DeleteUserSkillAsync(2, 1);

        // Assert
        Assert.False(result);
        
        // Verify skill still exists
        var userSkillInDb = await _context.UserSkills.FindAsync(1);
        Assert.NotNull(userSkillInDb);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}