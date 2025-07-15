using System.Security.Claims;
using Final_Project.DTOs;

namespace Final_Project.Services;

public interface IHomeService
{
    Task<List<HotelDto>> GetFeaturedDealsAsync();
    Task<List<HotelDto>> GetRecentlyViewedHotelsAsync(ClaimsPrincipal user);
    Task<List<TrendingCityDto>> GetTrendingDestinationsAsync();
}