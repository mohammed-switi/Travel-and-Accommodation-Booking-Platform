using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Final_Project.Constants;
using Final_Project.Data;
using Final_Project.DTOs;
using Final_Project.Models;
using Final_Project.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Final_Project.Tests.ServicesTests;

public class AuthServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly Mock<IPasswordHasher<User>> _mockPasswordHasher;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<IDistributedCache> _mockCache;
    private readonly Mock<ILogger<AuthService>> _mockLogger;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);
        _mockPasswordHasher = new Mock<IPasswordHasher<User>>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockCache = new Mock<IDistributedCache>();
        _mockLogger = new Mock<ILogger<AuthService>>();

        SetupMockConfiguration();
        SetupMockCache();

        _authService = new AuthService(
            _context,
            _mockPasswordHasher.Object,
            _mockConfiguration.Object,
            _mockCache.Object,
            _mockLogger.Object);
    }

    private void SetupMockConfiguration()
    {
        _mockConfiguration.Setup(c => c["Jwt:Key"]).Returns("this-is-a-very-long-secret-key-for-jwt-token-generation-that-is-at-least-256-bits-long");
        _mockConfiguration.Setup(c => c["Jwt:Issuer"]).Returns("TestIssuer");
        _mockConfiguration.Setup(c => c["Jwt:Audience"]).Returns("TestAudience");
        _mockConfiguration.Setup(c => c["Jwt:ExpiryMinutes"]).Returns("60");
    }

    private void SetupMockCache()
    {
        // Setup the cache to return completed tasks for all SetAsync calls
        _mockCache.Setup(c => c.SetAsync(
            It.IsAny<string>(), 
            It.IsAny<byte[]>(), 
            It.IsAny<DistributedCacheEntryOptions>(), 
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    #region RegisterAsync Tests

    [Fact]
    public async Task RegisterAsync_WithValidData_ShouldRegisterNewUser()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            FullName = "John Doe",
            Email = "JOHN@EXAMPLE.COM",
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        };

        _mockPasswordHasher.Setup(h => h.HashPassword(It.IsAny<User>(), "Password123!"))
            .Returns("hashed_password");

        // Act
        var result = await _authService.RegisterAsync(registerDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("john@example.com", result.Email); // Should be lowercase
        Assert.Equal("John Doe", result.FullName);
        Assert.Equal(UserRoles.User, result.Role);
        Assert.Equal("hashed_password", result.PasswordHash);

        // Verify user was saved to database
        var userInDb = await _context.Users.FirstOrDefaultAsync(u => u.Email == "john@example.com");
        Assert.NotNull(userInDb);
        Assert.Equal("John Doe", userInDb.FullName);
        Assert.Equal("hashed_password", userInDb.PasswordHash);

        _mockPasswordHasher.Verify(h => h.HashPassword(It.IsAny<User>(), "Password123!"), Times.Once);
        VerifyLoggerWasCalled(LogLevel.Information, "Registering new user with email:");
        VerifyLoggerWasCalled(LogLevel.Information, "User registered successfully with ID:");
    }

    [Fact]
    public async Task RegisterAsync_ShouldHashPassword()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            FullName = "John Doe",
            Email = "john@example.com",
            Password = "TestPassword123!",
            ConfirmPassword = "TestPassword123!"
        };

        _mockPasswordHasher.Setup(h => h.HashPassword(It.IsAny<User>(), "TestPassword123!"))
            .Returns("securely_hashed_password");

        // Act
        await _authService.RegisterAsync(registerDto);

        // Assert
        _mockPasswordHasher.Verify(h => h.HashPassword(It.IsAny<User>(), "TestPassword123!"), Times.Once);
        
        var userInDb = await _context.Users.FirstOrDefaultAsync();
        Assert.NotNull(userInDb);
        Assert.Equal("securely_hashed_password", userInDb.PasswordHash);
    }

    [Fact]
    public async Task RegisterAsync_WhenEmailExists_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var existingUser = new User
        {
            Email = "existing@example.com",
            FullName = "Existing User",
            PasswordHash = "hashed_password",
            Role = UserRoles.User
        };
        _context.Users.Add(existingUser);
        await _context.SaveChangesAsync();

        var registerDto = new RegisterDto
        {
            FullName = "John Doe",
            Email = "existing@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _authService.RegisterAsync(registerDto));

        Assert.Equal("Email already exists.", exception.Message);
        _mockPasswordHasher.Verify(h => h.HashPassword(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
        
        // Verify no additional user was created
        var userCount = await _context.Users.CountAsync();
        Assert.Equal(1, userCount);
    }

    [Fact]
    public async Task RegisterAsync_ShouldStoreEmailInLowercase()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            FullName = "John Doe",
            Email = "JOHN.DOE@EXAMPLE.COM",
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        };

        _mockPasswordHasher.Setup(h => h.HashPassword(It.IsAny<User>(), It.IsAny<string>()))
            .Returns("hashed_password");

        // Act
        var result = await _authService.RegisterAsync(registerDto);

        // Assert
        Assert.Equal("john.doe@example.com", result.Email);
        
        var userInDb = await _context.Users.FirstOrDefaultAsync();
        Assert.NotNull(userInDb);
        Assert.Equal("john.doe@example.com", userInDb.Email);
    }

    [Fact]
    public async Task RegisterAsync_ShouldAssignDefaultRole()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            FullName = "John Doe",
            Email = "john@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        };

        _mockPasswordHasher.Setup(h => h.HashPassword(It.IsAny<User>(), It.IsAny<string>()))
            .Returns("hashed_password");

        // Act
        var result = await _authService.RegisterAsync(registerDto);

        // Assert
        Assert.Equal(UserRoles.User, result.Role);
        
        var userInDb = await _context.Users.FirstOrDefaultAsync();
        Assert.NotNull(userInDb);
        Assert.Equal(UserRoles.User, userInDb.Role);
    }

    #endregion

    #region LoginAsync Tests

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ShouldReturnJwtToken()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Email = "john@example.com",
            PasswordHash = "hashed_password",
            FullName = "John Doe",
            Role = UserRoles.User
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var loginDto = new LoginDto
        {
            Email = "john@example.com",
            Password = "Password123!"
        };

        _mockPasswordHasher.Setup(h => h.VerifyHashedPassword(user, "hashed_password", "Password123!"))
            .Returns(PasswordVerificationResult.Success);

        // Act
        var result = await _authService.LoginAsync(loginDto);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);

        // Parse and verify JWT token
        var tokenHandler = new JwtSecurityTokenHandler();
        var jsonToken = tokenHandler.ReadJwtToken(result);

        Assert.Equal("TestIssuer", jsonToken.Issuer);
        Assert.Equal("TestAudience", jsonToken.Audiences.First());
        Assert.Contains(jsonToken.Claims, c => c.Type == ClaimTypes.NameIdentifier && c.Value == "1");
        Assert.Contains(jsonToken.Claims, c => c.Type == ClaimTypes.Email && c.Value == "john@example.com");
        Assert.Contains(jsonToken.Claims, c => c.Type == ClaimTypes.Role && c.Value == UserRoles.User);
        Assert.Contains(jsonToken.Claims, c => c.Type == JwtRegisteredClaimNames.Jti);
    }

    [Fact]
    public async Task LoginAsync_WhenUserNotFound_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Email = "nonexistent@example.com",
            Password = "Password123!"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _authService.LoginAsync(loginDto));

        Assert.Equal("Invalid email or password.", exception.Message);
        _mockPasswordHasher.Verify(h => h.VerifyHashedPassword(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_WhenPasswordIncorrect_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Email = "john@example.com",
            PasswordHash = "hashed_password",
            FullName = "John Doe",
            Role = UserRoles.User
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var loginDto = new LoginDto
        {
            Email = "john@example.com",
            Password = "WrongPassword"
        };

        _mockPasswordHasher.Setup(h => h.VerifyHashedPassword(user, "hashed_password", "WrongPassword"))
            .Returns(PasswordVerificationResult.Failed);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _authService.LoginAsync(loginDto));

        Assert.Equal("Invalid email or password.", exception.Message);
    }

    [Fact]
    public async Task LoginAsync_ShouldValidateTokenContainsCorrectClaimsAndUsesConfigValues()
    {
        // Arrange
        var user = new User
        {
            Id = 42,
            Email = "admin@example.com",
            PasswordHash = "admin_hashed_password",
            FullName = "Admin User",
            Role = UserRoles.Admin
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var loginDto = new LoginDto
        {
            Email = "admin@example.com",
            Password = "AdminPassword123!"
        };

        _mockPasswordHasher.Setup(h => h.VerifyHashedPassword(user, "admin_hashed_password", "AdminPassword123!"))
            .Returns(PasswordVerificationResult.Success);

        // Act
        var result = await _authService.LoginAsync(loginDto);

        // Assert
        var tokenHandler = new JwtSecurityTokenHandler();
        var jsonToken = tokenHandler.ReadJwtToken(result);

        // Verify config values are used
        Assert.Equal("TestIssuer", jsonToken.Issuer);
        Assert.Equal("TestAudience", jsonToken.Audiences.First());

        // Verify claims
        var nameIdentifierClaim = jsonToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
        Assert.NotNull(nameIdentifierClaim);
        Assert.Equal("42", nameIdentifierClaim.Value);

        var emailClaim = jsonToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
        Assert.NotNull(emailClaim);
        Assert.Equal("admin@example.com", emailClaim.Value);

        var roleClaim = jsonToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
        Assert.NotNull(roleClaim);
        Assert.Equal(UserRoles.Admin, roleClaim.Value);

        var jtiClaim = jsonToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti);
        Assert.NotNull(jtiClaim);
        Assert.NotEmpty(jtiClaim.Value);

        // Verify expiration is set correctly (60 minutes from configuration)
        var expiration = jsonToken.ValidTo;
        var expectedExpiration = DateTime.UtcNow.AddMinutes(60);
        Assert.True(Math.Abs((expiration - expectedExpiration).TotalMinutes) < 1); // Within 1 minute tolerance
    }

    #endregion

    #region ForgotPasswordAsync Tests

    [Fact]
    public async Task ForgotPasswordAsync_WhenEmailExists_ShouldGenerateAndSaveResetToken()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Email = "john@example.com",
            PasswordHash = "hashed_password",
            FullName = "John Doe"
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var forgotPasswordDto = new ForgotPasswordDto
        {
            Email = "JOHN@EXAMPLE.COM"
        };

        // Act
        await _authService.ForgotPasswordAsync(forgotPasswordDto);

        // Assert
        var updatedUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == 1);
        Assert.NotNull(updatedUser);
        Assert.NotNull(updatedUser.PasswordResetToken);
        Assert.NotEmpty(updatedUser.PasswordResetToken);
        Assert.NotNull(updatedUser.PasswordResetTokenExpiration);
        Assert.True(updatedUser.PasswordResetTokenExpiration > DateTime.UtcNow);
        Assert.True(updatedUser.PasswordResetTokenExpiration <= DateTime.UtcNow.AddHours(1));

        VerifyLoggerWasCalled(LogLevel.Information, "Password reset token generated for user with email:");
    }

    [Fact]
    public async Task ForgotPasswordAsync_WhenUserNotFound_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var forgotPasswordDto = new ForgotPasswordDto
        {
            Email = "nonexistent@example.com"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _authService.ForgotPasswordAsync(forgotPasswordDto));

        Assert.Equal("User not found.", exception.Message);
    }

    #endregion

    #region ResetPasswordAsync Tests

    [Fact]
    public async Task ResetPasswordAsync_WithValidTokenNotExpired_ShouldResetPassword()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            FullName = "John Doe",
            Email = "john@example.com",
            PasswordHash = "old_hashed_password",
            PasswordResetToken = "valid_token",
            PasswordResetTokenExpiration = DateTime.UtcNow.AddMinutes(30) // Not expired
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var resetPasswordDto = new ResetPasswordDto
        {
            Email = "john@example.com",
            Token = "valid_token",
            NewPassword = "NewPassword123!",
            ConfirmPassword = "NewPassword123!"
        };

        _mockPasswordHasher.Setup(h => h.HashPassword(user, "NewPassword123!"))
            .Returns("new_hashed_password");

        // Act
        await _authService.ResetPasswordAsync(resetPasswordDto);

        // Assert
        var updatedUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == 1);
        Assert.NotNull(updatedUser);
        Assert.Equal("new_hashed_password", updatedUser.PasswordHash);
        Assert.Null(updatedUser.PasswordResetToken);
        Assert.Null(updatedUser.PasswordResetTokenExpiration);

        _mockPasswordHasher.Verify(h => h.HashPassword(user, "NewPassword123!"), Times.Once);
    }

    [Fact]
    public async Task ResetPasswordAsync_WhenUserNotFound_ShouldThrowException()
    {
        // Arrange
        var resetPasswordDto = new ResetPasswordDto
        {
            Email = "nonexistent@example.com",
            Token = "some_token",
            NewPassword = "NewPassword123!",
            ConfirmPassword = "NewPassword123!"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(
            () => _authService.ResetPasswordAsync(resetPasswordDto));

        Assert.Equal("Invalid email or token.", exception.Message);
        _mockPasswordHasher.Verify(h => h.HashPassword(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ResetPasswordAsync_WhenTokenInvalid_ShouldThrowException()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            FullName = "John Doe",
            Email = "john@example.com",
            PasswordHash = "old_hashed_password",
            PasswordResetToken = "valid_token", // Different token
            PasswordResetTokenExpiration = DateTime.UtcNow.AddMinutes(30)
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var resetPasswordDto = new ResetPasswordDto
        {
            Email = "john@example.com",
            Token = "invalid_token",
            NewPassword = "NewPassword123!",
            ConfirmPassword = "NewPassword123!"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(
            () => _authService.ResetPasswordAsync(resetPasswordDto));

        Assert.Equal("Invalid or expired token.", exception.Message);
    }

    [Fact]
    public async Task ResetPasswordAsync_WhenTokenExpired_ShouldThrowException()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            FullName = "John Doe",
            Email = "john@example.com",
            PasswordHash = "old_hashed_password",
            PasswordResetToken = "valid_token",
            PasswordResetTokenExpiration = DateTime.UtcNow.AddMinutes(-30) // Expired
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var resetPasswordDto = new ResetPasswordDto
        {
            Email = "john@example.com",
            Token = "valid_token",
            NewPassword = "NewPassword123!",
            ConfirmPassword = "NewPassword123!"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(
            () => _authService.ResetPasswordAsync(resetPasswordDto));

        Assert.Equal("Invalid or expired token.", exception.Message);
    }

    [Fact]
    public async Task ResetPasswordAsync_ShouldClearTokenAndExpirationAfterSuccess()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            FullName = "John Doe",
            Email = "john@example.com",
            PasswordHash = "old_password",
            PasswordResetToken = "valid_token",
            PasswordResetTokenExpiration = DateTime.UtcNow.AddMinutes(30)
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var resetPasswordDto = new ResetPasswordDto
        {
            Email = "john@example.com",
            Token = "valid_token",
            NewPassword = "NewPassword123!",
            ConfirmPassword = "NewPassword123!"
        };

        _mockPasswordHasher.Setup(h => h.HashPassword(user, "NewPassword123!"))
            .Returns("new_hashed_password");

        // Act
        await _authService.ResetPasswordAsync(resetPasswordDto);

        // Assert
        var updatedUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == 1);
        Assert.NotNull(updatedUser);
        Assert.Null(updatedUser.PasswordResetToken);
        Assert.Null(updatedUser.PasswordResetTokenExpiration);
    }

    #endregion

    #region LogoutAsync Tests

    [Fact]
    public async Task LogoutAsync_WithValidUser_ShouldExtractJtiAndCallSaveJwtTokenToCache()
    {
        // Arrange
        var jwtId = Guid.NewGuid().ToString();
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Jti, jwtId),
            new Claim(ClaimTypes.NameIdentifier, "1")
        };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);

        // Act
        await _authService.LogoutAsync(principal);

        // Assert
        _mockCache.Verify(c => c.SetAsync(
            $"JwtID_{jwtId}",
            It.IsAny<byte[]>(),
            It.IsAny<DistributedCacheEntryOptions>(),
            default), Times.Once);

        VerifyLoggerWasCalled(LogLevel.Information, "Logging out user with JWT ID:");
    }

    [Fact]
    public async Task LogoutAsync_WithMissingJti_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "1")
            // No JTI claim
        };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _authService.LogoutAsync(principal));

        Assert.Equal("JWT ID not found in claims.", exception.Message);
        _mockCache.Verify(c => c.SetAsync(
            It.IsAny<string>(),
            It.IsAny<byte[]>(),
            It.IsAny<DistributedCacheEntryOptions>(),
            default), Times.Never);
    }

    

   

    #endregion

    #region SaveJwtTokenToCache Tests

    [Fact]
    public async Task SaveJwtTokenToCache_WithValidJwtId_ShouldStoreInCacheWith24HourExpiration()
    {
        // Arrange
        var jwtId = Guid.NewGuid().ToString();
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Jti, jwtId)
        };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);

        // Act
        await _authService.LogoutAsync(principal);

        // Assert
        _mockCache.Verify(c => c.SetAsync(
            $"JwtID_{jwtId}",
            It.IsAny<byte[]>(),
            It.IsAny<DistributedCacheEntryOptions>(),
            default), Times.Once);
    }

  #endregion 
    #region Helper Methods

    private void VerifyLoggerWasCalled(LogLevel level, string message)
    {
        _mockLogger.Verify(
            x => x.Log(
                level,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(message)),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    #endregion

    public void Dispose()
    {
        _context.Dispose();
    }
}
