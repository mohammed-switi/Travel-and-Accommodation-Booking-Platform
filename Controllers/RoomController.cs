using Final_Project.DTOs.Requests;
using Final_Project.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Final_Project.Controllers;

[ApiController]
[Route("api/rooms")]
<<<<<<< Updated upstream
public class RoomController(IRoomService roomService, IJwtService jwtService, ILogger<RoomController> logger) : ControllerBase
{
    [HttpGet]
    [Authorize] 
=======
public class RoomController(IRoomService roomService) : ControllerBase
{
    [HttpGet]
    [Authorize] // Any authenticated user can get rooms list
>>>>>>> Stashed changes
    public async Task<IActionResult> GetRooms([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] bool includeInactive = false)
    {
        var rooms = await roomService.GetRoomsAsync(page, pageSize, includeInactive);
        return rooms.Count > 0 ? Ok(rooms) : NoContent();
    }

    [HttpGet("{id}")]
<<<<<<< Updated upstream
    [Authorize]
=======
    [Authorize] // Any authenticated user can get a specific room
>>>>>>> Stashed changes
    public async Task<IActionResult> GetRoomById(int id)
    {
        var room = await roomService.GetRoomByIdAsync(id);
        return room != null ? Ok(room) : NotFound();
    }

    [HttpPost]
<<<<<<< Updated upstream
    [Authorize(Policy = "RequireAdminOrHotelOwner")] 
=======
    [Authorize(Policy = "RequireAdminOrHotelOwner")] // Only admins or hotel owners can create rooms
>>>>>>> Stashed changes
    public async Task<IActionResult> CreateRoom([FromBody] CreateRoomRequestDto roomDto)
    {
        try
        {
            var (userId, userRole) = jwtService.GetUserInfoFromClaims(User);

            var createdRoom = await roomService.CreateRoomAsync(roomDto, userId, userRole);
            return CreatedAtAction(nameof(GetRoomById), new { id = createdRoom.Id }, createdRoom);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating room");
            return StatusCode(500, new { Message = "An error occurred while creating the room." });
        }
    }

    [HttpPut("{id}")]
<<<<<<< Updated upstream
    [Authorize(Policy = "RequireAdminOrHotelOwner")] 
=======
    [Authorize(Policy = "RequireAdminOrHotelOwner")] // Only admins or hotel owners can update rooms
>>>>>>> Stashed changes
    public async Task<IActionResult> UpdateRoom(int id, [FromBody] UpdateRoomRequestDto roomDto)
    {
        try
        {
            var (userId, userRole) = jwtService.GetUserInfoFromClaims(User);

            var updatedRoom = await roomService.UpdateRoomAsync(id, roomDto, userId, userRole);
            return Ok(updatedRoom);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating room");
            return StatusCode(500, new { Message = "An error occurred while updating the room." });
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "RequireAdminOrHotelOwner")]
    public async Task<IActionResult> DeleteRoom(int id)
    {
        try
        {
            var (userId, userRole) = jwtService.GetUserInfoFromClaims(User);

            var success = await roomService.DeleteRoomAsync(id, userId, userRole);
            return success ? NoContent() : NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting room");
            return StatusCode(500, new { Message = "An error occurred while deleting the room." });
        }
    }
}
