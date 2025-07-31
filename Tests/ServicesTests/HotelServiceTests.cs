using Final_Project.Data;
using Final_Project.DTOs;
using Final_Project.DTOs.Requests;
using Final_Project.DTOs.Responses;
using Final_Project.Enums;
using Final_Project.Interfaces;
using Final_Project.Services;
using Final_Project.Tests.ServicesTests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Final_Project.Tests.ServicesTests;

public class HotelServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly Mock<IOwnershipValidationService> _mockOwnershipService;
    private readonly Mock<ILogger<HotelService>> _mockLogger;
    private readonly HotelService _hotelService;
    private readonly TestDataBuilder _testDataBuilder;

    public HotelServiceTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _mockOwnershipService = new Mock<IOwnershipValidationService>();
        _mockLogger = new Mock<ILogger<HotelService>>();
        _hotelService = new HotelService(_context, _mockOwnershipService.Object, _mockLogger.Object);
        _testDataBuilder = new TestDataBuilder(_context);
    }

    #region GetHotelsAsync Tests

    [Fact]
    public async Task GetHotelsAsync_WithPagination_ReturnsCorrectPageSize()
    {
        // Arrange
        await _testDataBuilder.SeedMultipleHotelsAsync(15);

        // Act
        var result = await _hotelService.GetHotelsAsync(1, 10);

        // Assert
        Assert.Equal(10, result.Count);
    }

    [Fact]
    public async Task GetHotelsAsync_SecondPage_ReturnsCorrectPage()
    {
        // Arrange
        await _testDataBuilder.SeedMultipleHotelsAsync(15);

        // Act
        var result = await _hotelService.GetHotelsAsync(2, 10);

        // Assert
        Assert.Equal(5, result.Count);
    }

    [Fact]
    public async Task GetHotelsAsync_ExcludeInactiveHotels_ReturnsOnlyActiveHotels()
    {
        // Arrange
        await _testDataBuilder.SeedMultipleHotelsAsync(10, mixActiveInactive: true);

        // Act
        var result = await _hotelService.GetHotelsAsync(1, 20, includeInactive: false);

        // Assert
        Assert.All(result, hotel => Assert.True(hotel.Id % 2 == 0)); // Only even IDs (active hotels)
    }

    [Fact]
    public async Task GetHotelsAsync_IncludeInactiveHotels_ReturnsAllHotels()
    {
        // Arrange
        await _testDataBuilder.SeedMultipleHotelsAsync(10, mixActiveInactive: true);

        // Act
        var result = await _hotelService.GetHotelsAsync(1, 20, includeInactive: true);

        // Assert
        Assert.Equal(10, result.Count);
    }

    [Fact]
    public async Task GetHotelsAsync_EmptyDatabase_ReturnsEmptyList()
    {
        // Act
        var result = await _hotelService.GetHotelsAsync(1, 10);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetHotelsAsync_DatabaseException_LogsAndRethrowsException()
    {
        // Arrange - Dispose context to simulate database error
        await _context.DisposeAsync();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ObjectDisposedException>(
            () => _hotelService.GetHotelsAsync(1, 10));

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("An error occurred while retrieving hotels")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    #endregion

    #region GetHotelByIdAsync Tests

    [Fact]
    public async Task GetHotelByIdAsync_WithValidId_ReturnsHotel()
    {
        // Arrange
        var hotel = await _testDataBuilder.SeedHotelWithRelatedDataAsync(1, "Luxury Hotel", 5);

        // Act
        var result = await _hotelService.GetHotelByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Luxury Hotel", result.Name);
        Assert.Equal(5, result.StarRating);
        Assert.Equal("Test City", result.City);
    }

    [Fact]
    public async Task GetHotelByIdAsync_WithNonExistentId_ThrowsInvalidOperationException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _hotelService.GetHotelByIdAsync(999));

        Assert.Equal("Hotel not found.", exception.Message);
    }

    [Fact]
    public async Task GetHotelByIdAsync_IncludesRelatedData_ReturnsCompleteHotelInfo()
    {
        // Arrange
        var hotel = await _testDataBuilder.SeedHotelWithRelatedDataAsync();

        // Act
        var result = await _hotelService.GetHotelByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.ImageUrl);
        Assert.NotEmpty(result.Reviews);
        Assert.Contains(result.Reviews, r => r.Comment == "Excellent service!");
    }

    [Fact]
    public async Task GetHotelByIdAsync_InactiveHotel_ThrowsInvalidOperationException()
    {
        // Arrange
        await _testDataBuilder.SeedHotelWithRelatedDataAsync(1, "Inactive Hotel", 3, isActive: false);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _hotelService.GetHotelByIdAsync(1));
    }

    #endregion

    #region CreateHotelAsync Tests

    [Fact]
    public async Task CreateHotelAsync_WithValidData_CreatesHotel()
    {
        // Arrange
        var city = _testDataBuilder.CreateCity().WithName("New York").Build();
        var mainImage = _testDataBuilder.CreateHotelImage().WithUrl("https://example.com/main.jpg").Build();
        _context.Cities.Add(city);
        _context.HotelImages.Add(mainImage);
        await _context.SaveChangesAsync();

        var createDto = new CreateHotelRequestDto
        {
            Name = "New Hotel",
            Description = "A brand new hotel",
            StarRating = 4,
            Location = "Downtown",
            City = "New York",
            ImageUrl = "https://example.com/main.jpg",
            Amenities = Amenities.Bar | Amenities.Wifi | Amenities.Pool,
            OwnerId = 1
        };

        // Act
        var result = await _hotelService.CreateHotelAsync(createDto, 1, "Admin");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("New Hotel", result.Name);
        Assert.Equal("A brand new hotel", result.Description);
        Assert.Equal(4, result.StarRating);
        Assert.Equal("New York", result.City);
        Assert.Equal("https://example.com/main.jpg", result.ImageUrl);

        var hotelInDb = await _context.Hotels.FindAsync(result.Id);
        Assert.NotNull(hotelInDb);
        Assert.Equal(4, hotelInDb.StarRating);
        Assert.True(hotelInDb.Amenities.HasFlag(Enums.Amenities.Wifi));
        Assert.True(hotelInDb.Amenities.HasFlag(Enums.Amenities.Pool));
    }

    [Fact]
    public async Task CreateHotelAsync_WithNonExistentCity_ThrowsArgumentException()
    {
        // Arrange
        var createDto = new CreateHotelRequestDto
        {
            Name = "New Hotel",
            City = "NonExistentCity",
            Description = "Test",
            StarRating = 3,
            Location = "Test Location",
            OwnerId = 1
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _hotelService.CreateHotelAsync(createDto, 1, "Admin"));

        Assert.Contains("City 'NonExistentCity' not found", exception.Message);
    }

    [Fact]
    public async Task CreateHotelAsync_WithInvalidImageUrl_ThrowsArgumentException()
    {
        // Arrange
        var city = _testDataBuilder.CreateCity().Build();
        _context.Cities.Add(city);
        await _context.SaveChangesAsync();

        var createDto = new CreateHotelRequestDto
        {
            Name = "New Hotel",
            City = "Test City",
            ImageUrl = "https://invalid-url.jpg",
            Description = "Test",
            StarRating = 3,
            Location = "Test Location",
            OwnerId = 1
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _hotelService.CreateHotelAsync(createDto, 1, "Admin"));

        Assert.Contains("Main image URL 'https://invalid-url.jpg' not found", exception.Message);
    }

    [Fact]
    public async Task CreateHotelAsync_SetsOwnerIdCorrectly()
    {
        // Arrange
        var city = _testDataBuilder.CreateCity().WithName("New York").Build();
              var mainImage = _testDataBuilder.CreateHotelImage().WithUrl("https://example.com/main.jpg").Build();
              _context.Cities.Add(city);
              _context.HotelImages.Add(mainImage);
              await _context.SaveChangesAsync();
      
              var createDto = new CreateHotelRequestDto
              {
                  Name = "New Hotel",
                  Description = "A brand new hotel",
                  StarRating = 4,
                  Location = "Downtown",
                  City = "New York",
                  ImageUrl = "https://example.com/main.jpg",
                  Amenities = Amenities.Bar | Amenities.Wifi | Amenities.Pool,
                  OwnerId = 123
              };
        // Act
        var result = await _hotelService.CreateHotelAsync(createDto, 123, "HotelOwner");

        // Assert
        var hotelInDb = await _context.Hotels.FindAsync(result.Id);
        Assert.Equal(123, hotelInDb.OwnerId);
    }

    [Fact]
    public async Task CreateHotelAsync_SetsCreatedTimestamp()
    {
        // Arrange
         var city = _testDataBuilder.CreateCity().WithName("New York").Build();
               var mainImage = _testDataBuilder.CreateHotelImage().WithUrl("https://example.com/main.jpg").Build();
               _context.Cities.Add(city);
               _context.HotelImages.Add(mainImage);
               await _context.SaveChangesAsync();
       
               var createDto = new CreateHotelRequestDto
               {
                   Name = "New Hotel",
                   Description = "A brand new hotel",
                   StarRating = 4,
                   Location = "Downtown",
                   City = "New York",
                   ImageUrl = "https://example.com/main.jpg",
                   Amenities = Amenities.Bar | Amenities.Wifi | Amenities.Pool,
                   OwnerId = 1
               };

        var beforeCreate = DateTime.UtcNow;

        // Act
        var result = await _hotelService.CreateHotelAsync(createDto, 1, "Admin");

        // Assert
        var afterCreate = DateTime.UtcNow;
        Assert.True(result.CreatedAt >= beforeCreate && result.CreatedAt <= afterCreate);
    }

    #endregion

    #region UpdateHotelAsync Tests

    [Fact]
    public async Task UpdateHotelAsync_WithValidData_UpdatesHotel()
    {
        // Arrange
        var hotel = await _testDataBuilder.SeedHotelWithRelatedDataAsync();
        var updateDto = new UpdateHotelRequestDto
        {
            Name = "Updated Hotel Name",
            Description = "Updated description",
            StarRating = 5,
            Location = "Updated Location",
            City = "Test City",
            Amenities =  Enums.Amenities.Wifi | Enums.Amenities.Pool | Enums.Amenities.Gym
        };

        _mockOwnershipService
            .Setup(x => x.CanUserManageHotelAsync(1, "Admin", 1))
            .ReturnsAsync(true);

        // Act
        var result = await _hotelService.UpdateHotelAsync(1, updateDto, 1, "Admin");

        // Assert
        Assert.Equal("Updated Hotel Name", result.Name);
        Assert.Equal("Updated description", result.Description);
        Assert.Equal(5, result.StarRating);
        Assert.Equal("Updated Location", result.Location);

        var hotelInDb = await _context.Hotels.FindAsync(1);
        Assert.Equal(5, hotelInDb.StarRating);
        Assert.True(hotelInDb.Amenities.HasFlag(Enums.Amenities.Wifi));
        Assert.True(hotelInDb.Amenities.HasFlag(Enums.Amenities.Pool));
        Assert.True(hotelInDb.Amenities.HasFlag(Enums.Amenities.Gym));
    }

    [Fact]
    public async Task UpdateHotelAsync_WithNonExistentHotel_ThrowsArgumentException()
    {
        // Arrange
        var updateDto = new UpdateHotelRequestDto
        {
            Name = "Updated Hotel",
            City = "Test City"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _hotelService.UpdateHotelAsync(999, updateDto, 1, "Admin"));
    }

    [Fact]
    public async Task UpdateHotelAsync_WithoutPermission_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var hotel = await _testDataBuilder.SeedHotelWithRelatedDataAsync();
        var updateDto = new UpdateHotelRequestDto
        {
            Name = "Updated Hotel",
            City = "Test City"
        };

        _mockOwnershipService
            .Setup(x => x.CanUserManageHotelAsync(1, "User", 1))
            .ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _hotelService.UpdateHotelAsync(1, updateDto, 1, "User"));
    }

    [Fact]
    public async Task UpdateHotelAsync_WithNonExistentCity_ThrowsArgumentException()
    {
        // Arrange
        var hotel = await _testDataBuilder.SeedHotelWithRelatedDataAsync();
        var updateDto = new UpdateHotelRequestDto
        {
            Name = "Updated Hotel",
            City = "NonExistentCity"
        };

        _mockOwnershipService
            .Setup(x => x.CanUserManageHotelAsync(1, "Admin", 1))
            .ReturnsAsync(true);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _hotelService.UpdateHotelAsync(1, updateDto, 1, "Admin"));

        Assert.Contains("City 'NonExistentCity' not found", exception.Message);
    }

    [Fact]
    public async Task UpdateHotelAsync_UpdatesTimestamp()
    {
        // Arrange
        var hotel = await _testDataBuilder.SeedHotelWithRelatedDataAsync();
        var originalUpdateTime = hotel.UpdatedAt;
        
        var updateDto = new UpdateHotelRequestDto
        {
            Name = "Updated Hotel",
            City = "Test City"
        };

        _mockOwnershipService
            .Setup(x => x.CanUserManageHotelAsync(1, "Admin", 1))
            .ReturnsAsync(true);

        // Wait a moment to ensure timestamp difference
        await Task.Delay(10);

        // Act
        var result = await _hotelService.UpdateHotelAsync(1, updateDto, 1, "Admin");

        // Assert
        Assert.True(result.UpdatedAt > originalUpdateTime);
    }

    #endregion

    #region DeleteHotelAsync Tests

    [Fact]
    public async Task DeleteHotelAsync_WithValidId_DeletesHotel()
    {
        // Arrange
        var hotel = await _testDataBuilder.SeedHotelWithRelatedDataAsync();

        _mockOwnershipService
            .Setup(x => x.CanUserManageHotelAsync(1, "Admin", 1))
            .ReturnsAsync(true);

        // Act
        var result = await _hotelService.DeleteHotelAsync(1, 1, "Admin");

        // Assert
        Assert.True(result);
        var hotelInDb = await _context.Hotels.FindAsync(1);
        Assert.Null(hotelInDb);
    }

    [Fact]
    public async Task DeleteHotelAsync_WithNonExistentId_ReturnsFalse()
    {
        // Act
        var result = await _hotelService.DeleteHotelAsync(999, 1, "Admin");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteHotelAsync_WithoutPermission_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var hotel = await _testDataBuilder.SeedHotelWithRelatedDataAsync();

        _mockOwnershipService
            .Setup(x => x.CanUserManageHotelAsync(1, "User", 1))
            .ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _hotelService.DeleteHotelAsync(1, 1, "User"));
    }

    [Fact]
    public async Task DeleteHotelAsync_WithActiveBookings_ThrowsInvalidOperationException()
    {
        // Arrange
        var (hotel, booking, room) = await _testDataBuilder.SeedHotelWithBookingConstraintsAsync(BookingStatus.Approved);

        _mockOwnershipService
            .Setup(x => x.CanUserManageHotelAsync(hotel.Id, "Admin", 1))
            .ReturnsAsync(true);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _hotelService.DeleteHotelAsync(hotel.Id, 1, "Admin"));

        Assert.Contains("Cannot delete hotel with active bookings", exception.Message);
    }

    [Fact]
    public async Task DeleteHotelAsync_WithCancelledBookingsOnly_DeletesSuccessfully()
    {
        // Arrange
        var (hotel, booking, room) = await _testDataBuilder.SeedHotelWithBookingConstraintsAsync(BookingStatus.Cancelled);

        _mockOwnershipService
            .Setup(x => x.CanUserManageHotelAsync(hotel.Id, "Admin", 1))
            .ReturnsAsync(true);

        // Act
        var result = await _hotelService.DeleteHotelAsync(hotel.Id, 1, "Admin");

        // Assert
        Assert.True(result);
        var hotelInDb = await _context.Hotels.FindAsync(hotel.Id);
        Assert.Null(hotelInDb);
    }

    #endregion

    #region SearchHotelsAsync Tests

    [Fact]
    public async Task SearchHotelsAsync_WithInvalidDates_ThrowsArgumentException()
    {
        // Arrange
        var searchDto = new SearchHotelsDto
        {
            CheckInDate = DateTime.Today.AddDays(2),
            CheckOutDate = DateTime.Today.AddDays(1), // Check-out before check-in
            Location = "Test City"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _hotelService.SearchHotelsAsync(searchDto));

        Assert.Contains("Check-in date must be before check-out date", exception.Message);
    }

    [Fact]
    public async Task SearchHotelsAsync_FiltersByLocation_ReturnsMatchingHotels()
    {
        // Arrange
        var city1 = _testDataBuilder.CreateCity(1).WithName("New York").Build();
        var city2 = _testDataBuilder.CreateCity(2).WithName("Los Angeles").Build();
        _context.Cities.AddRange(city1, city2);

        var hotel1 = _testDataBuilder.CreateHotel(1).WithName("NY Hotel").WithCity(1).Build();
        var hotel2 = _testDataBuilder.CreateHotel(2).WithName("LA Hotel").WithCity(2).Build();
        _context.Hotels.AddRange(hotel1, hotel2);
        await _context.SaveChangesAsync();

        var searchDto = new SearchHotelsDto
        {
            CheckInDate = DateTime.Today.AddDays(1),
            CheckOutDate = DateTime.Today.AddDays(3),
            Location = "New York"
        };

        // Act
        var result = await _hotelService.SearchHotelsAsync(searchDto);

        // Assert
        Assert.Single(result);
        Assert.Equal("NY Hotel", result[0].Name);
    }

    [Fact]
    public async Task SearchHotelsAsync_WithNoMatchingCriteria_ReturnsEmptyList()
    {
        // Arrange
        await _testDataBuilder.SeedHotelWithRelatedDataAsync();

        var searchDto = new SearchHotelsDto
        {
            CheckInDate = DateTime.Today.AddDays(1),
            CheckOutDate = DateTime.Today.AddDays(3),
            Location = "NonExistentCity"
        };

        // Act
        var result = await _hotelService.SearchHotelsAsync(searchDto);

        // Assert
        Assert.Empty(result);
    }

    #endregion

    #region GetHotelDetailsAsync Tests

    [Fact]
    public async Task GetHotelDetailsAsync_WithValidId_ReturnsHotelDetails()
    {
        // Arrange
        var hotel = await _testDataBuilder.SeedHotelWithRelatedDataAsync();
        await _testDataBuilder.SeedRoomWithHotelAsync(1, hotel.Id, RoomType.Deluxe, 200m);

        // Act
        var result = await _hotelService.GetHotelDetailsAsync(1, DateTime.Today.AddDays(1), DateTime.Today.AddDays(3));

        // Assert
        Assert.NotNull(result);
        Assert.Equal(hotel.Name, result.Name);
        Assert.NotEmpty(result.Reviews);
    }

    [Fact]
    public async Task GetHotelDetailsAsync_WithNonExistentId_ReturnsNull()
    {
        // Act
        var result = await _hotelService.GetHotelDetailsAsync(999, DateTime.Today.AddDays(1), DateTime.Today.AddDays(3));

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetHotelDetailsAsync_InactiveHotel_ReturnsNull()
    {
        // Arrange
        await _testDataBuilder.SeedHotelWithRelatedDataAsync(1, "Inactive Hotel", 3, isActive: false);

        // Act
        var result = await _hotelService.GetHotelDetailsAsync(1, DateTime.Today.AddDays(1), DateTime.Today.AddDays(3));

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region Additional Edge Case Tests

    [Fact]
    public async Task GetHotelsAsync_WithInvalidPaginationParameters_HandlesGracefully()
    {
        // Arrange
        await _testDataBuilder.SeedHotelWithRelatedDataAsync();

        // Act & Assert - Test negative page numbers
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            await _hotelService.GetHotelsAsync(-1, 10));

        // Test zero page size
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            await _hotelService.GetHotelsAsync(1, 0));

        // Test large page size
        var result = await _hotelService.GetHotelsAsync(1, 1000);
        Assert.Single(result);
    }

    [Fact]
    public async Task CreateHotelAsync_WithNullImageUrl_CreatesHotelWithoutMainImage()
    {
        // Arrange
        var city = _testDataBuilder.CreateCity().Build();
        _context.Cities.Add(city);
        await _context.SaveChangesAsync();

        var createDto = new CreateHotelRequestDto
        {
            Name = "No Image Hotel",
            City = "Test City",
            Description = "Hotel without main image",
            StarRating = 3,
            Location = "Test Location",
            ImageUrl = null, // No main image
            OwnerId = 1
        };

        // Act
        var result = await _hotelService.CreateHotelAsync(createDto, 1, "Admin");

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ImageUrl);
        
        var hotelInDb = await _context.Hotels.FindAsync(result.Id);
        Assert.Null(hotelInDb.MainImageId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateHotelAsync_WithEmptyImageUrl_CreatesHotelWithoutMainImage(string imageUrl)
    {
        // Arrange
        var city = _testDataBuilder.CreateCity().Build();
        _context.Cities.Add(city);
        await _context.SaveChangesAsync();

        var createDto = new CreateHotelRequestDto
        {
            Name = "Empty Image Hotel",
            City = "Test City",
            Description = "Hotel with empty image URL",
            StarRating = 3,
            Location = "Test Location",
            ImageUrl = imageUrl,
            OwnerId = 1
        };

        // Act
        var result = await _hotelService.CreateHotelAsync(createDto, 1, "Admin");

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ImageUrl);
    }

    #endregion

    public void Dispose()
    {
        _context.Dispose();
    }
}
