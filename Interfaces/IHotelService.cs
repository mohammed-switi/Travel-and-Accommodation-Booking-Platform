using Final_Project.DTOs;
using Final_Project.DTOs.Requests;
using Final_Project.DTOs.Responses;

namespace Final_Project.Services;

public interface IHotelService
{
    Task<List<HotelResponseDto>> GetHotelsAsync(int page, int pageSize, bool includeInactive = false);
    Task<HotelResponseDto> GetHotelByIdAsync(int id);
    Task<HotelResponseDto> CreateHotelAsync(CreateHotelRequestDto hotelDto, int userId, string userRole);
    Task<HotelResponseDto> UpdateHotelAsync(int id, UpdateHotelRequestDto hotelDto, int userId, string userRole);
    Task<bool> DeleteHotelAsync(int id, int userId, string userRole);
    Task<List<HotelSearchResultDto>> SearchHotelsAsync(SearchHotelsDto dto);
    Task<HotelResponseDto?> GetHotelDetailsAsync(int hotelId, DateTime? checkIn, DateTime? checkOut);
}