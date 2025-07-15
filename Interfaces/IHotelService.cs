using Final_Project.DTOs;

namespace Final_Project.Services;

public interface IHotelService
{
    Task<List<HotelSearchResultDto>> SearchHotelsAsync(SearchHotelsDto dto);

    Task<HotelDetailsDto> GetHotelDetailsAsync(int hotelId, DateTime? checkIn, DateTime? checkOut);
}