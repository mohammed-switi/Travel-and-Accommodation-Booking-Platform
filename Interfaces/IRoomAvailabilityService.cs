using Final_Project.DTOs;

namespace Final_Project.Services;

public interface IRoomAvailabilityService
{
    Task<List<RoomDto>> GetRoomAvailabilityAsync(int hotelId, DateTime? checkIn, DateTime? checkOut);
    
    Task<bool> IsRoomAvailableAsync(int roomId, DateTime checkInDate, DateTime checkOutDate);
    
    
}
