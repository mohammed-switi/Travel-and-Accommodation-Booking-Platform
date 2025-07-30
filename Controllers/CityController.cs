using Final_Project.DTOs;
using Final_Project.DTOs.Requests;
using Final_Project.DTOs.Responses;
using Final_Project.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Final_Project.Controllers;

[ApiController]
[Authorize]
[Route("api/admin/[controller]")]
public class CityController(ICityService cityService) : ControllerBase
{
    
    [HttpGet]
    public async Task<IActionResult> GetCities([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var cities = await cityService.GetCitiesAsync(page, pageSize);
        return cities.Count > 0 ? Ok(cities) : NoContent();
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetCityById(int id)
    {
        var city = await cityService.GetCityByIdAsync(id);
        if (city == null)
            return NotFound();

        return Ok(city);
    }

    [HttpPost]
    public async Task<IActionResult> CreateCity([FromBody] CreateCityRequestDto cityDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var createdCity = await cityService.CreateCityAsync(cityDto);
        return CreatedAtAction(nameof(GetCityById), new { id = createdCity.Id }, createdCity);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCity(int id, [FromBody] UpdateCityRequestDto cityDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var updatedCity = await cityService.UpdateCityAsync(id, cityDto);
        if (updatedCity == null)
            return NotFound();

        return Ok(updatedCity);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCity(int id)
    {
        var deleted = await cityService.DeleteCityAsync(id);
        if (!deleted)
            return NotFound();

        return NoContent();
    }
    
    
}