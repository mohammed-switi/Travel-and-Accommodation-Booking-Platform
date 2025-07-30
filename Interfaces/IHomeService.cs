using System.Security.Claims;
using Final_Project.DTOs;
using Final_Project.DTOs.Responses;

namespace Final_Project.Interfaces;

public interface IHomeService
{
    Task<List<FeaturedHotelDto>> GetFeaturedDealsAsync();
    Task<List<FeaturedHotelDto>> GetRecentlyViewedHotelsAsync(ClaimsPrincipal user);
    Task<List<TrendingCityDto>> GetTrendingDestinationsAsync();
}