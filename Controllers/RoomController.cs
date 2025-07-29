using Final_Project.DTOs;
using Final_Project.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Final_Project.Controllers;

[ApiController]
[Authorize]
[Route("api/rooms/[controller]")]
public class RoomController(IRoomService roomService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetRooms([FromQuery] bool includeInactive = false)
    {
        var rooms = await roomService.GetRoomsAsync(includeInactive);
        return rooms.Count > 0 ? Ok(rooms) : NoContent();
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetRoomById(int id)
    {
        var room = await roomService.GetRoomByIdAsync(id);
        return room != null ? Ok(room) : NotFound();
    }

    [HttpPost]
    public async Task<IActionResult> CreateRoom([FromBody] RoomDto roomDto)
    {
        try
        {
            var createdRoom = await roomService.CreateRoomAsync(roomDto);
            return CreatedAtAction(nameof(GetRoomById), new { id = createdRoom.Id }, createdRoom);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateRoom(int id, [FromBody] RoomDto roomDto)
    {
        try
        {
            var updatedRoom = await roomService.UpdateRoomAsync(id, roomDto);
            return Ok(updatedRoom);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteRoom(int id)
    {
        try
        {
            var success = await roomService.DeleteRoomAsync(id);
            return success ? NoContent() : NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }
}
