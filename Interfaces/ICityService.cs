using Final_Project.DTOs.Requests;
using Final_Project.DTOs.Responses;

namespace Final_Project.Services;

public interface ICityService
{
    Task<List<CityResponseDto>> GetCitiesAsync(int page, int pageSize);
    Task<CityResponseDto?> GetCityByIdAsync(int id);
    Task<CityResponseDto> CreateCityAsync(CreateCityRequestDto cityDto);
    Task<CityResponseDto> UpdateCityAsync(int id, UpdateCityRequestDto cityDto);
    Task<bool> DeleteCityAsync(int id);
}