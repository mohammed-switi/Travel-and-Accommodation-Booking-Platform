using System.Diagnostics;
using Final_Project.Data;
using Final_Project.DTOs;
using Final_Project.DTOs.Requests;
using Final_Project.Enums;
using Final_Project.Interfaces;
using Final_Project.Services;
using Final_Project.Tests.ServicesTests.Helpers;
using Microsoft.EntityFrameworkCore;
using Moq;
using Serilog;
using Xunit;
using Xunit.Abstractions;

namespace Final_Project.Tests.ServicesTests;

public class HotelServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly Mock<IOwnershipValidationService> _mockOwnershipService;
    private readonly Mock<ILogger<HotelService>> _mockLogger;
    private readonly HotelService _hotelService;
    private readonly TestDataBuilder _testDataBuilder;

    public HotelServiceTests(ITestOutputHelper output)
    {
        Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.TestOutput(output)
                .WriteTo.Console()
                .CreateLogger();
        
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        
        var logger = new LoggerFactory()
            .AddSerilog(Log.Logger)
            .CreateLogger<HotelService>();
        _context = new AppDbContext(options);
        _testDataBuilder = new TestDataBuilder(_context);
        _mockOwnershipService = new Mock<IOwnershipValidationService>();
        _mockLogger = new Mock<ILogger<HotelService>>();
        _hotelService = new HotelService(_context, _mockOwnershipService.Object, _mockLogger.Object);
    }

    #region GetHotelsAsync Tests

    [Fact]
    public async Task GetHotelsAsync_WithPagination_ReturnsCorrectPageSize()
    {
        // Arrange
        var testDataBuilder = new TestDataBuilder(_context);
        await testDataBuilder.SeedMultipleHotelsAsync();

        // Act
        var result = await _hotelService.GetHotelsAsync(1, 10);

        // Assert
        Assert.Equal(10, result.Count);
    }

    [Fact]
    public async Task GetHotelsAsync_SecondPage_ReturnsCorrectPage()
    {
        // Arrange
        var testDataBuilder = new TestDataBuilder(_context);
        await testDataBuilder.SeedMultipleHotelsAsync();

        // Act
        var result = await _hotelService.GetHotelsAsync(2, 10);

        // Assert
        Assert.Equal(5, result.Count);
    }

    [Fact]
    public async Task GetHotelsAsync_ExcludeInactiveHotels_ReturnsOnlyActiveHotels()
    {
        // Arrange
        var testDataBuilder = new TestDataBuilder(_context);
        await testDataBuilder.SeedMultipleHotelsAsync(10, mixActiveInactive: true);

        // Act
        var result = await _hotelService.GetHotelsAsync(1, 20, includeInactive: false);

        // Assert
        Assert.All(result, hotel => Assert.True(hotel.Id % 2 == 0)); // Only even IDs (active hotels)
    }

    [Fact]
    public async Task GetHotelsAsync_IncludeInactiveHotels_ReturnsAllHotels()
    {
        // Arrange
        var testDataBuilder = new TestDataBuilder(_context);
        await testDataBuilder.SeedMultipleHotelsAsync(10, mixActiveInactive: true);

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
        await Assert.ThrowsAsync<ObjectDisposedException>(
            () => _hotelService.GetHotelsAsync(1, 10));

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("An error occurred while retrieving hotels")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region GetHotelByIdAsync Tests

    [Fact]
    public async Task GetHotelByIdAsync_WithValidId_ReturnsHotel()
    {
        // Arrange
        var testDataBuilder = new TestDataBuilder(_context);
        await testDataBuilder.SeedHotelWithRelatedDataAsync(1, "Luxury Hotel", 5);

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
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _hotelService.GetHotelByIdAsync(999));
    }

    [Fact]
    public async Task GetHotelByIdAsync_IncludesRelatedData_ReturnsCompleteHotelInfo()
    {
        // Arrange
        var testDataBuilder = new TestDataBuilder(_context);
        await testDataBuilder.SeedHotelWithRelatedDataAsync();

        // Act
        var result = await _hotelService.GetHotelByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.ImageUrl);
        Assert.NotEmpty(result.Reviews!);
        Debug.Assert(result.Reviews != null, "not null");
        Assert.Contains(result.Reviews, r => r.Comment == "Excellent service!");
    }

    [Fact]
    public async Task GetHotelByIdAsync_InactiveHotel_ThrowsInvalidOperationException()
    {
        // Arrange
        var testDataBuilder = new TestDataBuilder(_context);
        await testDataBuilder.SeedHotelWithRelatedDataAsync(1, "Inactive Hotel", 3, isActive: false);

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
        var testDataBuilder = new TestDataBuilder(_context);
        var city = testDataBuilder.CreateCity().WithName("New York").Build();
        var mainImage = testDataBuilder.CreateHotelImage().WithUrl("https://example.com/main.jpg").Build();
        _context.Cities.Add(city);
        _context.HotelImages.Add(mainImage);
        await _context.SaveChangesAsync();

        // Setup mock to allow Admin to create hotels
        _mockOwnershipService
            .Setup(x => x.CanUserCreateHotelAsync("Admin",1,1))
            .ReturnsAsync(true);

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
        Assert.True(hotelInDb.Amenities.HasFlag(Amenities.Wifi));
        Assert.True(hotelInDb.Amenities.HasFlag(Amenities.Pool));
    }

    [Fact]
    public async Task CreateHotelAsync_WithNonExistentCity_ThrowsArgumentException()
    {
        // Arrange
        // Setup mock to allow Admin to create hotels
        _mockOwnershipService
            .Setup(x => x.CanUserCreateHotelAsync("Admin",1,1))
            .ReturnsAsync(true);
            
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
        await Assert.ThrowsAsync<ArgumentException>(
            () => _hotelService.CreateHotelAsync(createDto, 1, "Admin"));
    }

    [Fact]
    public async Task CreateHotelAsync_SetsOwnerIdCorrectly()
    {
        // Arrange
        var testDataBuilder = new TestDataBuilder(_context);
        var city = testDataBuilder.CreateCity().WithName("New York").Build();
        var mainImage = testDataBuilder.CreateHotelImage().WithUrl("https://example.com/main.jpg").Build();
        _context.Cities.Add(city);
        _context.HotelImages.Add(mainImage);
        await _context.SaveChangesAsync();

        // Setup mock to allow HotelOwner to create hotels
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
       
          _mockOwnershipService
                   .Setup(x => x.CanUserCreateHotelAsync("HotelOwner",createDto.OwnerId,createDto.OwnerId))
                   .ReturnsAsync(true);
             
        // Act
        var result = await _hotelService.CreateHotelAsync(createDto, 123, "HotelOwner");

        // Assert
        var hotelInDb = await _context.Hotels.FindAsync(result.Id);
        Assert.NotNull(hotelInDb);
        Assert.Equal(123, hotelInDb.OwnerId);
    }

    [Fact]
    public async Task CreateHotelAsync_SetsCreatedTimestamp()
    {
        // Arrange
        var testDataBuilder = new TestDataBuilder(_context);
        var city = testDataBuilder.CreateCity().WithName("New York").Build();
        var mainImage = testDataBuilder.CreateHotelImage().WithUrl("https://example.com/main.jpg").Build();
        _context.Cities.Add(city);
        _context.HotelImages.Add(mainImage);
        await _context.SaveChangesAsync();

        // Setup mock to allow Admin to create hotels
        _mockOwnershipService
            .Setup(x => x.CanUserCreateHotelAsync("Admin",1,1))
            .ReturnsAsync(true);
       
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
        
        // Check the DTO response first
        Assert.True(result.CreatedAt >= beforeCreate && result.CreatedAt <= afterCreate);
        
        // Verify the actual database entity has the timestamp set correctly
        var hotelInDb = await _context.Hotels.FindAsync(result.Id);
        Assert.NotNull(hotelInDb);
        Assert.True(hotelInDb.CreatedAt >= beforeCreate && hotelInDb.CreatedAt <= afterCreate);
    }

    [Fact]
    public async Task CreateHotelAsync_WithoutPermission_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var testDataBuilder = new TestDataBuilder(_context);
        var city = testDataBuilder.CreateCity().WithName("New York").Build();
        _context.Cities.Add(city);
        await _context.SaveChangesAsync();
            
        _mockOwnershipService
            .Setup(x => x.CanUserCreateHotelAsync("User", 1, 1))
            .ReturnsAsync(false);
            
        var createDto = new CreateHotelRequestDto
        {
            Name = "New Hotel",
            City = "New York",
            Description = "Test",
            StarRating = 3,
            Location = "Test Location",
            OwnerId = 1
        };

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _hotelService.CreateHotelAsync(createDto, 1, "User"));
    }

    #endregion

    #region UpdateHotelAsync Tests

    [Fact]
    public async Task UpdateHotelAsync_WithValidData_UpdatesHotel()
    {
        // Arrange
        var testDataBuilder = new TestDataBuilder(_context);
        await testDataBuilder.SeedHotelWithRelatedDataAsync();
        var updateDto = new UpdateHotelRequestDto
        {
            Name = "Updated Hotel Name",
            Description = "Updated description",
            StarRating = 5,
            Location = "Updated Location",
            City = "Test City",
            Amenities = Amenities.Wifi | Amenities.Pool | Amenities.Gym
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
        Assert.NotNull(hotelInDb);
        Assert.Equal(5, hotelInDb.StarRating);
        Assert.True(hotelInDb.Amenities.HasFlag(Amenities.Wifi));
        Assert.True(hotelInDb.Amenities.HasFlag(Amenities.Pool));
        Assert.True(hotelInDb.Amenities.HasFlag(Amenities.Gym));
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
        var testDataBuilder = new TestDataBuilder(_context);
        await testDataBuilder.SeedHotelWithRelatedDataAsync();
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
        var testDataBuilder = new TestDataBuilder(_context);
        await testDataBuilder.SeedHotelWithRelatedDataAsync();
        var updateDto = new UpdateHotelRequestDto
        {
            Name = "Updated Hotel",
            City = "NonExistentCity"
        };

        _mockOwnershipService
            .Setup(x => x.CanUserManageHotelAsync(1, "Admin", 1))
            .ReturnsAsync(true);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _hotelService.UpdateHotelAsync(1, updateDto, 1, "Admin"));
    }

    [Fact]
    public async Task UpdateHotelAsync_UpdatesTimestamp()
    {
        // Arrange
        var testDataBuilder = new TestDataBuilder(_context);
        var collection = await testDataBuilder.SeedHotelWithRelatedDataAsync();
        var hotel = collection.Item1;
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
        var testDataBuilder = new TestDataBuilder(_context);
        await testDataBuilder.SeedHotelWithRelatedDataAsync();

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
        var testDataBuilder = new TestDataBuilder(_context);
        await testDataBuilder.SeedHotelWithRelatedDataAsync();

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
        var testDataBuilder = new TestDataBuilder(_context);
        var (hotel, _, _) = await testDataBuilder.SeedHotelWithBookingConstraintsAsync();

        _mockOwnershipService
            .Setup(x => x.CanUserManageHotelAsync(hotel.Id, "Admin", 1))
            .ReturnsAsync(true);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _hotelService.DeleteHotelAsync(hotel.Id, 1, "Admin"));
    }

    [Fact]
    public async Task DeleteHotelAsync_WithCancelledBookingsOnly_DeletesSuccessfully()
    {
        // Arrange
        var testDataBuilder = new TestDataBuilder(_context);
        var (hotel, _, _) = await testDataBuilder.SeedHotelWithBookingConstraintsAsync(BookingStatus.Cancelled);

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

        Assert.Contains("Check-out date must be after check-in dat", exception.Message);
    }

    [Fact]
    public async Task SearchHotelsAsync_FiltersByLocation_ReturnsMatchingHotels()
    {
        // Arrange
        var testDataBuilder = new TestDataBuilder(_context);
        var city1 = testDataBuilder.CreateCity(1).WithName("New York").Build();
        var city2 = testDataBuilder.CreateCity(2).WithName("Los Angeles").Build();
        _context.Cities.AddRange(city1, city2);

        var hotel1 = testDataBuilder.CreateHotel(1).WithName("NY Hotel").WithCity(1).Build();
        var hotel2 = testDataBuilder.CreateHotel(2).WithName("LA Hotel").WithCity(2).Build();
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
        var testDataBuilder = new TestDataBuilder(_context);
        await testDataBuilder.SeedHotelWithRelatedDataAsync();

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

   
    #region Additional Edge Case Tests

    [Fact]
    public async Task GetHotelsAsync_WithInvalidPaginationParameters_HandlesGracefully()
    {
        // Arrange
        await _testDataBuilder.SeedHotelWithRelatedDataAsync();

        // Act & Assert 
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            await _hotelService.GetHotelsAsync(-1, 10));

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            await _hotelService.GetHotelsAsync(1, 0));

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

        // Setup mock to allow Admin to create hotels
        _mockOwnershipService
            .Setup(x => x.CanUserCreateHotelAsync("Admin",1,1))
            .ReturnsAsync(true);

        var createDto = new CreateHotelRequestDto
        {
            Name = "No Image Hotel",
            City = "Test City",
            Description = "Hotel without main image",
            StarRating = 3,
            Location = "Test Location",
            ImageUrl = null, // No main image
            OwnerId = 1,
            
        };

        // Act
        var result = await _hotelService.CreateHotelAsync(createDto, 1, "Admin");

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ImageUrl);
        
        var hotelInDb = await _context.Hotels.FindAsync(result.Id);
        Debug.Assert(hotelInDb != null, nameof(hotelInDb) + " != null");
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
        
        // Setup mock to allow Admin to create hotels
        _mockOwnershipService
            .Setup(x => x.CanUserCreateHotelAsync("Admin",1,1))
            .ReturnsAsync(true);

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
