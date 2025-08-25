using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SkillForge.Api.Configuration;
using SkillForge.Api.Data;
using SkillForge.Api.HealthChecks;
using SkillForge.Api.Services;
using SkillForge.Api.Hubs;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Add health checks
builder.Services.AddHealthChecks()
    .AddCheck<ExchangeBackgroundServiceHealthCheck>("exchange_background_service");

// Configure request size limits for file uploads
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 10 * 1024 * 1024; // 10MB limit
});
builder.Services.AddEndpointsApiExplorer();

// Configure Swagger with JWT support and file upload handling
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "SkillForge API", Version = "v1" });
    
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // Configure file upload support
    c.MapType<IFormFile>(() => new OpenApiSchema
    {
        Type = "string",
        Format = "binary"
    });
});

// Configure Entity Framework with connection string from environment variables
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Connection string 'DefaultConnection' not found. Please set the ConnectionStrings__DefaultConnection environment variable.");
}

// Configure database provider based on connection string
if (connectionString.Contains("Data Source="))
{
    // SQLite
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlite(connectionString));
}
else if (connectionString.Contains("Host=") || connectionString.Contains("Server=") && connectionString.Contains("Database=") && !connectionString.Contains("TrustServerCertificate"))
{
    // PostgreSQL
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(connectionString));
}
else
{
    // SQL Server
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(connectionString));
}

// Configure JWT Authentication with secret from environment variables
var jwtSecretKey = builder.Configuration["JwtSettings:SecretKey"];
if (string.IsNullOrEmpty(jwtSecretKey))
{
    throw new InvalidOperationException("JWT secret key not found. Please set the JwtSettings__SecretKey environment variable.");
}

var key = Encoding.ASCII.GetBytes(jwtSecretKey);

builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = false;
    x.SaveToken = true;
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false,
        ClockSkew = TimeSpan.Zero
    };
    
    // Enable JWT authentication for SignalR
    // Note: SignalR WebSockets cannot use Authorization headers, so query parameter is the standard approach
    x.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            // Only accept tokens for SignalR hub endpoints
            var path = context.HttpContext.Request.Path;
            if (path.StartsWithSegments("/hubs"))
            {
                // Try Authorization header first (for HTTP requests)
                var accessToken = context.Request.Headers.Authorization
                    .FirstOrDefault()?.Split(" ").Last();
                
                // Fall back to query parameter (required for WebSocket connections)
                if (string.IsNullOrEmpty(accessToken))
                {
                    accessToken = context.Request.Query["access_token"];
                }
                
                if (!string.IsNullOrEmpty(accessToken))
                {
                    context.Token = accessToken;
                }
            }
            return Task.CompletedTask;
        }
    };
});

// Configure CORS for SignalR support
builder.Services.AddCors(options =>
{
    // Get allowed origins from configuration
    var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? new[]
    {
        "http://localhost:3000",
        "http://frontend:3000", 
        "http://127.0.0.1:3000"
    };

    options.AddPolicy("AllowReactApp",
        corsBuilder => corsBuilder
            .WithOrigins(allowedOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()
            .SetIsOriginAllowed(origin =>
            {
                if (string.IsNullOrEmpty(origin)) return false;

                // Allow configured origins
                if (allowedOrigins.Contains(origin)) return true;

                // Allow localhost variations for development
                if (origin.StartsWith("http://localhost") || 
                    origin.StartsWith("http://127.0.0.1") || 
                    origin.StartsWith("http://frontend")) return true;

                // Allow Azure Container Apps domains
                if (origin.StartsWith("https://") && origin.Contains(".azurecontainerapps.io")) return true;

                // Allow Google Cloud Run domains  
                if (origin.StartsWith("https://") && origin.Contains(".run.app")) return true;

                return false;
            }));
});

// Configure SignalR with security settings
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
});

// Configure SignalR logging to exclude sensitive data
builder.Logging.AddFilter("Microsoft.AspNetCore.SignalR", LogLevel.Warning);
builder.Logging.AddFilter("Microsoft.AspNetCore.Http.Connections", LogLevel.Warning);

// Configure AutoMapper
builder.Services.AddAutoMapper(typeof(Program));

// Register application services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ISkillService, SkillService>();
builder.Services.AddScoped<ICreditService, CreditService>();
builder.Services.AddScoped<IExchangeService, ExchangeService>();
builder.Services.AddScoped<IMatchingService, MatchingService>();

// Register singleton services for real-time features
builder.Services.AddSingleton<IUserPresenceService, UserPresenceService>();
builder.Services.AddScoped<INotificationService, NotificationService>();

// Configure background service options
builder.Services.Configure<ExchangeBackgroundServiceOptions>(
    builder.Configuration.GetSection(ExchangeBackgroundServiceOptions.SectionName));

// Register background services
builder.Services.AddHostedService<ExchangeBackgroundService>();

// Build the application
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowReactApp");

// Ensure uploads directory exists and configure static file serving
var uploadsPath = Path.Combine(builder.Environment.ContentRootPath, "uploads");
Directory.CreateDirectory(uploadsPath);

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads"
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notification");
app.MapHealthChecks("/health");

// Initialise database on startup
using var scope = app.Services.CreateScope();
var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

try 
{
    var providerName = dbContext.Database.ProviderName;
    logger.LogInformation("Initialising database with provider: {ProviderName}", providerName);

    if (providerName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true)
    {
        logger.LogInformation("Using SQLite - creating database schema...");
        // For SQLite, use EnsureCreated to avoid SQL Server migration syntax issues
        var created = dbContext.Database.EnsureCreated();
        logger.LogInformation("SQLite database schema {Status}", created ? "created" : "already exists");
    }
    else if (providerName?.Contains("SqlServer", StringComparison.OrdinalIgnoreCase) == true)
    {
        logger.LogInformation("Using SQL Server - applying migrations...");
        dbContext.Database.Migrate();
        logger.LogInformation("SQL Server migrations completed successfully");
    }
    else
    {
        logger.LogWarning("Unknown database provider: {ProviderName}. Attempting to use migrations...", providerName);
        dbContext.Database.Migrate();
        logger.LogInformation("Migrations completed for provider: {ProviderName}", providerName);
    }
    
    // Seed the database
    DbInitializer.Initialize(dbContext);
    logger.LogInformation("Database seeding completed successfully");
}
catch (Exception ex)
{
    // Log but don't crash the app - important for production resilience
    logger.LogError(ex, "Database initialisation failed: {Message}", ex.Message);
    logger.LogWarning("Application will continue running - database may not be available");
}

app.Run();

// Make Program accessible for integration testing
public partial class Program { }