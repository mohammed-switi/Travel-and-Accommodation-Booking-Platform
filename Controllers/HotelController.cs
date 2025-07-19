using Final_Project.DTOs;
using Final_Project.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Final_Project.Controllers;

[ApiController]
[Authorize]
[Route("api/hotels/[controller]")]
public class HotelController(IHotelService hotelService): ControllerBase
{
    [HttpPost("search")]
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
        public async Task<IActionResult> GetHotelDetails(int hotelId, [FromQuery] DateTime? checkIn, [FromQuery] DateTime? checkOut)
        {
            var details = await hotelService.GetHotelDetailsAsync(hotelId, checkIn, checkOut);
    
            if (details == null) return NotFound();
    
            return Ok(details);
        }
}