using Final_Project.Constants;
using Final_Project.Data;
using Final_Project.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Final_Project.Tests.ServicesTests;

public class OwnershipValidationServiceTest
{
    
    private readonly AppDbContext _context;
    private readonly Mock<ILogger<OwnershipValidationService>> _loggerMock;
    private readonly OwnershipValidationService _service;

    public OwnershipValidationServiceTest()
    {
         var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _loggerMock = new Mock<ILogger<OwnershipValidationService>>();
        _service = new OwnershipValidationService(_context, _loggerMock.Object);
    }
   
    [Fact]
    public async Task CanUserCreateHotelAsync_ShouldReturnTrue_WhenUserRoleIsAdminOrHotelOwner()
    {
        // Arrange
        var userRoleAdmin = UserRoles.Admin;
        var userRoleHotelOwner = UserRoles.HotelOwner;

        // Act
        var resultAdmin = await _service.CanUserCreateHotelAsync(userRoleAdmin);
        var resultHotelOwner = await _service.CanUserCreateHotelAsync(userRoleHotelOwner);

        // Assert
        Assert.True(resultAdmin);
        Assert.True(resultHotelOwner);
    }
    
    

    // Add test methods here
    
}