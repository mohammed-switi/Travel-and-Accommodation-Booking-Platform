using Final_Project.Data;
using Final_Project.DTOs;
using Final_Project.Models;
using Microsoft.EntityFrameworkCore;

namespace Final_Project.Services;

public class HotelService(AppDbContext context, ILogger logger) : IHotelService
{
    public async Task<List<HotelSearchResultDto>> SearchHotelsAsync(SearchHotelsDto dto)
    {
        if (dto.CheckInDate >= dto.CheckOutDate)
        {
            logger.LogError("Check-out date must be after check-in date");
            throw new ArgumentException("Check-out date must be after check-in date.");
        }

        var query = context.Hotels
            .Include(h => h.Rooms)
            .Where(h => h.City.Name.ToLower().Contains(dto.Location.ToLower()) && h.IsActive)
            .AsQueryable();

        if (dto.MinPrice.HasValue)
            query = query.Where(h => h.Rooms.Any(r => r.PricePerNight >= dto.MinPrice));
        
        if (dto.MaxPrice.HasValue)
            query = query.Where(h => h.Rooms.Any(r => r.PricePerNight <= dto.MaxPrice));

        if (dto.StarRating.HasValue)
            query = query.Where(h => h.StarRating >= dto.StarRating);


        if (dto.Amenities?.Any() == true)
        {
            var combinedAmenities = dto.Amenities.Aggregate((a1, a2) => a1 | a2);
            query = query.Where(h => (h.Amenities & combinedAmenities) == combinedAmenities);
        }


        if (dto.RoomTypes?.Any() == true)
            query = query.Where(h => h.Rooms.Any(r => dto.RoomTypes.Contains(r.Type)));

        if (dto.CheckInDate != default && dto.CheckOutDate != default)
            query = query.Where(h => !context.BookingItems
                .Where(b => b.CheckOutDate > dto.CheckInDate && b.CheckInDate < dto.CheckOutDate)
                .Any(b => b.Room.HotelId == h.Id));

        if (dto.Adults.HasValue || dto.Children.HasValue || dto.Rooms.HasValue)
            query = query.Where(h => h.Rooms.Any(r =>
                (!dto.Adults.HasValue || r.MaxAdults >= dto.Adults) &&
                (!dto.Children.HasValue || r.MaxChildren >= dto.Children) &&
                (!dto.Rooms.HasValue || r.Quantity >= dto.Rooms)));

        var result = await query
            .Select(h => new HotelSearchResultDto
            {
                Id = h.Id,
                Name = h.Name,
                City = h.City.Name,
                StarRating = h.StarRating,
                ImageUrl = h.MainImage.Url,
                MinRoomPrice = h.Rooms.Min(r => r.PricePerNight)
            })
            .ToListAsync();

        if (!result.Any()) logger.LogWarning("No hotels found matching the search criteria.");

        return result;
    }

    public Task<HotelDetailsDto> GetHotelDetailsAsync(int hotelId, DateTime? checkIn, DateTime? checkOut)
    {
        throw new NotImplementedException();
    }
}
    
