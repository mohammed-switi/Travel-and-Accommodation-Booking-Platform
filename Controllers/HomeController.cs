using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Final_Project.Services;

namespace Final_Project.Controllers;

[ApiController]
[Authorize]
[Route("api/home/[controller]")]
public class HomeController(IHomeService homeService) : ControllerBase
{
    [HttpGet("featured-deals")]
    public async Task<IActionResult> GetFeaturedDeals()
    {
        var deals = await homeService.GetFeaturedDealsAsync();
        return Ok(deals);
    }

    
    [HttpGet("recently-viewed")]
    public async Task<IActionResult> GetRecentlyViewedHotels()
    {
        var user = HttpContext.User;
        var hotels = await homeService.GetRecentlyViewedHotelsAsync(user);
        return Ok(hotels);
    }

    [HttpGet("trending-destinations")]
    public async Task<IActionResult> GetTrendingDestinations()
    {
        var destinations = await homeService.GetTrendingDestinationsAsync();
        return Ok(destinations);
    }
}