using Final_Project.Data;
using Final_Project.DTOs;
using Final_Project.Models;
using Microsoft.EntityFrameworkCore;

namespace Final_Project.Services;

public class HotelService(AppDbContext context,IImageService imageService, IRoomAvailabilityService roomAvailabilityService, ILogger<HotelService> logger) : IHotelService
{
    public async Task<List<HotelSearchResultDto>> SearchHotelsAsync(SearchHotelsDto dto)
    {
        ValidateSearchDates(dto);
    
        var query = BuildHotelQuery(dto);
    
        var result = await ExecuteHotelQueryAsync(query);
    
        if (!result.Any()) 
            logger.LogWarning("No hotels found matching the search criteria.");
    
        return result;
    }

    public async Task<HotelDetailsDto> GetHotelDetailsAsync(int hotelId, DateTime? checkIn, DateTime? checkOut)
    {
        var hotel = await context.Hotels
            .Include(h => h.Images)
            .Include(h => h.Reviews)
            .ThenInclude(r => r.User)
            .FirstOrDefaultAsync(h => h.Id == hotelId && h.IsActive);
   
        if (hotel == null) return null;
   
        return new HotelDetailsDto
        {
            Id = hotel.Id,
            Name = hotel.Name,
            StarRating = hotel.StarRating,
            Location = hotel.Location,
            Description = hotel.Description,
            ImageUrls = imageService.GetHotelImageUrls(hotel),
            Reviews = hotel.Reviews?.Select(r => new ReviewDto
            {
                UserName = r.User.FullName,
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt
            }).ToList(),
            Rooms = await roomAvailabilityService.GetRoomAvailabilityAsync(hotelId, checkIn, checkOut)
        };
    }

    private void ValidateSearchDates(SearchHotelsDto dto)
    {
        if (dto.CheckInDate >= dto.CheckOutDate)
        {
            logger.LogError("Check-out date must be after check-in date");
            throw new ArgumentException("Check-out date must be after check-in date.");
        }
    }

    private IQueryable<Hotel> BuildHotelQuery(SearchHotelsDto dto)
    {
        var query = context.Hotels
            .Include(h => h.Rooms)
            .Where(h => h.City.Name.ToLower().Contains(dto.Location.ToLower()) && h.IsActive)
            .AsQueryable();
    
        query = ApplyPriceFilters(query, dto);
        query = ApplyStarRatingFilter(query, dto);
        query = ApplyAmenitiesFilter(query, dto);
        query = ApplyRoomTypesFilter(query, dto);
        query = ApplyBookingDateFilter(query, dto);
        query = ApplyRoomCapacityFilter(query, dto);
    
        return query;
    }

    private IQueryable<Hotel> ApplyPriceFilters(IQueryable<Hotel> query, SearchHotelsDto dto)
    {
        if (dto.MinPrice.HasValue)
            query = query.Where(h => h.Rooms.Any(r => r.PricePerNight >= dto.MinPrice));
    
        if (dto.MaxPrice.HasValue)
            query = query.Where(h => h.Rooms.Any(r => r.PricePerNight <= dto.MaxPrice));
    
        return query;
    }

    private IQueryable<Hotel> ApplyStarRatingFilter(IQueryable<Hotel> query, SearchHotelsDto dto)
    {
        if (dto.StarRating.HasValue)
            query = query.Where(h => h.StarRating >= dto.StarRating);
    
        return query;
    }

    private IQueryable<Hotel> ApplyAmenitiesFilter(IQueryable<Hotel> query, SearchHotelsDto dto)
    {
        if (dto.Amenities?.Any() == true)
        {
            var combinedAmenities = dto.Amenities.Aggregate((a1, a2) => a1 | a2);
            query = query.Where(h => (h.Amenities & combinedAmenities) == combinedAmenities);
        }
    
        return query;
    }

    private IQueryable<Hotel> ApplyRoomTypesFilter(IQueryable<Hotel> query, SearchHotelsDto dto)
    {
        if (dto.RoomTypes?.Any() == true)
            query = query.Where(h => h.Rooms.Any(r => dto.RoomTypes.Contains(r.Type)));
    
        return query;
    }

    private IQueryable<Hotel> ApplyBookingDateFilter(IQueryable<Hotel> query, SearchHotelsDto dto)
    {
        if (dto.CheckInDate != default && dto.CheckOutDate != default)
            query = query.Where(h => !context.BookingItems
                .Where(b => b.CheckOutDate > dto.CheckInDate && b.CheckInDate < dto.CheckOutDate)
                .Any(b => b.Room.HotelId == h.Id));
    
        return query;
    }

    private IQueryable<Hotel> ApplyRoomCapacityFilter(IQueryable<Hotel> query, SearchHotelsDto dto)
    {
        if (dto.Adults.HasValue || dto.Children.HasValue || dto.Rooms.HasValue)
            query = query.Where(h => h.Rooms.Any(r =>
                (!dto.Adults.HasValue || r.MaxAdults >= dto.Adults) &&
                (!dto.Children.HasValue || r.MaxChildren >= dto.Children) &&
                (!dto.Rooms.HasValue || r.Quantity >= dto.Rooms)));
    
        return query;
    }

    private async Task<List<HotelSearchResultDto>> ExecuteHotelQueryAsync(IQueryable<Hotel> query)
    {
        return await query
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
    }
}
    
