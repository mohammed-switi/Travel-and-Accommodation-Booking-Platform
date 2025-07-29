using Final_Project.Data;
using Final_Project.DTOs;
using Final_Project.Models;
using Microsoft.EntityFrameworkCore;

namespace Final_Project.Services;

public class HotelService(
    AppDbContext context,
    IImageService imageService,
    IRoomAvailabilityService roomAvailabilityService,
    ILogger<HotelService> logger) : IHotelService
{
    public async Task<List<HotelDto>> GetHotelsAsync(bool includeInactive = false)
    {
        try
        {
            var hotels = await context.Hotels
                .Where(h => includeInactive || h.IsActive)
                .Select(h => new HotelDto
                {
                    Id = h.Id,
                    Name = h.Name,
                    StarRating = h.StarRating,
                    Location = h.City.Name,
                    Description = h.Description,
                    ImageUrl = h.MainImage.Url,
                    Images = h.Images.Select(i => i.Url).ToList(),
                    Amenities = h.Amenities,
                    Reviews = h.Reviews.Select(r => new ReviewDto
                    {
                        UserName = r.User.FullName,
                        Rating = r.Rating,
                        Comment = r.Comment,
                        CreatedAt = r.CreatedAt
                    }).ToList(),
                    IsActive = h.IsActive,
                    CreatedAt = h.CreatedAt,
                    UpdatedAt = h.UpdatedAt
                })
                .ToListAsync();

            if (!hotels.Any()) logger.LogWarning("No active hotels found in the database.");

            return hotels;
        }
        catch (Exception e)
        {
            logger.LogError(e, "An error occurred while retrieving hotels.");
            throw;
        }
    }

    public async Task<HotelDto> GetHotelByIdAsync(int id)
    {
        var hotel = await context.Hotels
            .Where(h => h.Id == id && h.IsActive)
            .Select(hl => new HotelDto
            {
                Id = hl.Id,
                Name = hl.Name,
                StarRating = hl.StarRating,
                Location = hl.City.Name,
                Description = hl.Description,
                ImageUrl = hl.MainImage.Url,
                Images = hl.Images.Select(i => i.Url).ToList(),
                Amenities = hl.Amenities,
                Reviews = hl.Reviews.Select(r => new ReviewDto
                {
                    UserName = r.User.FullName,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt
                }).ToList(),
                IsActive = hl.IsActive,
                CreatedAt = hl.CreatedAt,
                UpdatedAt = hl.UpdatedAt
            })
            .FirstOrDefaultAsync();


        if (ReferenceEquals(hotel, null)) logger.LogWarning("Hotel with ID {HotelId} not found.", id);

        return hotel;
    }

    public Task<HotelDto> CreateHotelAsync(HotelDto hotelDto)
    {
        throw new NotImplementedException();
    }

    public Task<HotelDto> UpdateHotelAsync(int id, HotelDto hotelDto)
    {
        throw new NotImplementedException();
    }

    public Task<bool> DeleteHotelAsync(int id)
    {
        throw new NotImplementedException();
    }

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