using Final_Project.Data;
using Final_Project.DTOs;
using Final_Project.DTOs.Responses;
using Final_Project.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Final_Project.Services;

using System.Security.Claims;

public class HomeService(AppDbContext context, ILogger<HomeService> logger) : IHomeService
{
    public async Task<List<FeaturedHotelDto>> GetFeaturedDealsAsync()
    {
        try
        {
            var featuredDeals = await context.Hotels
                .Where(h => h.IsActive)
                .OrderByDescending(h => h.Rooms.Max(r => r.PricePerNight - r.PricePerNight * (r.Discount / 100)))
                .ThenByDescending(h => h.StarRating)
                .Take(5) // Take top 5 hotels
                .Select(h => new FeaturedHotelDto
                {
                    Id = h.Id,
                    Name = h.Name,
                    StarRating = h.StarRating,
                    Location = h.Location,
                    Description = h.Description,
                    ImageUrls = h.Images.Select(i => i.Url).ToList(),
                    DiscountedPrice = h.Rooms.Max(r => r.PricePerNight - r.PricePerNight * (r.Discount / 100))
                })
                .ToListAsync();

            if (!featuredDeals.Any()) logger.LogInformation("No featured deals available.");

            return featuredDeals;
        }
        catch (Exception ex)
        {
            logger.LogInformation($"Error fetching featured deals: {ex.Message}");
            throw;
        }
    }

    public async Task<List<FeaturedHotelDto>> GetRecentlyViewedHotelsAsync(ClaimsPrincipal user)
    {
        try
        {
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                logger.LogWarning("User ID not found or invalid in claims.");
                return new List<FeaturedHotelDto>();
            }

            var recentlyViewedHotels = await context.RecentlyViewedHotels
                .Where(rv => rv.UserId == userId)
                .OrderByDescending(rv => rv.ViewedAt)
                .Take(5)
                .Select(rv => new FeaturedHotelDto
                {
                    Id = rv.Hotel.Id,
                    Name = rv.Hotel.Name,
                    StarRating = rv.Hotel.StarRating,
                    Location = rv.Hotel.Location,
                    Description = rv.Hotel.Description,
                    ImageUrls = rv.Hotel.Images.Select(i => i.Url).ToList(),
                    DiscountedPrice = rv.Hotel.Rooms.Max(r => r.PricePerNight - r.PricePerNight * (r.Discount / 100))
                })
                .ToListAsync();

            if (!recentlyViewedHotels.Any())
                logger.LogInformation($"No recently viewed hotels found for user {userId}.");
            else
                logger.LogInformation(
                    $"Retrieved {recentlyViewedHotels.Count} recently viewed hotels for user {userId}.");

            return recentlyViewedHotels;
        }
        catch (Exception ex)
        {
            logger.LogError($"Error fetching recently viewed hotels: {ex.Message}");
            throw;
        }
    }

    public async Task<List<TrendingCityDto>> GetTrendingDestinationsAsync()
    {
        try
        {
            var trendingCities = await context.Bookings
                .Include(b => b.Items)
                .ThenInclude(bi => bi.Room)
                .ThenInclude(r => r.Hotel)
                .SelectMany(b => b.Items.Select(bi => bi.Room.Hotel.City))
                .GroupBy(city => city)
                .Select(g => new { City = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .Take(5)
                .Select(c => new TrendingCityDto
                {
                    CityId = c.City.Id,
                    City = c.City.Name,
                    BookingCount = c.Count
                })
                .ToListAsync();


            if (!trendingCities.Any())
                logger.LogInformation("No trending destinations available.");
            else
                logger.LogInformation($"Retrieved {trendingCities.Count} trending destinations.");

            return trendingCities;
        }
        catch (Exception ex)
        {
            logger.LogError($"Error fetching trending destinations: {ex.Message}");
            throw;
        }
    }
}