using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SkillForge.Api.Data;
using System.Net;
using Xunit;

namespace SkillForge.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Set environment variables BEFORE the application starts
        Environment.SetEnvironmentVariable("JWT_SECRET_KEY", "test-secret-key-for-smoke-tests-minimum-32-characters");
        Environment.SetEnvironmentVariable("JwtSettings__SecretKey", "test-secret-key-for-smoke-tests-minimum-32-characters");
        
        builder.ConfigureTestServices(services =>
        {
            // Remove any existing DbContext registrations
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Configure PostgreSQL for integration testing (matches production)
            var connectionString = "Host=localhost;Port=5433;Database=skillforge_test;Username=skillforge_test;Password=test_password_123";
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseNpgsql(connectionString);
                // Enable sensitive data logging for tests
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            });
        });

        builder.UseEnvironment("Testing");
    }

    private bool _databaseInitialized = false;
    private readonly object _lockObject = new object();

    public void EnsureDatabaseCreated()
    {
        if (_databaseInitialized) return;

        lock (_lockObject)
        {
            if (_databaseInitialized) return;

            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            try
            {
                // For PostgreSQL testing, use EnsureDeleted + EnsureCreated for clean schema
                // This avoids migration compatibility issues between SQL Server and PostgreSQL
                context.Database.EnsureDeleted(); // Clean slate for tests
                context.Database.EnsureCreated(); // Create schema from model
                _databaseInitialized = true;
            }
            catch (Exception ex)
            {
                // Log but continue - tests might still work
                Console.WriteLine($"Database initialization warning: {ex.Message}");
            }
        }
    }
}

public class SmokeTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public SmokeTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        
        // Ensure PostgreSQL test database is ready
        _factory.EnsureDatabaseCreated();
        
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task HealthCheck_ReturnsOk()
    {
        // Arrange
        var request = "/health";

        // Act
        var response = await _client.GetAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Healthy", content);
    }

    [Fact]
    public async Task Auth_RegisterWithValidData_ReturnsSuccessAndToken()
    {
        // Arrange
        var registerRequest = new
        {
            Email = $"smoketest+{Guid.NewGuid()}@example.com",
            Password = "TestPassword123!",
            Name = "Smoke Test User",
            Bio = "Test user for smoke testing"
        };

        var json = System.Text.Json.JsonSerializer.Serialize(registerRequest);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/auth/register", content);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        Assert.Contains("token", responseContent.ToLower());
        Assert.Contains("email", responseContent.ToLower());
    }

    [Fact]
    public async Task Database_PostgreSqlInitialization_Succeeds()
    {
        // Arrange & Act - Database should be initialized during app startup
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Assert - Verify database connection and basic operations
        Assert.True(await context.Database.CanConnectAsync());
        
        // Verify key tables exist by checking if we can query them
        var usersCount = await context.Users.CountAsync();
        var skillsCount = await context.Skills.CountAsync();
        
        // Should not throw exceptions (counts can be 0, that's fine)
        Assert.True(usersCount >= 0);
        Assert.True(skillsCount >= 0);
    }
}