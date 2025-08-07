using Final_Project.Data;
using Final_Project.DTOs.Requests;
using Final_Project.Models;
using Final_Project.Services;
using Final_Project.Tests.ServicesTests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Final_Project.Tests.ServicesTests;

public class CityServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly Mock<ILogger<CityService>> _mockLogger;
    private readonly CityService _cityService;
    private readonly TestDataBuilder _testDataBuilder;

    public CityServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);
        _mockLogger = new Mock<ILogger<CityService>>();
        _testDataBuilder = new TestDataBuilder(_context);
        _cityService = new CityService(_context, _mockLogger.Object);
    }

    #region GetCitiesAsync Tests

    [Fact]
    public async Task GetCitiesAsync_ReturnsPaginatedListOfCitiesWhenCitiesExist()
    {
        // Arrange
        await SeedMultipleCitiesAsync();

        // Act
        var result = await _cityService.GetCitiesAsync(page: 1, pageSize: 5);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.Count);
        
        // Verify the cities are properly mapped
        foreach (var cityDto in result)
        {
            Assert.True(cityDto.Id > 0);
            Assert.NotNull(cityDto.Name);
            Assert.NotNull(cityDto.Country);
            Assert.True(cityDto.CreatedAt > DateTime.MinValue);
        }

        // Verify no warning was logged since cities exist
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No cities found")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public async Task GetCitiesAsync_ReturnsCorrectPageWhenPaginationApplied()
    {
        // Arrange
        await SeedMultipleCitiesAsync();

        // Act
        var page1 = await _cityService.GetCitiesAsync(page: 1, pageSize: 3);
        var page2 = await _cityService.GetCitiesAsync(page: 2, pageSize: 3);

        // Assert
        Assert.Equal(3, page1.Count);
        Assert.Equal(3, page2.Count);
        
        // Verify different cities are returned for different pages
        var page1Ids = page1.Select(c => c.Id).ToList();
        var page2Ids = page2.Select(c => c.Id).ToList();
        Assert.Empty(page1Ids.Intersect(page2Ids));
    }

    [Fact]
    public async Task GetCitiesAsync_ReturnsEmptyListAndLogsWarningWhenNoCitiesFound()
    {
        // Arrange - Don't seed any cities

        // Act
        var result = await _cityService.GetCitiesAsync(page: 1, pageSize: 10);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
        
        // Verify warning was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No cities found in the database")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetCitiesAsync_LogsErrorAndRethrowsExceptionWhenDatabaseQueryFails()
    {
        // Arrange
        await _context.DisposeAsync(); // Force database error

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(
            () => _cityService.GetCitiesAsync(page: 1, pageSize: 10));

        // Verify error was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("An error occurred while retrieving cities")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region GetCityByIdAsync Tests

    [Fact]
    public async Task GetCityByIdAsync_ReturnsCorrectCityWhenItExists()
    {
        // Arrange
        var city = await SeedSingleCityAsync();

        // Act
        var result = await _cityService.GetCityByIdAsync(city.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(city.Id, result.Id);
        Assert.Equal(city.Name, result.Name);
        Assert.Equal(city.Country, result.Country);
        Assert.Equal(city.PostOffice, result.PostOffice);
        Assert.Equal(city.CreatedAt, result.CreatedAt);
        Assert.Equal(city.UpdatedAt, result.UpdatedAt);

        // Verify no warning was logged since city exists
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public async Task GetCityByIdAsync_ReturnsNullAndLogsWarningWhenCityDoesNotExist()
    {
        // Arrange
        const int nonExistentCityId = 999;

        // Act
        var result = await _cityService.GetCityByIdAsync(nonExistentCityId);

        // Assert
        Assert.Null(result);
        
        // Verify warning was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"City with ID {nonExistentCityId} not found")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetCityByIdAsync_LogsErrorAndRethrowsExceptionWhenDatabaseQueryFails()
    {
        // Arrange
        const int cityId = 1;
        await _context.DisposeAsync(); // Force database error

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(
            () => _cityService.GetCityByIdAsync(cityId));

        // Verify error was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"An error occurred while retrieving city with ID {cityId}")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region CreateCityAsync Tests

    [Fact]
    public async Task CreateCityAsync_CreatesSuccessfullyAndReturnsCorrectCityResponseDto()
    {
        // Arrange
        var createDto = new CreateCityRequestDto
        {
            Name = "New Test City",
            Country = "Test Country",
            PostOffice = "12345"
        };

        // Act
        var result = await _cityService.CreateCityAsync(createDto);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Id > 0);
        Assert.Equal(createDto.Name, result.Name);
        Assert.Equal(createDto.Country, result.Country);
        Assert.Equal(createDto.PostOffice, result.PostOffice);
        Assert.True(result.CreatedAt > DateTime.MinValue);
        Assert.Null(result.UpdatedAt);

        // Verify city was actually saved to database
        var savedCity = await _context.Cities.FindAsync(result.Id);
        Assert.NotNull(savedCity);
        Assert.Equal(createDto.Name, savedCity.Name);
        Assert.Equal(createDto.Country, savedCity.Country);
        Assert.Equal(createDto.PostOffice, savedCity.PostOffice);
    }

    [Fact]
    public async Task CreateCityAsync_ThrowsArgumentNullExceptionWhenCreateCityRequestDtoIsNull()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(
            () => _cityService.CreateCityAsync(null!));
        
        Assert.Equal("cityDto", exception.ParamName);
        Assert.Contains("City DTO cannot be null", exception.Message);
    }

    #endregion

    #region UpdateCityAsync Tests

    [Fact]
    public async Task UpdateCityAsync_UpdatesExistingCitySuccessfullyAndReturnsUpdatedData()
    {
        // Arrange
        var existingCity = await SeedSingleCityAsync();
        var updateDto = new UpdateCityRequestDto
        {
            Name = "Updated City Name",
            Country = "Updated Country",
            PostOffice = "54321"
        };

        // Act
        var result = await _cityService.UpdateCityAsync(existingCity.Id, updateDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(existingCity.Id, result.Id);
        Assert.Equal(updateDto.Name, result.Name);
        Assert.Equal(updateDto.Country, result.Country);
        Assert.Equal(updateDto.PostOffice, result.PostOffice);
        Assert.Equal(existingCity.CreatedAt, result.CreatedAt);
        Assert.NotNull(result.UpdatedAt);
        Assert.True(result.UpdatedAt > result.CreatedAt);

        // Verify city was actually updated in database
        var updatedCity = await _context.Cities.FindAsync(existingCity.Id);
        Assert.NotNull(updatedCity);
        Assert.Equal(updateDto.Name, updatedCity.Name);
        Assert.Equal(updateDto.Country, updatedCity.Country);
        Assert.Equal(updateDto.PostOffice, updatedCity.PostOffice);
        Assert.NotNull(updatedCity.UpdatedAt);
    }

    [Fact]
    public async Task UpdateCityAsync_ThrowsKeyNotFoundExceptionWhenCityDoesNotExist()
    {
        // Arrange
        const int nonExistentCityId = 999;
        var updateDto = new UpdateCityRequestDto
        {
            Name = "Updated Name",
            Country = "Updated Country",
            PostOffice = "12345"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _cityService.UpdateCityAsync(nonExistentCityId, updateDto));
        
        Assert.Contains($"City with ID {nonExistentCityId} not found", exception.Message);
    }

    [Fact]
    public async Task UpdateCityAsync_ThrowsArgumentNullExceptionWhenUpdateCityRequestDtoIsNull()
    {
        // Arrange
        var existingCity = await SeedSingleCityAsync();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(
            () => _cityService.UpdateCityAsync(existingCity.Id, null!));
        
        Assert.Equal("cityDto", exception.ParamName);
        Assert.Contains("City DTO cannot be null", exception.Message);
    }

    #endregion

    #region DeleteCityAsync Tests

    [Fact]
    public async Task DeleteCityAsync_DeletesExistingCityAndReturnsTrue()
    {
        // Arrange
        var city = await SeedSingleCityAsync();

        // Act
        var result = await _cityService.DeleteCityAsync(city.Id);

        // Assert
        Assert.True(result);
        
        // Verify city was actually deleted from database
        var deletedCity = await _context.Cities.FindAsync(city.Id);
        Assert.Null(deletedCity);

        // Verify success was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"City with ID {city.Id} deleted successfully")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteCityAsync_ReturnsFalseAndLogsWarningWhenCityDoesNotExist()
    {
        // Arrange
        const int nonExistentCityId = 999;

        // Act
        var result = await _cityService.DeleteCityAsync(nonExistentCityId);

        // Assert
        Assert.False(result);
        
        // Verify warning was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"City with ID {nonExistentCityId} not found")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region Helper Methods

    private async Task<City> SeedSingleCityAsync()
    {
        var city = _testDataBuilder.CreateCity()
            .WithName("Test City")
            .WithCountry("Test Country")
            .Build();

        _context.Cities.Add(city);
        await _context.SaveChangesAsync();
        return city;
    }

    private async Task SeedMultipleCitiesAsync()
    {
        var cities = new List<City>();
        for (int i = 1; i <= 10; i++)
        {
            var city = _testDataBuilder.CreateCity(i)
                .WithName($"City {i}")
                .WithCountry($"Country {i}")
                .Build();
            cities.Add(city);
        }

        _context.Cities.AddRange(cities);
        await _context.SaveChangesAsync();
    }

    #endregion

    public void Dispose()
    {
        _context.Dispose();
    }
}
