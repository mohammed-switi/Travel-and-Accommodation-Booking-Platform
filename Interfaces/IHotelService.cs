using Final_Project.DTOs;

namespace Final_Project.Services;

public interface IHotelService
{
    Task<List<HotelDto>> GetHotelsAsync(int page, int pageSize, bool includeInactive = false);
    Task<HotelDto> GetHotelByIdAsync(int id);
    Task<HotelDto> CreateHotelAsync(HotelDto hotelDto);
    Task<HotelDto> UpdateHotelAsync(int id, HotelDto hotelDto);
    Task<bool> DeleteHotelAsync(int id);
    Task<List<HotelSearchResultDto>> SearchHotelsAsync(SearchHotelsDto dto);

    Task<HotelDetailsDto> GetHotelDetailsAsync(int hotelId, DateTime? checkIn, DateTime? checkOut);
}