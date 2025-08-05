using Final_Project.Constants;
using Final_Project.Data;
using Final_Project.Services;
using Final_Project.Tests.ServicesTests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Final_Project.Tests.ServicesTests;

public class OwnershipValidationServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly Mock<ILogger<OwnershipValidationService>> _loggerMock;
    private readonly OwnershipValidationService _service;
    private readonly TestDataBuilder _testDataBuilder;

    public OwnershipValidationServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _loggerMock = new Mock<ILogger<OwnershipValidationService>>();
        _service = new OwnershipValidationService(_context, _loggerMock.Object);
        _testDataBuilder = new TestDataBuilder(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    #region CanUserCreateHotelAsync Tests

    [Fact]
    public async Task CanUserCreateHotelAsync_AdminRole_ReturnsTrue()
    {
        // Act
        var result = await _service.CanUserCreateHotelAsync(UserRoles.Admin, 1, 2);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CanUserCreateHotelAsync_HotelOwnerWithMatchingIds_ReturnsTrue()
    {
        // Arrange
        const int userId = 1;
        const int ownerId = 1;

        // Act
        var result = await _service.CanUserCreateHotelAsync(UserRoles.HotelOwner, ownerId, userId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CanUserCreateHotelAsync_HotelOwnerWithNonMatchingIds_ReturnsFalse()
    {
        // Arrange
        const int userId = 1;
        const int ownerId = 2;

        // Act
        var result = await _service.CanUserCreateHotelAsync(UserRoles.HotelOwner, ownerId, userId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CanUserCreateHotelAsync_RegularUser_ReturnsFalse()
    {
        // Act
        var result = await _service.CanUserCreateHotelAsync(UserRoles.User, 1, 1);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CanUserCreateHotelAsync_InvalidRole_ReturnsFalse()
    {
        // Act
        var result = await _service.CanUserCreateHotelAsync("InvalidRole", 1, 1);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region CanUserManageHotelAsync Tests

    [Fact]
    public async Task CanUserManageHotelAsync_AdminRole_ReturnsTrue()
    {
        // Act
        var result = await _service.CanUserManageHotelAsync(1, UserRoles.Admin, 1);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CanUserManageHotelAsync_HotelOwnerWithOwnedHotel_ReturnsTrue()
    {
        // Arrange
        const int userId = 1;
        const int hotelId = 1;
        var (hotel, _, _, _, _) = await _testDataBuilder.SeedHotelWithRelatedDataAsync(hotelId);

        // Act
        var result = await _service.CanUserManageHotelAsync(userId, UserRoles.HotelOwner, hotelId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CanUserManageHotelAsync_HotelOwnerWithNonOwnedHotel_ReturnsFalse()
    {
        // Arrange
        const int userId = 2; // Different user
        const int hotelId = 1;
        const int ownerId = 1; // Hotel owned by user 1
        var city = _testDataBuilder.CreateCity().Build();
        var owner = _testDataBuilder.CreateUser(ownerId).Build();
        var hotel = _testDataBuilder.CreateHotel(hotelId).WithOwner(ownerId).Build();
        
        _context.Cities.Add(city);
        _context.Users.Add(owner);
        _context.Hotels.Add(hotel);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.CanUserManageHotelAsync(userId, UserRoles.HotelOwner, hotelId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CanUserManageHotelAsync_HotelOwnerWithNonExistentHotel_ReturnsFalse()
    {
        // Act
        var result = await _service.CanUserManageHotelAsync(1, UserRoles.HotelOwner, 999);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CanUserManageHotelAsync_RegularUser_ReturnsFalse()
    {
        // Act
        var result = await _service.CanUserManageHotelAsync(1, UserRoles.User, 1);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region CanUserManageRoomAsync Tests

    [Fact]
    public async Task CanUserManageRoomAsync_AdminRole_ReturnsTrue()
    {
        // Act
        var result = await _service.CanUserManageRoomAsync(1, UserRoles.Admin, 1);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CanUserManageRoomAsync_HotelOwnerWithOwnedRoom_ReturnsTrue()
    {
        // Arrange
        const int userId = 1;
        const int roomId = 1;
        var room = await _testDataBuilder.SeedRoomWithHotelAsync(roomId);

        // Act
        var result = await _service.CanUserManageRoomAsync(userId, UserRoles.HotelOwner, roomId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CanUserManageRoomAsync_HotelOwnerWithNonOwnedRoom_ReturnsFalse()
    {
        // Arrange
        const int userId = 2; // Different user
        const int roomId = 1;
        const int hotelId = 1;
        const int ownerId = 1; // Hotel owned by user 1
        
        var city = _testDataBuilder.CreateCity().Build();
        var owner = _testDataBuilder.CreateUser(ownerId).Build();
        var hotel = _testDataBuilder.CreateHotel(hotelId).WithOwner(ownerId).Build();
        var room = _testDataBuilder.CreateRoom(roomId).WithHotel(hotelId).Build();
        
        _context.Cities.Add(city);
        _context.Users.Add(owner);
        _context.Hotels.Add(hotel);
        _context.Rooms.Add(room);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.CanUserManageRoomAsync(userId, UserRoles.HotelOwner, roomId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CanUserManageRoomAsync_NonExistentRoom_ReturnsFalse()
    {
        // Act
        var result = await _service.CanUserManageRoomAsync(1, UserRoles.HotelOwner, 999);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CanUserManageRoomAsync_RegularUser_ReturnsFalse()
    {
        // Arrange
        const int userId = 1;
        const int roomId = 1;
        await _testDataBuilder.SeedRoomWithHotelAsync(roomId);

        // Act
        var result = await _service.CanUserManageRoomAsync(userId, UserRoles.User, roomId);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region IsHotelOwnerAsync Tests

    [Fact]
    public async Task IsHotelOwnerAsync_UserOwnsHotel_ReturnsTrue()
    {
        // Arrange
        const int userId = 1;
        const int hotelId = 1;
        var (hotel, _, _, _, _) = await _testDataBuilder.SeedHotelWithRelatedDataAsync(hotelId);

        // Act
        var result = await _service.IsHotelOwnerAsync(userId, hotelId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsHotelOwnerAsync_UserDoesNotOwnHotel_ReturnsFalse()
    {
        // Arrange
        const int userId = 2; // Different user
        const int hotelId = 1;
        const int ownerId = 1; // Hotel owned by user 1
        
        var city = _testDataBuilder.CreateCity().Build();
        var owner = _testDataBuilder.CreateUser(ownerId).Build();
        var hotel = _testDataBuilder.CreateHotel(hotelId).WithOwner(ownerId).Build();
        
        _context.Cities.Add(city);
        _context.Users.Add(owner);
        _context.Hotels.Add(hotel);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.IsHotelOwnerAsync(userId, hotelId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsHotelOwnerAsync_HotelDoesNotExist_ReturnsFalse()
    {
        // Act
        var result = await _service.IsHotelOwnerAsync(1, 999);

        // Assert
        Assert.False(result);
    }

    #endregion
}