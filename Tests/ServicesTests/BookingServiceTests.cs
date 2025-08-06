using Final_Project.Data;
using Final_Project.Dtos;
using Final_Project.Enums;
using Final_Project.Models;
using Final_Project.Services;
using Final_Project.Tests.ServicesTests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Final_Project.Tests.ServicesTests;

public class BookingServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly Mock<IRoomAvailabilityService> _mockAvailabilityService;
    private readonly Mock<ILogger<BookingService>> _loggerMock;
    private readonly BookingService _service;
    private readonly TestDataBuilder _testDataBuilder;

    public BookingServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _mockAvailabilityService = new Mock<IRoomAvailabilityService>();
        _loggerMock = new Mock<ILogger<BookingService>>();
        _service = new BookingService(_context, _mockAvailabilityService.Object, _loggerMock.Object);
        _testDataBuilder = new TestDataBuilder(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    #region GetOrCreateCartAsync Tests

    [Fact]
    public async Task GetOrCreateCartAsync_ExistingCart_ReturnsExistingCart()
    {
        // Arrange
        const int userId = 1;
        var user = _testDataBuilder.CreateUser(userId).Build();
        var existingCart = new BookingCart { UserId = userId, Items = new List<BookingCartItem>() };
        
        _context.Users.Add(user);
        _context.BookingCarts.Add(existingCart);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetOrCreateCartAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.UserId);
        Assert.Equal(existingCart.Id, result.Id);
    }

    [Fact]
    public async Task GetOrCreateCartAsync_NoExistingCart_CreatesNewCart()
    {
        // Arrange
        const int userId = 1;

        // Act
        var result = await _service.GetOrCreateCartAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.UserId);
        Assert.NotEqual(0, result.Id); // Should have been saved to database
    }

    #endregion

    #region AddToCartAsync Tests

    [Fact]
    public async Task AddToCartAsync_ValidRequest_AddsItemToCart()
    {
        // Arrange
        const int userId = 1;
        const int roomId = 1;
        const decimal pricePerNight = 100m;
        var checkInDate = DateTime.Today.AddDays(1);
        var checkOutDate = DateTime.Today.AddDays(3);
        
        var room = await _testDataBuilder.SeedRoomWithHotelAsync(roomId, price: pricePerNight);
        _mockAvailabilityService.Setup(x => x.IsRoomAvailableAsync(roomId, checkInDate, checkOutDate))
            .ReturnsAsync(true);

        // Act
        var result = await _service.AddToCartAsync(userId, roomId, checkInDate, checkOutDate);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items);
        var cartItem = result.Items.First();
        Assert.Equal(roomId, cartItem.RoomId);
        Assert.Equal(checkInDate, cartItem.CheckInDate);
        Assert.Equal(checkOutDate, cartItem.CheckOutDate);
        Assert.Equal(pricePerNight * 2, cartItem.Price); // 2 nights
    }

    [Fact]
    public async Task AddToCartAsync_InvalidDates_ThrowsArgumentException()
    {
        // Arrange
        const int userId = 1;
        const int roomId = 1;
        var checkInDate = DateTime.Today.AddDays(3);
        var checkOutDate = DateTime.Today.AddDays(1); // Invalid: checkout before checkin

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.AddToCartAsync(userId, roomId, checkInDate, checkOutDate));
        
        Assert.Contains("Check-in date must be before check-out date", exception.Message);
    }

    [Fact]
    public async Task AddToCartAsync_RoomNotFound_ThrowsArgumentException()
    {
        // Arrange
        const int userId = 1;
        const int nonExistentRoomId = 999;
        var checkInDate = DateTime.Today.AddDays(1);
        var checkOutDate = DateTime.Today.AddDays(3);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.AddToCartAsync(userId, nonExistentRoomId, checkInDate, checkOutDate));
        
        Assert.Contains("Room not found", exception.Message);
    }

    [Fact]
    public async Task AddToCartAsync_RoomNotAvailable_ThrowsInvalidOperationException()
    {
        // Arrange
        const int userId = 1;
        const int roomId = 1;
        var checkInDate = DateTime.Today.AddDays(1);
        var checkOutDate = DateTime.Today.AddDays(3);
        
        await _testDataBuilder.SeedRoomWithHotelAsync(roomId);
        _mockAvailabilityService.Setup(x => x.IsRoomAvailableAsync(roomId, checkInDate, checkOutDate))
            .ReturnsAsync(false);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.AddToCartAsync(userId, roomId, checkInDate, checkOutDate));
        
        Assert.Contains("Room is not available for the selected dates", exception.Message);
    }

    [Fact]
    public async Task AddToCartAsync_CalculatesCorrectPrice_ForMultipleNights()
    {
        // Arrange
        const int userId = 1;
        const int roomId = 1;
        const decimal pricePerNight = 150m;
        var checkInDate = DateTime.Today.AddDays(1);
        var checkOutDate = DateTime.Today.AddDays(6); // 5 nights
        
        var room = await _testDataBuilder.SeedRoomWithHotelAsync(roomId, price: pricePerNight);
        _mockAvailabilityService.Setup(x => x.IsRoomAvailableAsync(roomId, checkInDate, checkOutDate))
            .ReturnsAsync(true);

        // Act
        var result = await _service.AddToCartAsync(userId, roomId, checkInDate, checkOutDate);

        // Assert
        var cartItem = result.Items.First();
        Assert.Equal(pricePerNight * 5, cartItem.Price); // 5 nights * 150 = 750
    }

    #endregion

    #region RemoveFromCartAsync Tests

    [Fact]
    public async Task RemoveFromCartAsync_ExistingItem_RemovesItemFromCart()
    {
        // Arrange
        const int userId = 1;
        const int roomId = 1;
        var checkInDate = DateTime.Today.AddDays(1);
        var checkOutDate = DateTime.Today.AddDays(3);
        
        await _testDataBuilder.SeedRoomWithHotelAsync(roomId);
        _mockAvailabilityService.Setup(x => x.IsRoomAvailableAsync(roomId, checkInDate, checkOutDate))
            .ReturnsAsync(true);
        
        // Add item to cart first
        var cart = await _service.AddToCartAsync(userId, roomId, checkInDate, checkOutDate);
        var cartItemId = cart.Items.First().Id;

        // Act
        var result = await _service.RemoveFromCartAsync(userId, cartItemId);

        // Assert
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task RemoveFromCartAsync_NonExistentItem_DoesNotThrow()
    {
        // Arrange
        const int userId = 1;
        const int nonExistentItemId = 999;
        
        // Create empty cart
        await _service.GetOrCreateCartAsync(userId);

        // Act & Assert - Should not throw
        var result = await _service.RemoveFromCartAsync(userId, nonExistentItemId);
        Assert.NotNull(result);
    }

    #endregion

    #region GetCartAsync Tests

    [Fact]
    public async Task GetCartAsync_ExistingCart_ReturnsCartWithItems()
    {
        // Arrange
        const int userId = 1;
        const int roomId = 1;
        var checkInDate = DateTime.Today.AddDays(1);
        var checkOutDate = DateTime.Today.AddDays(3);
        
        await _testDataBuilder.SeedRoomWithHotelAsync(roomId);
        _mockAvailabilityService.Setup(x => x.IsRoomAvailableAsync(roomId, checkInDate, checkOutDate))
            .ReturnsAsync(true);
        
        // Add item to cart first
        await _service.AddToCartAsync(userId, roomId, checkInDate, checkOutDate);

        // Act
        var result = await _service.GetCartAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.UserId);
        Assert.Single(result.Items);
        Assert.NotNull(result.Items.First().Room);
        Assert.NotNull(result.Items.First().Room.Hotel);
    }

    [Fact]
    public async Task GetCartAsync_NoCart_ThrowsInvalidOperationException()
    {
        // Arrange
        const int userId = 1;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.GetCartAsync(userId));
        
        Assert.Contains("Cart not found", exception.Message);
    }

    #endregion

    #region CreateBookingAsync Tests

    [Fact]
    public async Task CreateBookingAsync_ValidCart_CreatesBookingAndClearsCart()
    {
        // Arrange
        const int userId = 1;
        const int roomId = 1;
        const decimal pricePerNight = 100m;
        var checkInDate = DateTime.Today.AddDays(1);
        var checkOutDate = DateTime.Today.AddDays(3);
        
        await _testDataBuilder.SeedRoomWithHotelAsync(roomId, price: pricePerNight);
        _mockAvailabilityService.Setup(x => x.IsRoomAvailableAsync(roomId, checkInDate, checkOutDate))
            .ReturnsAsync(true);
        
        // Add item to cart
        await _service.AddToCartAsync(userId, roomId, checkInDate, checkOutDate);

        var checkoutDto = new BookingCheckoutDto
        {
            ContactName = "Test User",
            ContactPhone = "123456789",
            ContactEmail = "test@test.com",
            PaymentMethod = "Credit Card",
            SpecialRequests = "No special requests"
        };

        // Act
        var result = await _service.CreateBookingAsync(userId, checkoutDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.UserId);
        Assert.NotNull(result.BookingReference);
        Assert.Equal(checkoutDto.ContactName, result.ContactName);
        Assert.Equal(checkoutDto.ContactPhone, result.ContactPhone);
        Assert.Equal(checkoutDto.ContactEmail, result.ContactEmail);
        Assert.Equal(pricePerNight * 2, result.TotalPrice); // 2 nights
        Assert.Single(result.Items);
        
        // Verify cart was cleared
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.GetCartAsync(userId));
        Assert.Contains("Cart not found", exception.Message);
    }

    [Fact]
    public async Task CreateBookingAsync_EmptyCart_ThrowsInvalidOperationException()
    {
        // Arrange
        const int userId = 1;
        await _service.GetOrCreateCartAsync(userId); // Create empty cart

        var checkoutDto = new BookingCheckoutDto
        {
            ContactName = "Test User",
            ContactPhone = "123456789",
            ContactEmail = "test@test.com",
            PaymentMethod = "Credit Card"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateBookingAsync(userId, checkoutDto));
        
        Assert.Contains("Cannot create booking with empty cart", exception.Message);
    }

    [Fact]
    public async Task CreateBookingAsync_MultipleItems_CalculatesTotalPriceCorrectly()
    {
        // Arrange
        const int userId = 1;
        const decimal pricePerNight1 = 100m;
        const decimal pricePerNight2 = 150m;
        var checkInDate = DateTime.Today.AddDays(1);
        var checkOutDate = DateTime.Today.AddDays(3);
        
        // Create two rooms
        City city = _testDataBuilder.CreateCity(1).Build();
        Hotel hotel1 = _testDataBuilder.CreateHotel(1).WithCity(1).Build();
        User user = _testDataBuilder.CreateUser(userId).Build();
        
        
        await _testDataBuilder.SeedRoomWithHotelAsync(1, price: pricePerNight1,city:city,hotel:hotel1,owner:user);
        await _testDataBuilder.SeedRoomWithHotelAsync(2, hotelId: 2, price: pricePerNight2,city:city,hotel:hotel1,owner:user);
        
        _mockAvailabilityService.Setup(x => x.IsRoomAvailableAsync(It.IsAny<int>(), checkInDate, checkOutDate))
            .ReturnsAsync(true);
        
        // Add both items to cart
        await _service.AddToCartAsync(userId, 1, checkInDate, checkOutDate);
        await _service.AddToCartAsync(userId, 2, checkInDate, checkOutDate);

        var checkoutDto = new BookingCheckoutDto
        {
            ContactName = "Test User",
            ContactPhone = "123456789",
            ContactEmail = "test@test.com",
            PaymentMethod = "Credit Card"
        };

        // Act
        var result = await _service.CreateBookingAsync(userId, checkoutDto);

        // Assert
        Assert.Equal((pricePerNight1 + pricePerNight2) * 2, result.TotalPrice); // 2 nights for both rooms
        Assert.Equal(2, result.Items.Count);
    }

    #endregion

    #region GetBookingAsync Tests

    [Fact]
    public async Task GetBookingAsync_ExistingBooking_ReturnsBookingWithDetails()
    {
        // Arrange
        const int userId = 1;
        var (room, booking, bookingItem) = await _testDataBuilder.SeedRoomWithBookingAsync();

        // Act
        var result = await _service.GetBookingAsync(booking.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(booking.Id, result.Id);
        Assert.NotNull(result.Items);
        Assert.Single(result.Items);
        Assert.NotNull(result.Items.First().Room);
        Assert.NotNull(result.Items.First().Room.Hotel);
    }

    [Fact]
    public async Task GetBookingAsync_NonExistentBooking_ThrowsInvalidOperationException()
    {
        // Arrange
        const int nonExistentBookingId = 999;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.GetBookingAsync(nonExistentBookingId));
        
        Assert.Contains("Booking not found", exception.Message);
    }

    #endregion

    #region GetBookingByReferenceAsync Tests

    [Fact]
    public async Task GetBookingByReferenceAsync_ExistingReference_ReturnsBooking()
    {
        // Arrange
        var (room, booking, bookingItem) = await _testDataBuilder.SeedRoomWithBookingAsync();
        
        // Update booking with a known reference
        booking.BookingReference = "TEST-REF-123";
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetBookingByReferenceAsync("TEST-REF-123");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(booking.Id, result.Id);
        Assert.Equal("TEST-REF-123", result.BookingReference);
    }

    [Fact]
    public async Task GetBookingByReferenceAsync_NonExistentReference_ThrowsInvalidOperationException()
    {
        // Arrange
        const string nonExistentReference = "NON-EXISTENT-REF";

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.GetBookingByReferenceAsync(nonExistentReference));
    }

    #endregion

    #region GetUserBookingsAsync Tests

    [Fact]
    public async Task GetUserBookingsAsync_UserWithBookings_ReturnsUserBookings()
    {
        // Arrange
        const int userId = 1;
        const int otherUserId = 3;
        
        // Create bookings for both users
        var (room1, booking1,_) = await _testDataBuilder.SeedRoomWithBookingAsync();
        booking1.UserId = userId;
        
        var user2 = _testDataBuilder.CreateUser(otherUserId).Build();
        var booking2 = _testDataBuilder.CreateBooking(2).WithUser(otherUserId).Build();
        
        _context.Users.Add(user2);
        _context.Bookings.Add(booking2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetUserBookingsAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(userId, result.First().UserId);
        Assert.Equal(booking1.Id, result.First().Id);
    }

    [Fact]
    public async Task GetUserBookingsAsync_UserWithNoBookings_ReturnsEmptyList()
    {
        // Arrange
        const int userId = 1;

        // Act
        var result = await _service.GetUserBookingsAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    #endregion
}
