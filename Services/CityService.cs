using Final_Project.Data;
using Final_Project.DTOs;
using Final_Project.DTOs.Requests;
using Final_Project.DTOs.Responses;
using Microsoft.EntityFrameworkCore;

namespace Final_Project.Services;

public class CityService(AppDbContext context, ILogger<CityService> logger) : ICityService
{
  public async Task<List<CityResponseDto>> GetCitiesAsync(int page, int pageSize)
{
    try
    {
        var cities = await context.Cities
            .AsNoTracking()
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(city => new CityResponseDto
            {
                Id = city.Id,
                Name = city.Name,
                Country = city.Country,
                PostOffice = city.PostOffice,
                CreatedAt = city.CreatedAt,
                UpdatedAt = city.UpdatedAt
            })
            .ToListAsync();

        if (!cities.Any())
        {
            logger.LogWarning("No cities found in the database.");
        }

        return cities;
    }
    catch (Exception e)
    {
        logger.LogError(e, "An error occurred while retrieving cities.");
        throw;
    }
}
    public async Task<CityResponseDto?> GetCityByIdAsync(int id)
    {
        try
        {
            var city = await context.Cities
                .AsNoTracking()
                .Where(c => c.Id == id)
                .Select(c => new CityResponseDto 
                {
                    Id = c.Id,
                    Name = c.Name,
                    Country = c.Country,
                    PostOffice = c.PostOffice,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt
                })
                .FirstOrDefaultAsync();

            if (city == null)
            {
                logger.LogWarning("City with ID {CityId} not found.", id);
            }

            return city;
        }
        catch (Exception e)
        {
            logger.LogError(e, "An error occurred while retrieving city with ID {CityId}.", id);
            throw;
        }
    }

    public async Task<CityResponseDto> CreateCityAsync(CreateCityRequestDto cityDto)
    {
        if (cityDto == null)
        {
            throw new ArgumentNullException(nameof(cityDto), "City DTO cannot be null");
        }

        var city = new Models.City
        {
            Name = cityDto.Name,
            Country = cityDto.Country,
            PostOffice = cityDto.PostOffice,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = null
        };

        context.Cities.Add(city);
        await context.SaveChangesAsync();

        return new CityResponseDto
        {
            Id = city.Id,
            Name = city.Name,
            Country = city.Country,
            PostOffice = city.PostOffice,
            CreatedAt = city.CreatedAt
        };
    }

    public async Task<CityResponseDto> UpdateCityAsync(int id, UpdateCityRequestDto cityDto)
    {
        if (cityDto == null)
        {
            throw new ArgumentNullException(nameof(cityDto), "City DTO cannot be null");
        }

        var city = await context.Cities.FindAsync(id);
        if (city == null)
        {
            throw new KeyNotFoundException($"City with ID {id} not found.");
        }

        city.Name = cityDto.Name;
        city.Country = cityDto.Country;
        city.PostOffice = cityDto.PostOffice;
        city.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return new CityResponseDto
        {
            Id = city.Id,
            Name = city.Name,
            Country = city.Country,
            PostOffice = city.PostOffice,
            CreatedAt = city.CreatedAt,
            UpdatedAt = city.UpdatedAt
        };
    }

    public async Task<bool> DeleteCityAsync(int id)
    {
        var city = context.Cities.FindAsync(id).Result;
        if (city == null)
        {
            logger.LogWarning($"City with ID {id} not found.");
            return await Task.FromResult(false);
        }

        context.Cities.Remove(city);
        await context.SaveChangesAsync();

        logger.LogInformation($"City with ID {id} deleted successfully.");
        return await Task.FromResult(true);
    }
}