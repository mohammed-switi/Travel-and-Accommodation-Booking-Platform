using Final_Project.DTOs;
using Final_Project.DTOs.Requests;
using Final_Project.DTOs.Responses;
using Final_Project.Interfaces;
using Final_Project.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Final_Project.Controllers;

[ApiController]
[Route("api/hotels")]
<<<<<<< Updated upstream
public class HotelController(IHotelService hotelService, IJwtService jwtService, ILogger<HotelController> logger): ControllerBase
{
    [HttpPost("search")]
    [Authorize] 
=======
public class HotelController(IHotelService hotelService, ILogger<HotelController> logger): ControllerBase
{
    [HttpPost("search")]
    [Authorize] // Any authenticated user can search for hotels
>>>>>>> Stashed changes
    public async Task<IActionResult> SearchHotels([FromBody] SearchHotelsDto dto)
    {
        try
        {
            var hotels = await hotelService.SearchHotelsAsync(dto);
            return hotels.Count > 0 ? Ok(hotels) : NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }
    
    [HttpGet("{hotelId}")]
    [Authorize] // Any authenticated user can get hotel details
    public async Task<IActionResult> GetHotelDetails(int hotelId, [FromQuery] DateTime? checkIn, [FromQuery] DateTime? checkOut)
    {
        var details = await hotelService.GetHotelDetailsAsync(hotelId, checkIn, checkOut);

        if (details == null) return NotFound();

        return Ok(details);
    }
       
    [HttpPost]
    [Authorize(Policy = "RequireAdminOrHotelOwner")] // Only admins or hotel owners can create hotels
    public async Task<IActionResult> CreateHotel([FromBody] CreateHotelRequestDto hotelDto)
    {
        try
        {
<<<<<<< Updated upstream
            var (userId, userRole) = jwtService.GetUserInfoFromClaims(User);
=======
            // Extract user information from claims
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? 
                                   throw new InvalidOperationException("User ID not found in claims"));
            string userRole = User.FindFirstValue(ClaimTypes.Role) ?? 
                              throw new InvalidOperationException("User role not found in claims");
>>>>>>> Stashed changes
            
            var createdHotel = await hotelService.CreateHotelAsync(hotelDto, userId, userRole);
            return CreatedAtAction(nameof(GetHotelById), new { id = createdHotel.Id }, createdHotel);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating hotel");
            return StatusCode(500, new { Message = "An error occurred while creating the hotel." });
        }
    }

    [HttpPut("{id}")]
<<<<<<< Updated upstream
    [Authorize(Policy = "RequireAdminOrHotelOwner")] 
=======
    [Authorize(Policy = "RequireAdminOrHotelOwner")] // Only admins or hotel owners can update hotels
>>>>>>> Stashed changes
    public async Task<IActionResult> UpdateHotel(int id, [FromBody] UpdateHotelRequestDto hotelDto)
    {
        try
        {
<<<<<<< Updated upstream
            var (userId, userRole) = jwtService.GetUserInfoFromClaims(User);
=======
            // Extract user information from claims
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? 
                                   throw new InvalidOperationException("User ID not found in claims"));
            string userRole = User.FindFirstValue(ClaimTypes.Role) ?? 
                              throw new InvalidOperationException("User role not found in claims");
>>>>>>> Stashed changes
            
            var updatedHotel = await hotelService.UpdateHotelAsync(id, hotelDto, userId, userRole);
            return Ok(updatedHotel);
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
            logger.LogError(ex, "Error updating hotel");
            return StatusCode(500, new { Message = "An error occurred while updating the hotel." });
        }
    }

    [HttpDelete("{id}")]
<<<<<<< Updated upstream
    [Authorize(Policy = "RequireAdminOrHotelOwner")] 
=======
    [Authorize(Policy = "RequireAdminOrHotelOwner")] // Only admins or hotel owners can delete their own hotels
>>>>>>> Stashed changes
    public async Task<IActionResult> DeleteHotel(int id)
    {
        try
        {
<<<<<<< Updated upstream
            var (userId, userRole) = jwtService.GetUserInfoFromClaims(User);
=======
            // Extract user information from claims
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? 
                                   throw new InvalidOperationException("User ID not found in claims"));
            string userRole = User.FindFirstValue(ClaimTypes.Role) ?? 
                              throw new InvalidOperationException("User role not found in claims");
>>>>>>> Stashed changes
            
            var success = await hotelService.DeleteHotelAsync(id, userId, userRole);
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
            logger.LogError(ex, "Error deleting hotel");
            return StatusCode(500, new { Message = "An error occurred while deleting the hotel." });
        }
    }

    [HttpGet]
<<<<<<< Updated upstream
    [Authorize] 
=======
    [Authorize] // Any authenticated user can get hotels list
>>>>>>> Stashed changes
    public async Task<IActionResult> GetHotels([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] bool includeInactive = false)
    {
        try
        {
            var hotels = await hotelService.GetHotelsAsync(page, pageSize, includeInactive);
            return hotels.Count > 0 ? Ok(hotels) : NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "An error occurred while retrieving hotels.", Details = ex.Message });
        }
    }

    [HttpGet("{id}")]
<<<<<<< Updated upstream
    [Authorize] 
=======
    [Authorize] // Any authenticated user can get a specific hotel
>>>>>>> Stashed changes
    public async Task<IActionResult> GetHotelById(int id)
    {
        try
        {
            var hotel = await hotelService.GetHotelByIdAsync(id);
            return hotel != null ? Ok(hotel) : NotFound();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "An error occurred while retrieving the hotel.", Details = ex.Message });
        }
    }
}