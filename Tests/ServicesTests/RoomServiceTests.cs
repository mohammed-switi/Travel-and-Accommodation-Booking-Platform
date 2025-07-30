using Final_Project.Data;
using Final_Project.DTOs.Requests;
using Final_Project.Enums;
using Final_Project.Interfaces;
using Final_Project.Services;
using Final_Project.Tests.ServicesTests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Final_Project.Tests.ServicesTests;

public class RoomServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly Mock<IOwnershipValidationService> _mockOwnershipService;
    private readonly Mock<ILogger<RoomService>> _mockLogger;
    private readonly RoomService _roomService;
    private readonly TestDataBuilder _testDataBuilder;

    public RoomServiceTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _mockOwnershipService = new Mock<IOwnershipValidationService>();
        _mockLogger = new Mock<ILogger<RoomService>>();
        _roomService = new RoomService(_context, _mockOwnershipService.Object, _mockLogger.Object);
        _testDataBuilder = new TestDataBuilder(_context);
    }

    #region GetRoomByIdAsync Tests

    [Fact]
    public async Task GetRoomByIdAsync_WithValidId_ReturnsRoomDto()
    {
        // Arrange
        var room = await _testDataBuilder.SeedRoomWithHotelAsync();

        // Act
        var result = await _roomService.GetRoomByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Standard", result.RoomType);
        Assert.Equal(100m, result.Price);
        Assert.Equal(2, result.MaxAdults);
        Assert.Equal(1, result.MaxChildren);
        Assert.Equal(5, result.AvailableQuantity);
    }

    [Fact]
    public async Task GetRoomByIdAsync_WithNonExistentId_ReturnsNull()
    {
        // Act
        var result = await _roomService.GetRoomByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetRoomByIdAsync_CorrectlyMapsRoomProperties()
    {
        // Arrange
        var room = await _testDataBuilder.SeedRoomWithHotelAsync(1, 1, RoomType.Deluxe, 250.75m);

        // Act
        var result = await _roomService.GetRoomByIdAsync(1);

        // Assert
        Assert.Equal(room.Type.ToString(), result.RoomType);
        Assert.Equal(room.Discount, result.Price);
        Assert.Equal(room.MaxAdults, result.MaxAdults);
        Assert.Equal(room.MaxChildren, result.MaxChildren);
        Assert.Equal(room.Quantity, result.AvailableQuantity);
    }

    #endregion

    #region GetRoomsAsync Tests

    [Fact]
    public async Task GetRoomsAsync_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        await _testDataBuilder.SeedMultipleRoomsAsync(15);

        // Act
        var result = await _roomService.GetRoomsAsync(1, 10);

        // Assert
        Assert.Equal(10, result.Count);
    }

    [Fact]
    public async Task GetRoomsAsync_SecondPage_ReturnsRemainingRooms()
    {
        // Arrange
        await _testDataBuilder.SeedMultipleRoomsAsync(15);

        // Act
        var result = await _roomService.GetRoomsAsync(2, 10);

        // Assert
        Assert.Equal(5, result.Count);
    }

    [Fact]
    public async Task GetRoomsAsync_ExcludeInactiveHotels_ReturnsOnlyActiveHotelRooms()
    {
        // Arrange
        var (activeHotel, inactiveHotel, activeRoom, inactiveRoom) = 
            await _testDataBuilder.SeedActiveInactiveHotelsWithRoomsAsync();

        // Act
        var result = await _roomService.GetRoomsAsync(1, 10);

        // Assert
        Assert.Single(result);
        Assert.Equal(activeRoom.Id, result.First().Id);
    }

    [Fact]
    public async Task GetRoomsAsync_IncludeInactiveHotels_ReturnsAllRooms()
    {
        // Arrange
        await _testDataBuilder.SeedActiveInactiveHotelsWithRoomsAsync();

        // Act
        var result = await _roomService.GetRoomsAsync(1, 10, true);

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetRoomsAsync_EmptyDatabase_ReturnsEmptyList()
    {
        // Act
        var result = await _roomService.GetRoomsAsync(1, 10);

        // Assert
        Assert.Empty(result);
    }

    #endregion

    #region CreateRoomAsync Tests

    [Fact]
    public async Task CreateRoomAsync_WithValidData_CreatesRoom()
    {
        // Arrange
        var hotel = _testDataBuilder.CreateHotel().Build();
        _context.Hotels.Add(hotel);
        await _context.SaveChangesAsync();

        var createRoomDto = new CreateRoomRequestDto
        {
            RoomType = "Standard",
            Price = 150.00m,
            MaxAdults = 2,
            MaxChildren = 1,
            AvailableQuantity = 5,
            HotelId = 1,
            RoomNumber = "A101",
            ImageUrl = "https://example.com/room1.jpg"
        };

        _mockOwnershipService
            .Setup(x => x.CanUserManageHotelAsync(1, "Admin", 1))
            .ReturnsAsync(true);

        // Act
        var result = await _roomService.CreateRoomAsync(createRoomDto, 1, "Admin");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Standard", result.RoomType);
        Assert.Equal(150.00m, result.Price);

        // Verify mock was called with correct parameters
        _mockOwnershipService.Verify(
            x => x.CanUserManageHotelAsync(1, "Admin", 1),
            Times.Once);

        var roomInDb = await _context.Rooms.FindAsync(result.Id);
        Assert.NotNull(roomInDb);
        Assert.Equal(RoomType.Standard, roomInDb.Type);
    }

    [Fact]
    public async Task CreateRoomAsync_WithNonExistentHotel_ThrowsArgumentException()
    {
        // Arrange
        var createRoomDto = new CreateRoomRequestDto
        {
            RoomType = "Standard",
            Price = 150.00m,
            MaxAdults = 2,
            MaxChildren = 1,
            AvailableQuantity = 5,
            HotelId = 999
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _roomService.CreateRoomAsync(createRoomDto, 1, "Admin"));

        Assert.Contains("Hotel with ID 999 not found", exception.Message);
    }

    [Fact]
    public async Task CreateRoomAsync_WithoutPermission_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var hotel = _testDataBuilder.CreateHotel().Build();
        _context.Hotels.Add(hotel);
        await _context.SaveChangesAsync();

        var createRoomDto = new CreateRoomRequestDto
        {
            RoomType = "Standard",
            Price = 150.00m,
            MaxAdults = 2,
            MaxChildren = 1,
            AvailableQuantity = 5,
            HotelId = 1,
            RoomNumber = "A101",
            ImageUrl = "https://example.com/room1.jpg"
        };

        _mockOwnershipService
            .Setup(x => x.CanUserManageHotelAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(false);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _roomService.CreateRoomAsync(createRoomDto, 1, "User"));

        Assert.Contains("You don't have permission to create rooms in this hotel", exception.Message);
    }

    #endregion

    #region UpdateRoomAsync Tests

    [Fact]
    public async Task UpdateRoomAsync_WithValidData_UpdatesRoom()
    {
        // Arrange
        var room = await _testDataBuilder.SeedRoomWithHotelAsync();

        var updateRoomDto = new UpdateRoomRequestDto
        {
            RoomType = "Deluxe",
            Price = 200.00m,
            MaxAdults = 4,
            MaxChildren = 2,
            AvailableQuantity = 3
        };

        _mockOwnershipService.Setup(x => x.CanUserManageRoomAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(true);

        // Act
        var result = await _roomService.UpdateRoomAsync(1, updateRoomDto, 1, "Admin");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Deluxe", result.RoomType);
        Assert.Equal(200.00m, result.Price);
        Assert.Equal(4, result.MaxAdults);
        Assert.Equal(2, result.MaxChildren);
        Assert.Equal(3, result.AvailableQuantity);

        var roomInDb = await _context.Rooms.FindAsync(1);
        Assert.NotNull(roomInDb);
        Assert.Equal(RoomType.Deluxe, roomInDb.Type);
        Assert.Equal(200.00m, roomInDb.Discount);
    }

    [Fact]
    public async Task UpdateRoomAsync_WithoutPermission_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var room = await _testDataBuilder.SeedRoomWithHotelAsync();

        var updateRoomDto = new UpdateRoomRequestDto
        {
            RoomType = "Deluxe",
            Price = 200.00m,
            MaxAdults = 4,
            MaxChildren = 2,
            AvailableQuantity = 3
        };

        _mockOwnershipService.Setup(x => x.CanUserManageRoomAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(false);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _roomService.UpdateRoomAsync(1, updateRoomDto, 1, "User"));

        Assert.Contains("You don't have permission to update this room", exception.Message);
    }

    [Fact]
    public async Task UpdateRoomAsync_WithNonExistentRoom_ThrowsArgumentException()
    {
        // Arrange
        var updateRoomDto = new UpdateRoomRequestDto
        {
            RoomType = "Deluxe",
            Price = 200.00m,
            MaxAdults = 4,
            MaxChildren = 2,
            AvailableQuantity = 3
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _roomService.UpdateRoomAsync(999, updateRoomDto, 1, "Admin"));
    }

    #endregion

    #region DeleteRoomAsync Tests

    [Fact]
    public async Task DeleteRoomAsync_WithValidId_DeletesRoom()
    {
        // Arrange
        var room = await _testDataBuilder.SeedRoomWithHotelAsync();

        _mockOwnershipService.Setup(x => x.CanUserManageRoomAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(true);

        // Act
        var result = await _roomService.DeleteRoomAsync(1, 1, "Admin");

        // Assert
        Assert.True(result);
        var roomInDb = await _context.Rooms.FindAsync(1);
        Assert.Null(roomInDb);
    }

    [Fact]
    public async Task DeleteRoomAsync_WithNonExistentRoom_ReturnsFalse()
    {
        // Act
        var result = await _roomService.DeleteRoomAsync(999, 1, "Admin");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteRoomAsync_WithActiveBookings_ThrowsInvalidOperationException()
    {
        // Arrange
        var (room, booking, bookingItem) = await _testDataBuilder.SeedRoomWithBookingAsync(BookingStatus.Approved);

        _mockOwnershipService.Setup(x => x.CanUserManageRoomAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(true);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _roomService.DeleteRoomAsync(room.Id, 1, "Admin"));

        Assert.Contains("Cannot delete room with active bookings", exception.Message);
    }

    [Fact]
    public async Task DeleteRoomAsync_WithCancelledBookings_DeletesSuccessfully()
    {
        // Arrange
        var (room, booking, bookingItem) = await _testDataBuilder.SeedRoomWithBookingAsync(BookingStatus.Cancelled);

        _mockOwnershipService.Setup(x => x.CanUserManageRoomAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(true);

        // Act
        var result = await _roomService.DeleteRoomAsync(room.Id, 1, "Admin");

        // Assert
        Assert.True(result);
        var roomInDb = await _context.Rooms.FindAsync(room.Id);
        Assert.Null(roomInDb);
    }

    #endregion

    #region Additional Edge Case Tests

    [Fact]
    public async Task GetRoomsAsync_WithInvalidPaginationParameters_HandlesGracefully()
    {
        // Arrange
        await _testDataBuilder.SeedRoomWithHotelAsync();

        // Act & Assert - Test negative page numbers
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            await _roomService.GetRoomsAsync(-1, 10));

        // Test zero page size
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            await _roomService.GetRoomsAsync(1, 0));

        // Test large page size
        var result3 = await _roomService.GetRoomsAsync(1, 1000);
        Assert.Single(result3);
    }

    [Theory]
    [InlineData("InvalidRoomType")]
    [InlineData("")]
    public async Task CreateRoomAsync_WithInvalidRoomType_ThrowsArgumentException(string invalidRoomType)
    {
        // Arrange
        await _testDataBuilder.SeedRoomWithHotelAsync();

        var createRoomDto = new CreateRoomRequestDto
        {
            RoomType = invalidRoomType,
            Price = 150.00m,
            MaxAdults = 2,
            MaxChildren = 1,
            AvailableQuantity = 5,
            HotelId = 1
        };

        _mockOwnershipService
            .Setup(x => x.CanUserManageHotelAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(true);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _roomService.CreateRoomAsync(createRoomDto, 1, "Admin"));
    }

    
    #endregion

    public void Dispose()
    {
        _context.Dispose();
    }
}