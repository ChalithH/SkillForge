using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SkillForge.Api.Data;
using SkillForge.Api.Models;
using SkillForge.Api.Services;
using Xunit;

namespace SkillForge.Tests.Services;

public class SkillServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<ILogger<SkillService>> _mockLogger;
    private readonly SkillService _skillService;

    public SkillServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _mockLogger = new Mock<ILogger<SkillService>>();
        _skillService = new SkillService(_context, _mockLogger.Object);

        SeedTestData();
    }

    private void SeedTestData()
    {
        var skills = new List<Skill>
        {
            new Skill { Id = 1, Name = "JavaScript", Category = "Programming", Description = "JavaScript programming language" },
            new Skill { Id = 2, Name = "Python", Category = "Programming", Description = "Python programming language" },
            new Skill { Id = 3, Name = "Guitar", Category = "Music", Description = "Guitar playing skills" },
            new Skill { Id = 4, Name = "Piano", Category = "Music", Description = "Piano playing skills" },
            new Skill { Id = 5, Name = "Cooking", Category = "Lifestyle", Description = "Cooking various cuisines" }
        };

        _context.Skills.AddRange(skills);
        _context.SaveChanges();
    }

    [Fact]
    public async Task GetAllSkillsAsync_ReturnsAllSkillsOrderedByName()
    {
        // Act
        var result = await _skillService.GetAllSkillsAsync();

        // Assert
        var skills = result.ToList();
        Assert.Equal(5, skills.Count);
        Assert.Equal("Cooking", skills[0].Name);
        Assert.Equal("Guitar", skills[1].Name);
        Assert.Equal("JavaScript", skills[2].Name);
        Assert.Equal("Piano", skills[3].Name);
        Assert.Equal("Python", skills[4].Name);
    }

    [Fact]
    public async Task GetSkillByIdAsync_ExistingId_ReturnsSkill()
    {
        // Act
        var result = await _skillService.GetSkillByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("JavaScript", result.Name);
        Assert.Equal("Programming", result.Category);
    }

    [Fact]
    public async Task GetSkillByIdAsync_NonExistingId_ReturnsNull()
    {
        // Act
        var result = await _skillService.GetSkillByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetCategoriesAsync_ReturnsDistinctCategoriesOrdered()
    {
        // Act
        var result = await _skillService.GetCategoriesAsync();

        // Assert
        var categories = result.ToList();
        Assert.Equal(3, categories.Count);
        Assert.Equal("Lifestyle", categories[0]);
        Assert.Equal("Music", categories[1]);
        Assert.Equal("Programming", categories[2]);
    }

    [Fact]
    public async Task GetSkillsByCategoryAsync_ValidCategory_ReturnsSkillsInCategory()
    {
        // Act
        var result = await _skillService.GetSkillsByCategoryAsync("Programming");

        // Assert
        var skills = result.ToList();
        Assert.Equal(2, skills.Count);
        Assert.All(skills, s => Assert.Equal("Programming", s.Category));
    }

    [Fact]
    public async Task GetSkillsByCategoryAsync_CaseInsensitive_ReturnsSkills()
    {
        // Act
        var result = await _skillService.GetSkillsByCategoryAsync("pROGramming");

        // Assert
        var skills = result.ToList();
        Assert.Equal(2, skills.Count);
        Assert.All(skills, s => Assert.Equal("Programming", s.Category));
    }

    [Fact]
    public async Task GetSkillsByCategoryAsync_NonExistingCategory_ReturnsEmptyList()
    {
        // Act
        var result = await _skillService.GetSkillsByCategoryAsync("NonExistent");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task SearchSkillsAsync_ByName_ReturnsMatchingSkills()
    {
        // Act
        var result = await _skillService.SearchSkillsAsync("Java");

        // Assert
        var skills = result.ToList();
        Assert.Single(skills);
        Assert.Equal("JavaScript", skills[0].Name);
    }

    [Fact]
    public async Task SearchSkillsAsync_ByDescription_ReturnsMatchingSkills()
    {
        // Act
        var result = await _skillService.SearchSkillsAsync("programming language");

        // Assert
        var skills = result.ToList();
        Assert.Equal(2, skills.Count);
        Assert.All(skills, s => Assert.Contains("programming language", s.Description.ToLower()));
    }

    [Fact]
    public async Task SearchSkillsAsync_ByCategory_ReturnsMatchingSkills()
    {
        // Act
        var result = await _skillService.SearchSkillsAsync("Music");

        // Assert
        var skills = result.ToList();
        Assert.Equal(2, skills.Count);
        Assert.All(skills, s => Assert.Equal("Music", s.Category));
    }

    [Fact]
    public async Task SearchSkillsAsync_EmptySearchTerm_ReturnsAllSkills()
    {
        // Act
        var result = await _skillService.SearchSkillsAsync("");

        // Assert
        var skills = result.ToList();
        Assert.Equal(5, skills.Count);
    }

    [Fact]
    public async Task SearchSkillsAsync_NullSearchTerm_ReturnsAllSkills()
    {
        // Act
        var result = await _skillService.SearchSkillsAsync(null);

        // Assert
        var skills = result.ToList();
        Assert.Equal(5, skills.Count);
    }

    [Fact]
    public async Task CreateSkillAsync_ValidSkill_CreatesAndReturnsSkill()
    {
        // Arrange
        var newSkill = new Skill
        {
            Name = "React",
            Category = "Programming",
            Description = "React framework for JavaScript"
        };

        // Act
        var result = await _skillService.CreateSkillAsync(newSkill);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Id > 0);
        Assert.Equal("React", result.Name);

        var skillInDb = await _context.Skills.FindAsync(result.Id);
        Assert.NotNull(skillInDb);
        Assert.Equal("React", skillInDb.Name);
    }

    [Fact]
    public async Task CreateSkillAsync_DuplicateName_ReturnsNull()
    {
        // Arrange
        var duplicateSkill = new Skill
        {
            Name = "JavaScript",
            Category = "Web Development",
            Description = "Different description"
        };

        // Act
        var result = await _skillService.CreateSkillAsync(duplicateSkill);

        // Assert
        Assert.Null(result);
        
        // Verify warning was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("already exists")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateSkillAsync_ExistingSkill_UpdatesAndReturnsSkill()
    {
        // Arrange
        var updatedSkill = new Skill
        {
            Name = "JavaScript ES6",
            Category = "Web Development",
            Description = "Modern JavaScript"
        };

        // Act
        var result = await _skillService.UpdateSkillAsync(1, updatedSkill);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("JavaScript ES6", result.Name);
        Assert.Equal("Web Development", result.Category);
        Assert.Equal("Modern JavaScript", result.Description);

        var skillInDb = await _context.Skills.FindAsync(1);
        Assert.Equal("JavaScript ES6", skillInDb.Name);
    }

    [Fact]
    public async Task UpdateSkillAsync_NonExistingSkill_ReturnsNull()
    {
        // Arrange
        var updatedSkill = new Skill
        {
            Name = "New Name",
            Category = "New Category",
            Description = "New Description"
        };

        // Act
        var result = await _skillService.UpdateSkillAsync(999, updatedSkill);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateSkillAsync_DuplicateName_ReturnsNull()
    {
        // Arrange
        var updatedSkill = new Skill
        {
            Name = "Python", // This name already exists for skill ID 2
            Category = "Different Category",
            Description = "Different Description"
        };

        // Act
        var result = await _skillService.UpdateSkillAsync(1, updatedSkill);

        // Assert
        Assert.Null(result);
        
        // Verify warning was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("already exists")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteSkillAsync_ExistingSkillNotInUse_DeletesAndReturnsTrue()
    {
        // Act
        var result = await _skillService.DeleteSkillAsync(1);

        // Assert
        Assert.True(result);

        var skillInDb = await _context.Skills.FindAsync(1);
        Assert.Null(skillInDb);
    }

    [Fact]
    public async Task DeleteSkillAsync_NonExistingSkill_ReturnsFalse()
    {
        // Act
        var result = await _skillService.DeleteSkillAsync(999);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteSkillAsync_SkillInUse_ReturnsFalse()
    {
        // Arrange - Add a user with the skill
        var user = new User
        {
            Id = 1,
            Email = "test@test.com",
            Name = "Test User",
            PasswordHash = "hash",
            TimeCredits = 10,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);

        var userSkill = new UserSkill
        {
            UserId = 1,
            SkillId = 1,
            ProficiencyLevel = 3,
            IsOffering = true
        };
        _context.UserSkills.Add(userSkill);
        await _context.SaveChangesAsync();

        // Act
        var result = await _skillService.DeleteSkillAsync(1);

        // Assert
        Assert.False(result);

        var skillInDb = await _context.Skills.FindAsync(1);
        Assert.NotNull(skillInDb); // Skill should still exist
        
        // Verify warning was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Still in use")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}