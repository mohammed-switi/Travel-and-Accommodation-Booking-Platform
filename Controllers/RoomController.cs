using Final_Project.DTOs;
using Final_Project.DTOs.Requests;
using Final_Project.DTOs.Responses;
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
    public async Task<IActionResult> GetRooms([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] bool includeInactive = false)
    {
        var rooms = await roomService.GetRoomsAsync(page, pageSize, includeInactive);
        return rooms.Count > 0 ? Ok(rooms) : NoContent();
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetRoomById(int id)
    {
        var room = await roomService.GetRoomByIdAsync(id);
        return room != null ? Ok(room) : NotFound();
    }

    [HttpPost]
    public async Task<IActionResult> CreateRoom([FromBody] CreateRoomRequestDto roomDto)
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
    public async Task<IActionResult> UpdateRoom(int id, [FromBody] UpdateRoomRequestDto roomDto)
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
