using Final_Project.DTOs.Requests;
using Final_Project.DTOs.Responses;

namespace Final_Project.Interfaces;

public interface IRoomService
{
    Task<RoomResponseDto> GetRoomByIdAsync(int id);
    Task<List<RoomResponseDto>> GetRoomsAsync(int page, int pageSize, bool includeInactive = false);
    Task<RoomResponseDto> CreateRoomAsync(CreateRoomRequestDto roomDto);
    Task<RoomResponseDto> UpdateRoomAsync(int id, UpdateRoomRequestDto roomDto);
    Task<bool> DeleteRoomAsync(int id);
}
