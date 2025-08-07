using Final_Project.Data;
using Final_Project.Models;
using Final_Project.Services;
using Final_Project.Tests.ServicesTests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Final_Project.Tests.ServicesTests;

public class HomeServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly Mock<ILogger<HomeService>> _mockLogger;
    private readonly HomeService _homeService;
    private readonly TestDataBuilder _testDataBuilder;

    public HomeServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);
        _mockLogger = new Mock<ILogger<HomeService>>();
        _testDataBuilder = new TestDataBuilder(_context);
        _homeService = new HomeService(_context, _mockLogger.Object);
    }

    #region GetFeaturedDealsAsync Tests

    [Fact]
    public async Task GetFeaturedDealsAsync_ReturnsTop5ActiveHotelsOrderedByDiscountedPriceAndStarRating()
    {
        // Arrange
        await SeedHotelsWithRoomsForFeaturedDealsAsync();

        // Act
        var result = await _homeService.GetFeaturedDealsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.Count);
        
        // Verify ordering: first by discounted price (descending), then by star rating (descending)
        for (int i = 0; i < result.Count - 1; i++)
        {
            var current = result[i];
            var next = result[i + 1];
            
            // Current should have higher or equal discounted price
            Assert.True(current.DiscountedPrice >= next.DiscountedPrice);
            
            // If prices are equal, current should have higher or equal star rating
            if (current.DiscountedPrice == next.DiscountedPrice)
            {
                Assert.True(current.StarRating >= next.StarRating);
            }
        }

        // Verify all returned hotels are active
        var hotelIds = result.Select(h => h.Id).ToList();
        var hotels = await _context.Hotels.Where(h => hotelIds.Contains(h.Id)).ToListAsync();
        Assert.All(hotels, h => Assert.True(h.IsActive));
    }

    [Fact]
    public async Task GetFeaturedDealsAsync_ReturnsEmptyListWhenNoActiveHotelsExist()
    {
        // Arrange - Create only inactive hotels
        await SeedInactiveHotelsAsync();

        // Act
        var result = await _homeService.GetFeaturedDealsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
        
        // Verify log message
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No featured deals available")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetFeaturedDealsAsync_LogsExceptionAndRethrowsWhenErrorOccurs()
    {
        // Arrange - Dispose context to force an exception
        await _context.DisposeAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(
            () => _homeService.GetFeaturedDealsAsync());

        // Verify exception is logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error fetching featured deals")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region GetRecentlyViewedHotelsAsync Tests

    [Fact]
    public async Task GetRecentlyViewedHotelsAsync_ReturnsTop5MostRecentlyViewedHotelsForValidUser()
    {
        // Arrange
        const int userId = 123;
        var user = TestDataBuilder.CreateClaimsPrincipal(userId.ToString(), "User");
        
        await SeedRecentlyViewedHotelsAsync(userId);

        // Act
        var result = await _homeService.GetRecentlyViewedHotelsAsync(user);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.Count);
        
        // Verify ordering by most recent first
        var viewedTimes = await _context.RecentlyViewedHotels
            .Where(rv => rv.UserId == userId)
            .OrderByDescending(rv => rv.ViewedAt)
            .Take(5)
            .Select(rv => rv.ViewedAt)
            .ToListAsync();

        for (int i = 0; i < result.Count - 1; i++)
        {
            Assert.True(viewedTimes[i] >= viewedTimes[i + 1]);
        }

        // Verify log message
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Retrieved {result.Count} recently viewed hotels for user {userId}")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetRecentlyViewedHotelsAsync_ReturnsEmptyListWhenUserClaimIsMissing()
    {
        // Arrange
        var user = TestDataBuilder.CreateClaimsPrincipal(null!, "User");

        // Act
        var result = await _homeService.GetRecentlyViewedHotelsAsync(user);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
        
        // Verify warning log
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("User ID not found or invalid in claims")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetRecentlyViewedHotelsAsync_ReturnsEmptyListWhenUserClaimIsInvalid()
    {
        // Arrange
        var user = TestDataBuilder.CreateClaimsPrincipal("invalid-user-id", "User");

        // Act
        var result = await _homeService.GetRecentlyViewedHotelsAsync(user);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
        
        // Verify warning log
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("User ID not found or invalid in claims")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetRecentlyViewedHotelsAsync_ReturnsEmptyListAndLogsInfoWhenUserHasNoRecentlyViewedHotels()
    {
        // Arrange
        const int userId = 456;
        var user = TestDataBuilder.CreateClaimsPrincipal(userId.ToString(), "User");
        
        // Don't seed any recently viewed hotels for this user

        // Act
        var result = await _homeService.GetRecentlyViewedHotelsAsync(user);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
        
        // Verify info log
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"No recently viewed hotels found for user {userId}")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetRecentlyViewedHotelsAsync_LogsAndRethrowsExceptionOnError()
    {
        // Arrange
        var user = TestDataBuilder.CreateClaimsPrincipal("123", "User");
        await _context.DisposeAsync(); // Force an exception

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(
            () => _homeService.GetRecentlyViewedHotelsAsync(user));

        // Verify error log
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error fetching recently viewed hotels")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region GetTrendingDestinationsAsync Tests

    [Fact]
    public async Task GetTrendingDestinationsAsync_ReturnsTop5CitiesWithHighestBookingCounts()
    {
        // Arrange
        await SeedBookingsWithCitiesAsync();

        // Act
        var result = await _homeService.GetTrendingDestinationsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Count <= 5);
        
        // Verify ordering by booking count (descending)
        for (int i = 0; i < result.Count - 1; i++)
        {
            Assert.True(result[i].BookingCount >= result[i + 1].BookingCount);
        }

        // Verify log message
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Retrieved {result.Count} trending destinations")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetTrendingDestinationsAsync_ReturnsEmptyListAndLogsInfoIfThereAreNoBookings()
    {
        // Arrange - Don't seed any bookings

        // Act
        var result = await _homeService.GetTrendingDestinationsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
        
        // Verify info log
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No trending destinations available")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetTrendingDestinationsAsync_LogsAndRethrowsExceptionOnError()
    {
        // Arrange
        await _context.DisposeAsync(); // Force an exception

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(
            () => _homeService.GetTrendingDestinationsAsync());

        // Verify error log
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error fetching trending destinations")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region Helper Methods

    private async Task SeedHotelsWithRoomsForFeaturedDealsAsync()
    {
        var city = _testDataBuilder.CreateCity().WithName("Test City").Build();
        var owner = _testDataBuilder.CreateUser().WithName("Hotel Owner").Build();
        
        _context.Cities.Add(city);
        _context.Users.Add(owner);

        var hotels = new List<Hotel>();
        var rooms = new List<Room>();
        var images = new List<HotelImage>();

        // Create 7 hotels (5 active, 2 inactive) with different price ranges and star ratings
        for (int i = 1; i <= 7; i++)
        {
            var isActive = i <= 5; // First 5 are active
            var starRating = (i % 5) + 1;
            var hotel = _testDataBuilder.CreateHotel(i)
                .WithName($"Hotel {i}")
                .WithActiveStatus(isActive)
                .WithStarRating(starRating)
                .WithCity(1)
                .WithOwner(1)
                .Build();

            hotels.Add(hotel);

            // Add hotel image
            var image = _testDataBuilder.CreateHotelImage(i)
                .WithHotel(i)
                .WithUrl($"https://example.com/hotel{i}.jpg")
                .Build();
            images.Add(image);

            // Add rooms with different prices to create variety in discounted prices
            var basePrice = 100m + (i * 50m); // Prices: 150, 200, 250, 300, 350, 400, 450
            var discount = i % 3 == 0 ? 20m : 0m; // Every 3rd hotel has 20% discount

            var room = _testDataBuilder.CreateRoom(i)
                .WithHotel(i)
                .WithPrice(basePrice)
                .Build();

            // Set discount property if it exists (assuming Room has Discount property)
            room.Discount = discount;
            rooms.Add(room);
        }

        _context.Hotels.AddRange(hotels);
        _context.HotelImages.AddRange(images);
        _context.Rooms.AddRange(rooms);
        await _context.SaveChangesAsync();
    }

    private async Task SeedInactiveHotelsAsync()
    {
        var city = _testDataBuilder.CreateCity().Build();
        var owner = _testDataBuilder.CreateUser().Build();
        
        _context.Cities.Add(city);
        _context.Users.Add(owner);

        var hotels = new List<Hotel>();
        for (int i = 1; i <= 3; i++)
        {
            var hotel = _testDataBuilder.CreateHotel(i)
                .WithActiveStatus(false) // All inactive
                .WithCity(1)
                .WithOwner(1)
                .Build();
            hotels.Add(hotel);
        }

        _context.Hotels.AddRange(hotels);
        await _context.SaveChangesAsync();
    }

    private async Task SeedRecentlyViewedHotelsAsync(int userId)
    {
        // Create test data
        var city = _testDataBuilder.CreateCity().Build();
        var owner = _testDataBuilder.CreateUser().Build();
        var user = _testDataBuilder.CreateUser(userId).Build();
        
        _context.Cities.Add(city);
        _context.Users.AddRange(owner, user);

        var hotels = new List<Hotel>();
        var images = new List<HotelImage>();
        var rooms = new List<Room>();
        var recentlyViewed = new List<RecentlyViewedHotel>();

        var baseTime = DateTime.UtcNow.AddDays(-10);

        // Create 7 hotels and recently viewed records (only top 5 should be returned)
        for (int i = 1; i <= 7; i++)
        {
            var hotel = _testDataBuilder.CreateHotel(i)
                .WithName($"Hotel {i}")
                .WithCity(1)
                .WithOwner(1)
                .Build();
            hotels.Add(hotel);

            var image = _testDataBuilder.CreateHotelImage(i)
                .WithHotel(i)
                .Build();
            images.Add(image);

            var room = _testDataBuilder.CreateRoom(i)
                .WithHotel(i)
                .WithPrice(100m + (i * 10m))
                .Build();
            rooms.Add(room);

            var viewed = _testDataBuilder.CreateRecentlyViewedHotel(i)
                .WithUser(userId)
                .WithHotel(i)
                .WithViewedAt(baseTime.AddHours(i)) // More recent times for higher IDs
                .Build();
            recentlyViewed.Add(viewed);
        }

        _context.Hotels.AddRange(hotels);
        _context.HotelImages.AddRange(images);
        _context.Rooms.AddRange(rooms);
        _context.RecentlyViewedHotels.AddRange(recentlyViewed);
        await _context.SaveChangesAsync();
    }

    private async Task SeedBookingsWithCitiesAsync()
    {
        var cities = new List<City>();
        var users = new List<User>();
        var hotels = new List<Hotel>();
        var rooms = new List<Room>();
        var bookings = new List<Booking>();
        var bookingItems = new List<BookingItem>();

        // Create 3 cities with different booking counts
        for (int cityId = 1; cityId <= 3; cityId++)
        {
            var city = _testDataBuilder.CreateCity(cityId)
                .WithName($"City {cityId}")
                .Build();
            cities.Add(city);

            var owner = _testDataBuilder.CreateUser(cityId).Build();
            users.Add(owner);

            // Create hotel in this city
            var hotel = _testDataBuilder.CreateHotel(cityId)
                .WithCity(cityId)
                .WithOwner(cityId)
                .WithName($"Hotel in City {cityId}")
                .Build();
            hotels.Add(hotel);

            var room = _testDataBuilder.CreateRoom(cityId)
                .WithHotel(cityId)
                .Build();
            rooms.Add(room);

            // Create different numbers of bookings per city
            // City 1: 5 bookings, City 2: 3 bookings, City 3: 7 bookings
            var bookingCount = cityId == 1 ? 5 : cityId == 2 ? 3 : 7;
            
            for (int bookingId = 1; bookingId <= bookingCount; bookingId++)
            {
                var globalBookingId = (cityId - 1) * 10 + bookingId;
                
                var booking = _testDataBuilder.CreateBooking(globalBookingId)
                    .WithUser(cityId)
                    .Build();
                bookings.Add(booking);

                var bookingItem = _testDataBuilder.CreateBookingItem(globalBookingId)
                    .WithBooking(globalBookingId)
                    .WithRoom(cityId)
                    .Build();
                bookingItems.Add(bookingItem);
            }
        }

        _context.Cities.AddRange(cities);
        _context.Users.AddRange(users);
        _context.Hotels.AddRange(hotels);
        _context.Rooms.AddRange(rooms);
        _context.Bookings.AddRange(bookings);
        _context.BookingItems.AddRange(bookingItems);
        await _context.SaveChangesAsync();
    }

    #endregion

    public void Dispose()
    {
        _context.Dispose();
    }
}
