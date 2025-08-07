using Final_Project.Data;
using Final_Project.Enums;
using Final_Project.Models;
using Final_Project.Services;
using Final_Project.Tests.ServicesTests.Helpers;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Final_Project.Tests.ServicesTests;

public class RoomAvailabilityServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly RoomAvailabilityService _roomAvailabilityService;
    private readonly TestDataBuilder _testDataBuilder;

    public RoomAvailabilityServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);
        _testDataBuilder = new TestDataBuilder(_context);
        _roomAvailabilityService = new RoomAvailabilityService(_context);
    }

    #region GetRoomAvailabilityAsync Tests

    [Fact]
    public async Task GetRoomAvailabilityAsync_ReturnsListOfRoomsWithCorrectAvailabilityWhenValidDatesAndOverlappingBookingsExist()
    {
        // Arrange
        var (hotelId, roomIds) = await SeedHotelWithRoomsAndBookingsAsync();
        var checkIn = DateTime.Today.AddDays(5);
        var checkOut = DateTime.Today.AddDays(7);

        // Act
        var result = await _roomAvailabilityService.GetRoomAvailabilityAsync(hotelId, checkIn, checkOut);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count); // 3 rooms in hotel

        // Room 1: Quantity 5, has 2 overlapping bookings, should have 3 available
        var room1 = result.First(r => r.Id == roomIds[0]);
        Assert.Equal(3, room1.AvailableQuantity);

        // Room 2: Quantity 3, has 1 overlapping booking, should have 2 available
        var room2 = result.First(r => r.Id == roomIds[1]);
        Assert.Equal(2, room2.AvailableQuantity);

        // Room 3: Quantity 4, has no overlapping bookings, should have 4 available
        var room3 = result.First(r => r.Id == roomIds[2]);
        Assert.Equal(4, room3.AvailableQuantity);

        // Verify other properties are correctly mapped
        Assert.All(result, room =>
        {
            Assert.True(room.Id > 0);
            Assert.NotNull(room.RoomType);
            Assert.True(room.Price > 0);
            Assert.True(room.MaxAdults > 0);
            Assert.True(room.AvailableQuantity >= 0);
        });
    }

    [Fact]
    public async Task GetRoomAvailabilityAsync_ReturnsAllRoomsWithFullQuantityWhenNoCheckInCheckOutDatesProvided()
    {
        // Arrange
        var (hotelId, roomIds) = await SeedHotelWithRoomsAndBookingsAsync();

        // Act - No check-in/check-out dates provided
        var result = await _roomAvailabilityService.GetRoomAvailabilityAsync(hotelId, null, null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);

        // All rooms should have their full quantity available
        var room1 = result.First(r => r.Id == roomIds[0]);
        Assert.Equal(5, room1.AvailableQuantity); // Full quantity

        var room2 = result.First(r => r.Id == roomIds[1]);
        Assert.Equal(3, room2.AvailableQuantity); // Full quantity

        var room3 = result.First(r => r.Id == roomIds[2]);
        Assert.Equal(4, room3.AvailableQuantity); // Full quantity
    }

    [Fact]
    public async Task GetRoomAvailabilityAsync_IgnoresOverlappingBookingsWhenCheckOutLessOrEqualCheckIn()
    {
        // Arrange
        var (hotelId, roomIds) = await SeedHotelWithRoomsAndBookingsAsync();
        var checkIn = DateTime.Today.AddDays(7);
        var checkOut = DateTime.Today.AddDays(5); // checkOut < checkIn

        // Act
        var result = await _roomAvailabilityService.GetRoomAvailabilityAsync(hotelId, checkIn, checkOut);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);

        // All rooms should have their full quantity available since invalid date range is ignored
        var room1 = result.First(r => r.Id == roomIds[0]);
        Assert.Equal(5, room1.AvailableQuantity);

        var room2 = result.First(r => r.Id == roomIds[1]);
        Assert.Equal(3, room2.AvailableQuantity);

        var room3 = result.First(r => r.Id == roomIds[2]);
        Assert.Equal(4, room3.AvailableQuantity);
    }

    [Fact]
    public async Task GetRoomAvailabilityAsync_IgnoresOverlappingBookingsWhenCheckOutEqualsCheckIn()
    {
        // Arrange
        var (hotelId, roomIds) = await SeedHotelWithRoomsAndBookingsAsync();
        var checkIn = DateTime.Today.AddDays(5);
        var checkOut = DateTime.Today.AddDays(5); // checkOut == checkIn

        // Act
        var result = await _roomAvailabilityService.GetRoomAvailabilityAsync(hotelId, checkIn, checkOut);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);

        // All rooms should have their full quantity available
        Assert.All(result, room => Assert.True(room.AvailableQuantity > 0));
    }

    [Fact]
    public async Task GetRoomAvailabilityAsync_CalculatesAvailabilityCorrectlyWhenSomeRoomsPartiallyBooked()
    {
        // Arrange
        var (hotelId, roomIds) = await SeedHotelWithPartialBookingsAsync();
        var checkIn = DateTime.Today.AddDays(10);
        var checkOut = DateTime.Today.AddDays(12);

        // Act
        var result = await _roomAvailabilityService.GetRoomAvailabilityAsync(hotelId, checkIn, checkOut);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);

        // Room 1: Quantity 10, has 3 bookings in the period, should have 7 available
        var room1 = result.First(r => r.Id == roomIds[0]);
        Assert.Equal(7, room1.AvailableQuantity);

        // Room 2: Quantity 8, has 1 booking in the period, should have 7 available
        var room2 = result.First(r => r.Id == roomIds[1]);
        Assert.Equal(7, room2.AvailableQuantity);
    }

    [Fact]
    public async Task GetRoomAvailabilityAsync_ReturnsEmptyListWhenHotelHasNoRooms()
    {
        // Arrange - Create hotel without rooms
        var hotel = await SeedHotelWithoutRoomsAsync();

        // Act
        var result = await _roomAvailabilityService.GetRoomAvailabilityAsync(hotel.Id, DateTime.Today, DateTime.Today.AddDays(2));

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    #endregion

    #region IsRoomAvailableAsync Tests

    [Fact]
    public async Task IsRoomAvailableAsync_ReturnsTrueWhenNoOverlappingBookingsExist()
    {
        // Arrange
        var (hotelId, roomIds) = await SeedHotelWithRoomsAndBookingsAsync();
        var roomId = roomIds[2]; // Room 3 has no overlapping bookings in the setup
        var checkIn = DateTime.Today.AddDays(5);
        var checkOut = DateTime.Today.AddDays(7);

        // Act
        var result = await _roomAvailabilityService.IsRoomAvailableAsync(roomId, checkIn, checkOut);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsRoomAvailableAsync_ReturnsFalseWhenOverlappingBookingsFound()
    {
        // Arrange
        var (hotelId, roomIds) = await SeedHotelWithRoomsAndBookingsAsync();
        var roomId = roomIds[0]; // Room 1 has overlapping bookings
        var checkIn = DateTime.Today.AddDays(5);
        var checkOut = DateTime.Today.AddDays(7);

        // Act
        var result = await _roomAvailabilityService.IsRoomAvailableAsync(roomId, checkIn, checkOut);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsRoomAvailableAsync_ReturnsTrueWhenCheckOutEqualsNextCheckIn()
    {
        // Arrange
        var (hotelId, roomIds) = await SeedHotelWithRoomsAndBookingsAsync();
        var roomId = roomIds[2];
        
        // Create a booking that ends exactly when our new booking starts
        var existingBooking = _testDataBuilder.CreateBooking(999).Build();
        var existingBookingItem = _testDataBuilder.CreateBookingItem(999)
            .WithBooking(999)
            .WithRoom(roomId)
            .WithDates(DateTime.Today.AddDays(1), DateTime.Today.AddDays(3)) // Ends on day 3
            .Build();

        _context.Bookings.Add(existingBooking);
        _context.BookingItems.Add(existingBookingItem);
        await _context.SaveChangesAsync();

        // Act - Check availability starting exactly when existing booking ends
        var result = await _roomAvailabilityService.IsRoomAvailableAsync(roomId, DateTime.Today.AddDays(3), DateTime.Today.AddDays(5));

        // Assert
        Assert.True(result); // Should be available because checkout == checkin is not overlapping
    }

    [Fact]
    public async Task IsRoomAvailableAsync_ReturnsTrueWhenCheckInEqualsNextCheckOut()
    {
        // Arrange
        var (hotelId, roomIds) = await SeedHotelWithRoomsAndBookingsAsync();
        var roomId = roomIds[2]; // Use room 3 which has no existing overlapping bookings
        
        // Create a booking that starts exactly when our new booking ends
        var existingBooking = _testDataBuilder.CreateBooking(998).Build();
        var existingBookingItem = _testDataBuilder.CreateBookingItem(998)
            .WithBooking(998)
            .WithRoom(roomId)
            .WithDates(DateTime.Today.AddDays(5), DateTime.Today.AddDays(7)) // Starts on day 5
            .Build();

        _context.Bookings.Add(existingBooking);
        _context.BookingItems.Add(existingBookingItem);
        await _context.SaveChangesAsync();

        // Act - Check availability ending exactly when existing booking starts
        var result = await _roomAvailabilityService.IsRoomAvailableAsync(roomId, DateTime.Today.AddDays(3), DateTime.Today.AddDays(5));

        // Assert
        Assert.True(result); // Should be available because checkout == checkin is not overlapping
    }

    [Fact]
    public async Task IsRoomAvailableAsync_ReturnsFalseWhenBookingOverlapsPartially()
    {
        // Arrange
        var (hotelId, roomIds) = await SeedHotelWithRoomsAndBookingsAsync();
        var roomId = roomIds[0];
        
        // Create an overlapping booking
        var existingBooking = _testDataBuilder.CreateBooking(997).Build();
        var existingBookingItem = _testDataBuilder.CreateBookingItem(997)
            .WithBooking(997)
            .WithRoom(roomId)
            .WithDates(DateTime.Today.AddDays(4), DateTime.Today.AddDays(6))
            .Build();

        _context.Bookings.Add(existingBooking);
        _context.BookingItems.Add(existingBookingItem);
        await _context.SaveChangesAsync();

        // Act - Check availability with partial overlap (5-7 overlaps with 4-6)
        var result = await _roomAvailabilityService.IsRoomAvailableAsync(roomId, DateTime.Today.AddDays(5), DateTime.Today.AddDays(7));

        // Assert
        Assert.False(result); // Should not be available due to overlap
    }

    #endregion

    #region Helper Methods

    private async Task<(int hotelId, List<int> roomIds)> SeedHotelWithRoomsAndBookingsAsync()
    {
        // Create basic entities
        var city = _testDataBuilder.CreateCity().Build();
        var owner = _testDataBuilder.CreateUser().Build();
        var customer = _testDataBuilder.CreateUser(2).Build();

        var hotel = _testDataBuilder.CreateHotel()
            .WithCity(city.Id)
            .WithOwner(owner.Id)
            .Build();

        _context.Cities.Add(city);
        _context.Users.AddRange(owner, customer);
        _context.Hotels.Add(hotel);

        // Create rooms with different quantities
        var room1 = _testDataBuilder.CreateRoom(1)
            .WithHotel(hotel.Id)
            .WithType(RoomType.Standard)
            .WithQuantity(5)
            .WithPrice(100m)
            .Build();

        var room2 = _testDataBuilder.CreateRoom(2)
            .WithHotel(hotel.Id)
            .WithType(RoomType.Deluxe)
            .WithQuantity(3)
            .WithPrice(150m)
            .Build();

        var room3 = _testDataBuilder.CreateRoom(3)
            .WithHotel(hotel.Id)
            .WithType(RoomType.Suite)
            .WithQuantity(4)
            .WithPrice(200m)
            .Build();

        _context.Rooms.AddRange(room1, room2, room3);

        // Create bookings with overlapping dates (Days 5-7)
        var booking1 = _testDataBuilder.CreateBooking(1).WithUser(customer.Id).Build();
        var booking2 = _testDataBuilder.CreateBooking(2).WithUser(customer.Id).Build();
        var booking3 = _testDataBuilder.CreateBooking(3).WithUser(customer.Id).Build();

        _context.Bookings.AddRange(booking1, booking2, booking3);

        // Create booking items that overlap with our test period (Days 5-7)
        var bookingItem1 = _testDataBuilder.CreateBookingItem(1)
            .WithBooking(booking1.Id)
            .WithRoom(room1.Id)
            .WithDates(DateTime.Today.AddDays(4), DateTime.Today.AddDays(8)) // Overlaps
            .Build();

        var bookingItem2 = _testDataBuilder.CreateBookingItem(2)
            .WithBooking(booking1.Id)
            .WithRoom(room1.Id)
            .WithDates(DateTime.Today.AddDays(6), DateTime.Today.AddDays(9)) // Overlaps
            .Build();

        var bookingItem3 = _testDataBuilder.CreateBookingItem(3)
            .WithBooking(booking2.Id)
            .WithRoom(room2.Id)
            .WithDates(DateTime.Today.AddDays(5), DateTime.Today.AddDays(7)) // Overlaps
            .Build();

        // Room 3 has no overlapping bookings in our test period
        
        var bookingItem4 = _testDataBuilder.CreateBookingItem(4)
            .WithBooking(booking3.Id)
            .WithRoom(room3.Id)
            .WithDates(DateTime.Today.AddDays(5), DateTime.Today.AddDays(7)) // No overlap
            .Build();

        _context.BookingItems.AddRange(bookingItem1, bookingItem2, bookingItem3);
        await _context.SaveChangesAsync();

        return (hotel.Id, new List<int> { room1.Id, room2.Id, room3.Id });
    }

    private async Task<(int hotelId, List<int> roomIds)> SeedHotelWithPartialBookingsAsync()
    {
        // Create basic entities
        var city = _testDataBuilder.CreateCity().Build();
        var owner = _testDataBuilder.CreateUser().Build();
        var customer = _testDataBuilder.CreateUser(2).Build();

        var hotel = _testDataBuilder.CreateHotel()
            .WithCity(city.Id)
            .WithOwner(owner.Id)
            .Build();

        _context.Cities.Add(city);
        _context.Users.AddRange(owner, customer);
        _context.Hotels.Add(hotel);

        // Create rooms with higher quantities
        var room1 = _testDataBuilder.CreateRoom(1)
            .WithHotel(hotel.Id)
            .WithQuantity(10)
            .Build();

        var room2 = _testDataBuilder.CreateRoom(2)
            .WithHotel(hotel.Id)
            .WithQuantity(8)
            .Build();

        _context.Rooms.AddRange(room1, room2);

        // Create bookings
        var booking1 = _testDataBuilder.CreateBooking(1).WithUser(customer.Id).Build();
        var booking2 = _testDataBuilder.CreateBooking(2).WithUser(customer.Id).Build();
        var booking3 = _testDataBuilder.CreateBooking(3).WithUser(customer.Id).Build();
        var booking4 = _testDataBuilder.CreateBooking(4).WithUser(customer.Id).Build();

        _context.Bookings.AddRange(booking1, booking2, booking3, booking4);

        // Create booking items for the test period (Days 10-12)
        var bookingItem1 = _testDataBuilder.CreateBookingItem(1)
            .WithBooking(booking1.Id)
            .WithRoom(room1.Id)
            .WithDates(DateTime.Today.AddDays(9), DateTime.Today.AddDays(13)) // 3 bookings for room1
            .Build();

        var bookingItem2 = _testDataBuilder.CreateBookingItem(2)
            .WithBooking(booking2.Id)
            .WithRoom(room1.Id)
            .WithDates(DateTime.Today.AddDays(10), DateTime.Today.AddDays(12))
            .Build();

        var bookingItem3 = _testDataBuilder.CreateBookingItem(3)
            .WithBooking(booking3.Id)
            .WithRoom(room1.Id)
            .WithDates(DateTime.Today.AddDays(11), DateTime.Today.AddDays(14))
            .Build();

        var bookingItem4 = _testDataBuilder.CreateBookingItem(4)
            .WithBooking(booking4.Id)
            .WithRoom(room2.Id)
            .WithDates(DateTime.Today.AddDays(10), DateTime.Today.AddDays(12)) // 1 booking for room2
            .Build();

        _context.BookingItems.AddRange(bookingItem1, bookingItem2, bookingItem3, bookingItem4);
        await _context.SaveChangesAsync();

        return (hotel.Id, new List<int> { room1.Id, room2.Id });
    }

    private async Task<Hotel> SeedHotelWithoutRoomsAsync()
    {
        var city = _testDataBuilder.CreateCity().Build();
        var owner = _testDataBuilder.CreateUser().Build();
        var hotel = _testDataBuilder.CreateHotel()
            .WithCity(city.Id)
            .WithOwner(owner.Id)
            .Build();

        _context.Cities.Add(city);
        _context.Users.Add(owner);
        _context.Hotels.Add(hotel);
        await _context.SaveChangesAsync();

        return hotel;
    }

    #endregion

    public void Dispose()
    {
        _context.Dispose();
    }
}
