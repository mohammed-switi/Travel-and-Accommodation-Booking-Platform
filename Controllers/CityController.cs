using Final_Project.DTOs;
using Final_Project.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Final_Project.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class CityController(ICityService cityService) : ControllerBase
{
    
    [HttpGet]
    public async Task<IActionResult> GetCities()
    {
        var cities = await cityService.GetCitiesAsync();
        return Ok(cities);
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
    public async Task<IActionResult> CreateCity([FromBody] CityDto cityDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var createdCity = await cityService.CreateCityAsync(cityDto);
        return CreatedAtAction(nameof(GetCityById), new { id = createdCity.Id }, createdCity);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCity(int id, [FromBody] CityDto cityDto)
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