using Final_Project.DTOs;
using Final_Project.Services;
using Microsoft.AspNetCore.Mvc;

namespace Final_Project.Controllers;

[ApiController]
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
}