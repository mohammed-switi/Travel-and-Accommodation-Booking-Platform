using Final_Project.DTOs;

namespace Final_Project.Services;

public interface ICityService
{
    Task<List<CityDto>> GetCitiesAsync(int page, int pageSize);
    Task<CityDto?> GetCityByIdAsync(int id);
    Task<CityDto> CreateCityAsync(CityDto cityDto);
    Task<CityDto> UpdateCityAsync(int id, CityDto cityDto);
    Task<bool> DeleteCityAsync(int id);
}