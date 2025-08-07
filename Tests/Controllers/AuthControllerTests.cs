using Final_Project.Controllers;
using Final_Project.DTOs;
using Final_Project.Models;
using Final_Project.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using Xunit;

namespace Final_Project.Tests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _mockAuthService;
    private readonly AuthController _authController;

    public AuthControllerTests()
    {
        _mockAuthService = new Mock<IAuthService>();
        _authController = new AuthController(_mockAuthService.Object);
    }

    #region Register Tests

    [Fact]
    public async Task Register_ReturnsOkWithSuccessMessage_WhenRegistrationIsSuccessful()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            FullName = "Test User",
            Email = "test@example.com",
            Password = "Test@123",
            ConfirmPassword = "Test@123"
        };

        var expectedUser = new User { Id = 1, FullName = "Test User", Email = "test@example.com" };
        _mockAuthService.Setup(x => x.RegisterAsync(registerDto))
            .ReturnsAsync(expectedUser);

        // Act
        var result = await _authController.Register(registerDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value;
        
        // Use reflection to check the anonymous object properties
        var messageProperty = response?.GetType().GetProperty("message");
        var userIdProperty = response?.GetType().GetProperty("userId");
        
        Assert.NotNull(messageProperty);
        Assert.NotNull(userIdProperty);
        Assert.Equal("User Registered successfully", messageProperty.GetValue(response));
        Assert.Equal(1, userIdProperty.GetValue(response));
        
        _mockAuthService.Verify(x => x.RegisterAsync(registerDto), Times.Once);
    }

    [Fact]
    public async Task Register_ReturnsBadRequest_WhenRegistrationFails()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            FullName = "Test User",
            Email = "test@example.com",
            Password = "Test@123",
            ConfirmPassword = "Test@123"
        };

        var exceptionMessage = "Email already exists";
        _mockAuthService.Setup(x => x.RegisterAsync(registerDto))
            .ThrowsAsync(new Exception(exceptionMessage));

        // Act
        var result = await _authController.Register(registerDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = badRequestResult.Value;
        
        var messageProperty = response?.GetType().GetProperty("message");
        Assert.NotNull(messageProperty);
        Assert.Equal(exceptionMessage, messageProperty.GetValue(response));
        
        _mockAuthService.Verify(x => x.RegisterAsync(registerDto), Times.Once);
    }

    #endregion

    #region Login Tests

    [Fact]
    public async Task Login_ReturnsOkWithToken_WhenLoginIsSuccessful()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Email = "test@example.com",
            Password = "Test@123"
        };

        var expectedToken = "jwt-token-string";
        _mockAuthService.Setup(x => x.LoginAsync(loginDto))
            .ReturnsAsync(expectedToken);

        // Act
        var result = await _authController.Login(loginDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value;
        
        var tokenProperty = response?.GetType().GetProperty("token");
        Assert.NotNull(tokenProperty);
        Assert.Equal(expectedToken, tokenProperty.GetValue(response));
        
        _mockAuthService.Verify(x => x.LoginAsync(loginDto), Times.Once);
    }

    [Fact]
    public async Task Login_ReturnsBadRequest_WhenModelStateIsInvalid()
    {
        // Arrange
        var loginDto = new LoginDto(); // Invalid model
        _authController.ModelState.AddModelError("Email", "Email is required");

        // Act
        var result = await _authController.Login(loginDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Invalid login data.", badRequestResult.Value);
        
        _mockAuthService.Verify(x => x.LoginAsync(It.IsAny<LoginDto>()), Times.Never);
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_WhenLoginFails()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Email = "test@example.com",
            Password = "WrongPassword"
        };

        var exceptionMessage = "Invalid email or password";
        _mockAuthService.Setup(x => x.LoginAsync(loginDto))
            .ThrowsAsync(new Exception(exceptionMessage));

        // Act
        var result = await _authController.Login(loginDto);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        var response = unauthorizedResult.Value;
        
        var messageProperty = response?.GetType().GetProperty("message");
        Assert.NotNull(messageProperty);
        Assert.Equal(exceptionMessage, messageProperty.GetValue(response));
        
        _mockAuthService.Verify(x => x.LoginAsync(loginDto), Times.Once);
    }

    #endregion

    #region ResetPassword Tests

    [Fact]
    public async Task ResetPassword_ReturnsOkWithSuccessMessage_WhenResetIsSuccessful()
    {
        // Arrange
        var resetDto = new ResetPasswordDto
        {
            Email = "test@example.com",
            Token = "reset-token",
            NewPassword = "NewPassword@123",
            ConfirmPassword = "NewPassword@123"
        };

        _mockAuthService.Setup(x => x.ResetPasswordAsync(resetDto))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _authController.ResetPassword(resetDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value;
        
        var messageProperty = response?.GetType().GetProperty("Message");
        Assert.NotNull(messageProperty);
        Assert.Equal("Password reset successful.", messageProperty.GetValue(response));
        
        _mockAuthService.Verify(x => x.ResetPasswordAsync(resetDto), Times.Once);
    }

    [Fact]
    public async Task ResetPassword_ReturnsBadRequestWithModelState_WhenModelStateIsInvalid()
    {
        // Arrange
        var resetDto = new ResetPasswordDto(); // Invalid model
        _authController.ModelState.AddModelError("Email", "Email is required");

        // Act
        var result = await _authController.ResetPassword(resetDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value); // Just verify the value is not null since ModelState is returned
        
        _mockAuthService.Verify(x => x.ResetPasswordAsync(It.IsAny<ResetPasswordDto>()), Times.Never);
    }

    [Fact]
    public async Task ResetPassword_ReturnsBadRequest_WhenResetFails()
    {
        // Arrange
        var resetDto = new ResetPasswordDto
        {
            Email = "test@example.com",
            Token = "invalid-token",
            NewPassword = "NewPassword@123",
            ConfirmPassword = "NewPassword@123"
        };

        var exceptionMessage = "Invalid reset token";
        _mockAuthService.Setup(x => x.ResetPasswordAsync(resetDto))
            .ThrowsAsync(new Exception(exceptionMessage));

        // Act
        var result = await _authController.ResetPassword(resetDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = badRequestResult.Value;
        
        var errorProperty = response?.GetType().GetProperty("error");
        Assert.NotNull(errorProperty);
        Assert.Equal(exceptionMessage, errorProperty.GetValue(response));
        
        _mockAuthService.Verify(x => x.ResetPasswordAsync(resetDto), Times.Once);
    }

    #endregion

    #region Logout Tests

    [Fact]
    public async Task Logout_ReturnsOkWithSuccessMessage_WhenLogoutIsSuccessful()
    {
        // Arrange
        SetupAuthenticatedUser();
        _mockAuthService.Setup(x => x.LogoutAsync(It.IsAny<ClaimsPrincipal>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _authController.Logout();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value;
        
        var messageProperty = response?.GetType().GetProperty("message");
        Assert.NotNull(messageProperty);
        Assert.Equal("User logged out successfully", messageProperty.GetValue(response));
        
        _mockAuthService.Verify(x => x.LogoutAsync(It.IsAny<ClaimsPrincipal>()), Times.Once);
    }

    [Fact]
    public async Task Logout_ReturnsUnauthorized_WhenUserIsNotAuthenticated()
    {
        // Arrange - Don't set up authenticated user, so User.Identity.IsAuthenticated will be false

        // Act
        var result = await _authController.Logout();

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        var response = unauthorizedResult.Value;
        
        var messageProperty = response?.GetType().GetProperty("message");
        Assert.NotNull(messageProperty);
        Assert.Equal("User is not authenticated", messageProperty.GetValue(response));
        
        _mockAuthService.Verify(x => x.LogoutAsync(It.IsAny<ClaimsPrincipal>()), Times.Never);
    }

    [Fact]
    public async Task Logout_ReturnsUnauthorized_WhenUserIsNull()
    {
        // Arrange - Set User to null explicitly
        var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        httpContext.User = null!;
        _authController.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        // Act
        var result = await _authController.Logout();

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        var response = unauthorizedResult.Value;
        
        var messageProperty = response?.GetType().GetProperty("message");
        Assert.NotNull(messageProperty);
        Assert.Equal("User is not authenticated", messageProperty.GetValue(response));
        
        _mockAuthService.Verify(x => x.LogoutAsync(It.IsAny<ClaimsPrincipal>()), Times.Never);
    }

    [Fact]
    public async Task Logout_ReturnsBadRequest_WhenLogoutFails()
    {
        // Arrange
        SetupAuthenticatedUser();
        var exceptionMessage = "Token blacklisting failed";
        _mockAuthService.Setup(x => x.LogoutAsync(It.IsAny<ClaimsPrincipal>()))
            .ThrowsAsync(new Exception(exceptionMessage));

        // Act
        var result = await _authController.Logout();

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = badRequestResult.Value;
        
        var messageProperty = response?.GetType().GetProperty("message");
        Assert.NotNull(messageProperty);
        Assert.Equal(exceptionMessage, messageProperty.GetValue(response));
        
        _mockAuthService.Verify(x => x.LogoutAsync(It.IsAny<ClaimsPrincipal>()), Times.Once);
    }

    #endregion

    #region Helper Methods

    private void SetupAuthenticatedUser()
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "1"),
            new(ClaimTypes.Email, "test@example.com"),
            new(ClaimTypes.Role, "User")
        };

        var identity = new ClaimsIdentity(claims, "TestAuthentication");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        httpContext.User = claimsPrincipal;

        _authController.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    #endregion
}
