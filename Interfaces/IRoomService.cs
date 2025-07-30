using Final_Project.DTOs;

namespace Final_Project.Interfaces;

public interface IRoomService
{
    Task<RoomDto> GetRoomByIdAsync(int id);
    Task<List<RoomDto>> GetRoomsAsync(int page, int pageSize, bool includeInactive = false);
    Task<RoomDto> CreateRoomAsync(RoomDto roomDto);
    Task<RoomDto> UpdateRoomAsync(int id, RoomDto roomDto);
    Task<bool> DeleteRoomAsync(int id);
}
