using Final_Project.Data;
using Final_Project.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Final_Project.Services;

using System.Security.Claims;

public class HomeService(AppDbContext context, ILogger<HomeService> logger) : IHomeService
{
    public async Task<List<HotelDto>> GetFeaturedDealsAsync()
    {
        try
        {
            var featuredDeals = await context.Hotels
                .Where(h => h.IsActive)
                .OrderByDescending(h => h.Rooms.Max(r => r.PricePerNight - r.PricePerNight * (r.Discount / 100)))
                .ThenByDescending(h => h.StarRating)
                .Take(5) // Take top 5 hotels
                .Select(h => new HotelDto
                {
                    Id = h.Id,
                    Name = h.Name,
                    StarRating = h.StarRating,
                    Location = h.Location,
                    Discount = h.Rooms.Max(r => r.PricePerNight - r.PricePerNight * (r.Discount / 100)),
                })
                .ToListAsync();

            if (!featuredDeals.Any())
            {
                logger.LogInformation("No featured deals available.");
            }

            return featuredDeals;
        }
        catch (Exception ex)
        {
            logger.LogInformation($"Error fetching featured deals: {ex.Message}");
            throw;
        }
    }

    public async Task<List<HotelDto>> GetRecentlyViewedHotelsAsync(ClaimsPrincipal user)
    {
        try
        {
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                logger.LogWarning("User ID not found or invalid in claims.");
                return new List<HotelDto>();
            }

            var recentlyViewedHotels = await context.RecentlyViewedHotels
                .Where(rv => rv.UserId == userId)
                .OrderByDescending(rv => rv.ViewedAt)
                .Take(5)
                .Select(rv => new HotelDto
                {
                    Id = rv.Hotel.Id,
                    Name = rv.Hotel.Name,
                    StarRating = rv.Hotel.StarRating,
                    Location = rv.Hotel.Location,
                    Discount = rv.Hotel.Rooms.Max(r => r.PricePerNight - r.PricePerNight * (r.Discount / 100)),
                })
                .ToListAsync();

            if (!recentlyViewedHotels.Any())
            {
                logger.LogInformation($"No recently viewed hotels found for user {userId}.");
            }
            else
            {
                logger.LogInformation(
                    $"Retrieved {recentlyViewedHotels.Count} recently viewed hotels for user {userId}.");
            }

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
            {
                logger.LogInformation("No trending destinations available.");
            }
            else
            {
                logger.LogInformation($"Retrieved {trendingCities.Count} trending destinations.");
            }

            return trendingCities;
        }
        catch (Exception ex)
        {
            logger.LogError($"Error fetching trending destinations: {ex.Message}");
            throw;
        }
    }
}