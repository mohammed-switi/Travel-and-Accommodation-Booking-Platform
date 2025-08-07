using System.Security.Claims;
using Final_Project.DTOs;
using Final_Project.DTOs.Requests;
using Final_Project.DTOs.Responses;

namespace Final_Project.Services;

public interface IHotelService
{
    Task<List<HotelResponseDto>> GetHotelsAsync(int page, int pageSize, bool includeInactive = false);
    Task<HotelResponseDto> GetHotelByIdAsync(int id,DateTime? checkIn, DateTime? checkOut ,ClaimsPrincipal? user = null);
    Task<HotelResponseDto> CreateHotelAsync(CreateHotelRequestDto hotelDto, int userId, string userRole);
    Task<HotelResponseDto> UpdateHotelAsync(int id, UpdateHotelRequestDto hotelDto, int userId, string userRole);
    Task<bool> DeleteHotelAsync(int id, int userId, string userRole);
    Task<List<HotelSearchResultDto>> SearchHotelsAsync(SearchHotelsDto dto);
}