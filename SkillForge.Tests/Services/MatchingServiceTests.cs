using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SkillForge.Api.Data;
using SkillForge.Api.DTOs;
using SkillForge.Api.Models;
using SkillForge.Api.Services;
using Xunit;

namespace SkillForge.Tests.Services;

public class MatchingServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IUserPresenceService> _mockUserPresenceService;
    private readonly Mock<ILogger<MatchingService>> _mockLogger;
    private readonly MatchingService _matchingService;

    public MatchingServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _mockUserPresenceService = new Mock<IUserPresenceService>();
        _mockLogger = new Mock<ILogger<MatchingService>>();
        _matchingService = new MatchingService(_context, _mockUserPresenceService.Object, _mockLogger.Object);

        SeedTestData();
    }

    private void SeedTestData()
    {
        var users = new List<User>
        {
            new User
            {
                Id = 1,
                Email = "alice@test.com",
                Name = "Alice",
                PasswordHash = "hash",
                TimeCredits = 10,
                Bio = "Software developer",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new User
            {
                Id = 2,
                Email = "bob@test.com",
                Name = "Bob",
                PasswordHash = "hash",
                TimeCredits = 15,
                Bio = "Web designer",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new User
            {
                Id = 3,
                Email = "charlie@test.com",
                Name = "Charlie",
                PasswordHash = "hash",
                TimeCredits = 5,
                Bio = "Music teacher",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new User
            {
                Id = 4,
                Email = "diana@test.com",
                Name = "Diana",
                PasswordHash = "hash",
                TimeCredits = 20,
                Bio = "Language tutor",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        var skills = new List<Skill>
        {
            new Skill { Id = 1, Name = "JavaScript", Category = "Programming", Description = "JS programming" },
            new Skill { Id = 2, Name = "Python", Category = "Programming", Description = "Python programming" },
            new Skill { Id = 3, Name = "Guitar", Category = "Music", Description = "Guitar lessons" },
            new Skill { Id = 4, Name = "Spanish", Category = "Languages", Description = "Spanish language" }
        };

        var userSkills = new List<UserSkill>
        {
            // Alice offers JavaScript, wants Python
            new UserSkill { Id = 1, UserId = 1, SkillId = 1, ProficiencyLevel = 4, IsOffering = true },
            new UserSkill { Id = 2, UserId = 1, SkillId = 2, ProficiencyLevel = 1, IsOffering = false },
            
            // Bob offers Python, wants JavaScript
            new UserSkill { Id = 3, UserId = 2, SkillId = 2, ProficiencyLevel = 5, IsOffering = true },
            new UserSkill { Id = 4, UserId = 2, SkillId = 1, ProficiencyLevel = 2, IsOffering = false },
            
            // Charlie offers Guitar
            new UserSkill { Id = 5, UserId = 3, SkillId = 3, ProficiencyLevel = 3, IsOffering = true },
            
            // Diana offers Spanish and Python
            new UserSkill { Id = 6, UserId = 4, SkillId = 4, ProficiencyLevel = 5, IsOffering = true },
            new UserSkill { Id = 7, UserId = 4, SkillId = 2, ProficiencyLevel = 3, IsOffering = true }
        };

        var reviews = new List<Review>
        {
            // Bob has high ratings
            new Review { Id = 1, ReviewerId = 1, ReviewedUserId = 2, ExchangeId = 1, Rating = 5, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Review { Id = 2, ReviewerId = 3, ReviewedUserId = 2, ExchangeId = 2, Rating = 4, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            
            // Diana has one review
            new Review { Id = 3, ReviewerId = 1, ReviewedUserId = 4, ExchangeId = 3, Rating = 5, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        };

        _context.Users.AddRange(users);
        _context.Skills.AddRange(skills);
        _context.UserSkills.AddRange(userSkills);
        _context.Reviews.AddRange(reviews);
        _context.SaveChanges();

        // Setup mock presence service
        _mockUserPresenceService.Setup(x => x.IsUserOnlineAsync(1)).ReturnsAsync(true);
        _mockUserPresenceService.Setup(x => x.IsUserOnlineAsync(2)).ReturnsAsync(true);
        _mockUserPresenceService.Setup(x => x.IsUserOnlineAsync(3)).ReturnsAsync(false);
        _mockUserPresenceService.Setup(x => x.IsUserOnlineAsync(4)).ReturnsAsync(false);
    }

    [Fact]
    public async Task BrowseUsersAsync_NoFilters_ReturnsAllUsersExceptCurrent()
    {
        // Act
        var result = await _matchingService.BrowseUsersAsync(1);

        // Assert
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(3, result.Items.Count());
        Assert.All(result.Items, item => Assert.NotEqual(1, item.Id));
    }

    [Fact]
    public async Task BrowseUsersAsync_WithCategoryFilter_ReturnsUsersInCategory()
    {
        // Act
        var result = await _matchingService.BrowseUsersAsync(1, category: "Programming");

        // Assert
        Assert.Equal(2, result.Items.Count()); // Bob and Diana offer Programming skills
        Assert.Contains(result.Items, u => u.Id == 2);
        Assert.Contains(result.Items, u => u.Id == 4);
    }

    [Fact]
    public async Task BrowseUsersAsync_WithSkillNameFilter_ReturnsUsersWithSkill()
    {
        // Act
        var result = await _matchingService.BrowseUsersAsync(1, skillName: "Python");

        // Assert
        Assert.Equal(2, result.Items.Count()); // Bob and Diana offer Python
        Assert.All(result.Items, item => 
            Assert.Contains(item.Skills, s => s.Skill.Name.Contains("Python")));
    }

    [Fact]
    public async Task BrowseUsersAsync_WithMinRatingFilter_ReturnsUsersAboveRating()
    {
        // Act
        var result = await _matchingService.BrowseUsersAsync(1, minRating: 4.5);

        // Assert
        // Bob (rating 4.5) and Diana (rating 5.0) should match
        Assert.Equal(2, result.Items.Count());
        Assert.Contains(result.Items, u => u.Id == 2); // Bob
        Assert.Contains(result.Items, u => u.Id == 4); // Diana
    }

    [Fact]
    public async Task BrowseUsersAsync_WithOnlineFilter_ReturnsOnlineUsers()
    {
        // Act
        var result = await _matchingService.BrowseUsersAsync(1, isOnline: true);

        // Assert
        Assert.Single(result.Items); // Only Bob is online (besides current user)
        Assert.Equal(2, result.Items.First().Id);
    }

    [Fact]
    public async Task BrowseUsersAsync_WithPagination_ReturnsPaginatedResults()
    {
        // Act
        var page1 = await _matchingService.BrowseUsersAsync(1, page: 1, limit: 2);
        var page2 = await _matchingService.BrowseUsersAsync(1, page: 2, limit: 2);

        // Assert
        Assert.Equal(3, page1.TotalCount);
        Assert.Equal(2, page1.Items.Count());
        Assert.Equal(1, page1.Page);
        Assert.Equal(2, page1.PageSize);
        Assert.Equal(2, page1.TotalPages);

        Assert.Single(page2.Items);
        Assert.Equal(2, page2.Page);
    }

    [Fact]
    public async Task BrowseUsersAsync_LimitCappedAt50()
    {
        // Act
        var result = await _matchingService.BrowseUsersAsync(1, limit: 100);

        // Assert
        Assert.Equal(50, result.PageSize);
    }

    [Fact]
    public async Task GetUserMatchDetailsAsync_ExistingUser_ReturnsUserDetails()
    {
        // Act
        var result = await _matchingService.GetUserMatchDetailsAsync(2, 1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Id);
        Assert.Equal("Bob", result.Name);
        Assert.Equal(4.5, result.Rating);
        Assert.Equal(2, result.ReviewCount);
        Assert.True(result.IsOnline);
        Assert.NotEmpty(result.Skills);
    }

    [Fact]
    public async Task GetUserMatchDetailsAsync_NonExistingUser_ReturnsNull()
    {
        // Act
        var result = await _matchingService.GetUserMatchDetailsAsync(999, 1);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetRecommendedMatchesAsync_UserWithLearningInterests_ReturnsMatches()
    {
        // Act - User 1 wants to learn Python
        var result = await _matchingService.GetRecommendedMatchesAsync(1, limit: 10);

        // Assert
        var matches = result.ToList();
        Assert.Equal(2, matches.Count); // Bob and Diana offer Python
        Assert.All(matches, m => 
            Assert.Contains(m.Skills, s => s.SkillId == 2 && s.IsOffering));
    }

    [Fact]
    public async Task GetRecommendedMatchesAsync_UserWithNoInterests_ReturnsTopRatedUsers()
    {
        // Act - User 3 has no learning interests
        var result = await _matchingService.GetRecommendedMatchesAsync(3, limit: 10);

        // Assert
        var matches = result.ToList();
        Assert.NotEmpty(matches);
        // Should return users ordered by rating
        Assert.Equal(4, matches.First().Id); // Diana has highest rating
    }

    [Fact]
    public async Task GetRecommendedMatchesAsync_LimitWorks()
    {
        // Act
        var result = await _matchingService.GetRecommendedMatchesAsync(1, limit: 1);

        // Assert
        Assert.Single(result);
    }

    [Fact]
    public async Task GetTopRatedUsersAsync_NoCategory_ReturnsTopRatedUsers()
    {
        // Act
        var result = await _matchingService.GetTopRatedUsersAsync(limit: 10);

        // Assert
        var topUsers = result.ToList();
        Assert.Equal(2, topUsers.Count); // Only Bob and Diana have reviews
        
        // Diana should be first (rating 5.0), then Bob (rating 4.5)
        Assert.Equal(4, topUsers[0].Id);
        Assert.Equal(5.0, topUsers[0].Rating);
        Assert.Equal(2, topUsers[1].Id);
        Assert.Equal(4.5, topUsers[1].Rating);
    }

    [Fact]
    public async Task GetTopRatedUsersAsync_WithCategory_FiltersbyCategory()
    {
        // Act
        var result = await _matchingService.GetTopRatedUsersAsync(category: "Programming", limit: 10);

        // Assert
        var topUsers = result.ToList();
        Assert.Equal(2, topUsers.Count); // Bob and Diana offer Programming
        Assert.All(topUsers, u => 
            Assert.Contains(u.Skills, s => s.Skill.Category == "Programming" && s.IsOffering));
    }

    [Fact]
    public async Task GetTopRatedUsersAsync_LimitWorks()
    {
        // Act
        var result = await _matchingService.GetTopRatedUsersAsync(limit: 1);

        // Assert
        Assert.Single(result);
        Assert.Equal(4, result.First().Id); // Diana has highest rating
    }

    [Fact]
    public async Task UserMatchDto_PopulatesAllFieldsCorrectly()
    {
        // Act
        var result = await _matchingService.GetUserMatchDetailsAsync(2, 1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Id);
        Assert.Equal("Bob", result.Name);
        Assert.Equal("bob@test.com", result.Email);
        Assert.Equal("Web designer", result.Bio);
        Assert.Equal(15, result.TimeCredits);
        Assert.Equal(4.5, result.Rating);
        Assert.Equal(2, result.ReviewCount);
        Assert.True(result.IsOnline);
        
        // Check skills
        Assert.NotEmpty(result.Skills);
        var pythonSkill = result.Skills.First(s => s.SkillId == 2);
        Assert.Equal(5, pythonSkill.ProficiencyLevel);
        Assert.True(pythonSkill.IsOffering);
        Assert.Equal("Python", pythonSkill.Skill.Name);
        Assert.Equal("Programming", pythonSkill.Skill.Category);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}