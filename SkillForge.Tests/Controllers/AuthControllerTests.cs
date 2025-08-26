using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using SkillForge.Api.Controllers;
using SkillForge.Api.DTOs.Auth;
using SkillForge.Api.Models;
using SkillForge.Api.Services;
using System.Net;
using System.Security.Claims;

namespace SkillForge.Tests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _mockAuthService;
    private readonly Mock<ILogger<AuthController>> _mockLogger;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _mockAuthService = new Mock<IAuthService>();
        _mockLogger = new Mock<ILogger<AuthController>>();
        _controller = new AuthController(_mockAuthService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task Register_ValidData_ReturnsOk()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Email = "test@example.com",
            Password = "password123",
            Name = "Test User",
            Bio = "Test bio"
        };

        var authResponse = new AuthResponseDto
        {
            Email = registerDto.Email,
            Name = registerDto.Name,
            Bio = registerDto.Bio,
            TimeCredits = 5,
            Token = "test-token",
            Id = 1
        };

        _mockAuthService.Setup(x => x.RegisterAsync(It.IsAny<RegisterDto>()))
                       .ReturnsAsync(authResponse);

        // Act
        var result = await _controller.Register(registerDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedAuthResponse = Assert.IsType<AuthResponseDto>(okResult.Value);
        Assert.Equal(registerDto.Email, returnedAuthResponse.Email);
        Assert.Equal(registerDto.Name, returnedAuthResponse.Name);
        Assert.Equal(5, returnedAuthResponse.TimeCredits);
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsBadRequest()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Email = "duplicate@example.com",
            Password = "password123",
            Name = "Test User"
        };

        _mockAuthService.Setup(x => x.RegisterAsync(It.IsAny<RegisterDto>()))
                       .ReturnsAsync((AuthResponseDto?)null);

        // Act
        var result = await _controller.Register(registerDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var response = badRequestResult.Value;
        Assert.NotNull(response);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsOk()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Email = "test@example.com",
            Password = "password123"
        };

        var authResponse = new AuthResponseDto
        {
            Email = loginDto.Email,
            Name = "Test User",
            TimeCredits = 10,
            Token = "test-token",
            Id = 1
        };

        _mockAuthService.Setup(x => x.LoginAsync(It.IsAny<LoginDto>()))
                       .ReturnsAsync(authResponse);

        // Act
        var result = await _controller.Login(loginDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedAuthResponse = Assert.IsType<AuthResponseDto>(okResult.Value);
        Assert.Equal(loginDto.Email, returnedAuthResponse.Email);
    }

    [Fact]
    public async Task Login_InvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Email = "test@example.com",
            Password = "wrongpassword"
        };

        _mockAuthService.Setup(x => x.LoginAsync(It.IsAny<LoginDto>()))
                       .ReturnsAsync((AuthResponseDto?)null);

        // Act
        var result = await _controller.Login(loginDto);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        var response = unauthorizedResult.Value;
        Assert.NotNull(response);
    }

    [Fact]
    public async Task GetCurrentUser_ValidUser_ReturnsOk()
    {
        // Arrange
        var userId = 123;
        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            Name = "Test User",
            TimeCredits = 15,
            Bio = "Test bio",
            ProfileImageUrl = "test-image-url"
        };

        _mockAuthService.Setup(x => x.GetUserByIdAsync(userId))
                       .ReturnsAsync(user);

        // Setup user claims
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims))
            }
        };

        // Act
        var result = await _controller.GetCurrentUser();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedData = okResult.Value;
        Assert.NotNull(returnedData);

        // Verify the anonymous object contains the expected properties
        var properties = returnedData.GetType().GetProperties();
        var emailProperty = properties.FirstOrDefault(p => p.Name == "email");
        var nameProperty = properties.FirstOrDefault(p => p.Name == "name");
        var timeCreditsProperty = properties.FirstOrDefault(p => p.Name == "timeCredits");

        Assert.NotNull(emailProperty);
        Assert.NotNull(nameProperty);
        Assert.NotNull(timeCreditsProperty);
        
        Assert.Equal(user.Email, emailProperty.GetValue(returnedData));
        Assert.Equal(user.Name, nameProperty.GetValue(returnedData));
        Assert.Equal(user.TimeCredits, timeCreditsProperty.GetValue(returnedData));
    }

    [Fact]
    public async Task GetCurrentUser_InvalidUserId_ReturnsUnauthorized()
    {
        // Arrange - Setup invalid user ID claim
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "invalid")
        };

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims))
            }
        };

        // Act
        var result = await _controller.GetCurrentUser();

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task UpdateProfile_ValidData_ReturnsOk()
    {
        // Arrange
        var userId = 123;
        var updateDto = new UpdateProfileDto
        {
            Name = "Updated Name",
            Bio = "Updated bio"
        };

        var updatedUser = new User
        {
            Id = userId,
            Email = "test@example.com",
            Name = updateDto.Name,
            Bio = updateDto.Bio,
            TimeCredits = 15
        };

        _mockAuthService.Setup(x => x.UpdateProfileAsync(userId, It.IsAny<UpdateProfileDto>()))
                       .ReturnsAsync(updatedUser);

        // Setup user claims
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims))
            }
        };

        // Act
        var result = await _controller.UpdateProfile(updateDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedData = okResult.Value;
        Assert.NotNull(returnedData);

        // Verify the anonymous object contains the expected properties
        var properties = returnedData.GetType().GetProperties();
        var nameProperty = properties.FirstOrDefault(p => p.Name == "name");
        var bioProperty = properties.FirstOrDefault(p => p.Name == "bio");

        Assert.NotNull(nameProperty);
        Assert.NotNull(bioProperty);
        
        Assert.Equal(updateDto.Name, nameProperty.GetValue(returnedData));
        Assert.Equal(updateDto.Bio, bioProperty.GetValue(returnedData));
    }
}