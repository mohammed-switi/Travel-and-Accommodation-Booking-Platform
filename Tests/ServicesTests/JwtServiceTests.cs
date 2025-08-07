using System.Security.Claims;
using Final_Project.Services;
using Final_Project.Tests.ServicesTests.Helpers;
using Xunit;

namespace Final_Project.Tests.ServicesTests;

public class JwtServiceTests
{
    private readonly JwtService _jwtService;

    public JwtServiceTests()
    {
        _jwtService = new JwtService();
    }

    #region GetUserIdFromClaims Tests

    [Fact]
    public void GetUserIdFromClaims_WhenNameIdentifierClaimIsPresentAndValid_ReturnsCorrectUserId()
    {
        // Arrange
        const int expectedUserId = 123;
        var claimsPrincipal = TestDataBuilder.CreateClaimsPrincipal(expectedUserId.ToString(), "Admin");

        // Act
        var result = _jwtService.GetUserIdFromClaims(claimsPrincipal);

        // Assert
        Assert.Equal(expectedUserId, result);
    }

    [Fact]
    public void GetUserIdFromClaims_WhenNameIdentifierClaimIsMissing_ThrowsInvalidOperationException()
    {
        // Arrange
        var claimsPrincipal = TestDataBuilder.CreateClaimsPrincipal(null, "Admin");

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            _jwtService.GetUserIdFromClaims(claimsPrincipal));
        
        Assert.Equal("User ID not found in claims", exception.Message);
    }

    [Fact]
    public void GetUserIdFromClaims_WhenNameIdentifierClaimIsEmpty_ThrowsInvalidOperationException()
    {
        // Arrange
        var claimsPrincipal = TestDataBuilder.CreateClaimsPrincipal("", "Admin");

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            _jwtService.GetUserIdFromClaims(claimsPrincipal));
        
        Assert.Equal("User ID not found in claims", exception.Message);
    }

    [Fact]
    public void GetUserIdFromClaims_WhenNameIdentifierClaimIsNonNumeric_ThrowsInvalidOperationException()
    {
        // Arrange
        var claimsPrincipal = TestDataBuilder.CreateClaimsPrincipal("invalid-user-id", "Admin");

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            _jwtService.GetUserIdFromClaims(claimsPrincipal));
        
        Assert.Equal("Invalid user ID format in claims", exception.Message);
    }

    [Fact]
    public void GetUserIdFromClaims_WhenNameIdentifierClaimIsFloatingPoint_ThrowsInvalidOperationException()
    {
        // Arrange
        var claimsPrincipal = TestDataBuilder.CreateClaimsPrincipal("123.45", "Admin");

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            _jwtService.GetUserIdFromClaims(claimsPrincipal));
        
        Assert.Equal("Invalid user ID format in claims", exception.Message);
    }

    #endregion

    #region GetUserRoleFromClaims Tests

    [Fact]
    public void GetUserRoleFromClaims_WhenRoleClaimIsPresent_ReturnsCorrectRole()
    {
        // Arrange
        const string expectedRole = "Admin";
        var claimsPrincipal = TestDataBuilder.CreateClaimsPrincipal("123", expectedRole);

        // Act
        var result = _jwtService.GetUserRoleFromClaims(claimsPrincipal);

        // Assert
        Assert.Equal(expectedRole, result);
    }

    [Fact]
    public void GetUserRoleFromClaims_WhenRoleClaimIsMissing_ThrowsInvalidOperationException()
    {
        // Arrange
        var claimsPrincipal = TestDataBuilder.CreateClaimsPrincipal("123", null);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            _jwtService.GetUserRoleFromClaims(claimsPrincipal));
        
        Assert.Equal("User role not found in claims", exception.Message);
    }

    [Fact]
    public void GetUserRoleFromClaims_WhenRoleClaimIsEmpty_ThrowsInvalidOperationException()
    {
        // Arrange
        var claimsPrincipal = TestDataBuilder.CreateClaimsPrincipal("123", "");

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            _jwtService.GetUserRoleFromClaims(claimsPrincipal));
        
        Assert.Equal("User role not found in claims", exception.Message);
    }

    [Theory]
    [InlineData("User")]
    [InlineData("Admin")]
    [InlineData("HotelOwner")]
    [InlineData("SuperAdmin")]
    public void GetUserRoleFromClaims_WithVariousValidRoles_ReturnsCorrectRole(string role)
    {
        // Arrange
        var claimsPrincipal = TestDataBuilder.CreateClaimsPrincipal("123", role);

        // Act
        var result = _jwtService.GetUserRoleFromClaims(claimsPrincipal);

        // Assert
        Assert.Equal(role, result);
    }

    #endregion

    #region GetUserInfoFromClaims Tests

    [Fact]
    public void GetUserInfoFromClaims_WhenBothClaimsArePresentAndValid_ReturnsCorrectUserIdAndRole()
    {
        // Arrange
        const int expectedUserId = 456;
        const string expectedRole = "HotelOwner";
        var claimsPrincipal = TestDataBuilder.CreateClaimsPrincipal(expectedUserId.ToString(), expectedRole);

        // Act
        var (userId, userRole) = _jwtService.GetUserInfoFromClaims(claimsPrincipal);

        // Assert
        Assert.Equal(expectedUserId, userId);
        Assert.Equal(expectedRole, userRole);
    }

    [Fact]
    public void GetUserInfoFromClaims_WhenNameIdentifierClaimIsMissing_ThrowsInvalidOperationException()
    {
        // Arrange
        var claimsPrincipal = TestDataBuilder.CreateClaimsPrincipal(null, "Admin");

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            _jwtService.GetUserInfoFromClaims(claimsPrincipal));
        
        Assert.Equal("User ID not found in claims", exception.Message);
    }

    [Fact]
    public void GetUserInfoFromClaims_WhenRoleClaimIsMissing_ThrowsInvalidOperationException()
    {
        // Arrange
        var claimsPrincipal = TestDataBuilder.CreateClaimsPrincipal("123", null);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            _jwtService.GetUserInfoFromClaims(claimsPrincipal));
        
        Assert.Equal("User role not found in claims", exception.Message);
    }

    [Fact]
    public void GetUserInfoFromClaims_WhenNameIdentifierClaimIsNonNumeric_ThrowsInvalidOperationException()
    {
        // Arrange
        var claimsPrincipal = TestDataBuilder.CreateClaimsPrincipal("invalid-id", "Admin");

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            _jwtService.GetUserInfoFromClaims(claimsPrincipal));
        
        Assert.Equal("Invalid user ID format in claims", exception.Message);
    }

    [Theory]
    [InlineData("789", "User")]
    [InlineData("101112", "Admin")]
    [InlineData("131415", "HotelOwner")]
    public void GetUserInfoFromClaims_WithVariousValidUserIdAndRoleCombinations_ReturnsCorrectTuple(string userIdString, string role)
    {
        // Arrange
        var expectedUserId = int.Parse(userIdString);
        var claimsPrincipal = TestDataBuilder.CreateClaimsPrincipal(userIdString, role);

        // Act
        var (userId, userRole) = _jwtService.GetUserInfoFromClaims(claimsPrincipal);

        // Assert
        Assert.Equal(expectedUserId, userId);
        Assert.Equal(role, userRole);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void GetUserIdFromClaims_WhenClaimsPrincipalIsNull_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            _jwtService.GetUserIdFromClaims(null!));
    }

    [Fact]
    public void GetUserRoleFromClaims_WhenClaimsPrincipalIsNull_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            _jwtService.GetUserRoleFromClaims(null!));
    }

    [Fact]
    public void GetUserInfoFromClaims_WhenClaimsPrincipalIsNull_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            _jwtService.GetUserInfoFromClaims(null!));
    }

    [Fact]
    public void GetUserIdFromClaims_WhenClaimsPrincipalHasNoIdentity_ThrowsInvalidOperationException()
    {
        // Arrange
        var claimsPrincipal = new ClaimsPrincipal();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            _jwtService.GetUserIdFromClaims(claimsPrincipal));
        
        Assert.Equal("User ID not found in claims", exception.Message);
    }

    [Fact]
    public void GetUserRoleFromClaims_WhenClaimsPrincipalHasNoIdentity_ThrowsInvalidOperationException()
    {
        // Arrange
        var claimsPrincipal = new ClaimsPrincipal();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            _jwtService.GetUserRoleFromClaims(claimsPrincipal));
        
        Assert.Equal("User role not found in claims", exception.Message);
    }

    #endregion
}
