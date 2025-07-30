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
       
        
     
        
        
    [HttpPost]
    public async Task<IActionResult> CreateHotel([FromBody] HotelDto hotelDto)
    {
        try
        {
            var createdHotel = await hotelService.CreateHotelAsync(hotelDto);
            return CreatedAtAction(nameof(GetHotelDetails), new { hotelId = createdHotel.Id }, createdHotel);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateHotel(int id, [FromBody] HotelDto hotelDto)
    {
        try
        {
            var updatedHotel = await hotelService.UpdateHotelAsync(id, hotelDto);
            return Ok(updatedHotel);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteHotel(int id)
    {
        try
        {
            var success = await hotelService.DeleteHotelAsync(id);
            return success ? NoContent() : NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpGet]
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