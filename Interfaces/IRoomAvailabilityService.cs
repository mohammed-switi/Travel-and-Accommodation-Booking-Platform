using Final_Project.DTOs;
using Final_Project.DTOs.Responses;

namespace Final_Project.Services;

public interface IRoomAvailabilityService
{
    Task<List<RoomResponseDto>> GetRoomAvailabilityAsync(int hotelId, DateTime? checkIn, DateTime? checkOut);
    
    Task<bool> IsRoomAvailableAsync(int roomId, DateTime checkInDate, DateTime checkOutDate);
    
    
}
