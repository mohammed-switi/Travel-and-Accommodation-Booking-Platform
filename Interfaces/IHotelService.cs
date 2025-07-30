using Final_Project.DTOs;
using Final_Project.DTOs.Requests;
using Final_Project.DTOs.Responses;

namespace Final_Project.Services;

public interface IHotelService
{
    Task<List<HotelResponseDto>> GetHotelsAsync(int page, int pageSize, bool includeInactive = false);
    Task<HotelResponseDto> GetHotelByIdAsync(int id);
    Task<HotelResponseDto> CreateHotelAsync(CreateHotelRequestDto hotelDto);
    Task<HotelResponseDto> UpdateHotelAsync(int id, UpdateHotelRequestDto hotelDto);
    Task<bool> DeleteHotelAsync(int id);
    Task<List<HotelSearchResultDto>> SearchHotelsAsync(SearchHotelsDto dto);

    Task<HotelResponseDto?> GetHotelDetailsAsync(int hotelId, DateTime? checkIn, DateTime? checkOut);
}