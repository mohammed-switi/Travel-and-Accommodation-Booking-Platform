using Final_Project.Data;
using Final_Project.DTOs;
using Final_Project.DTOs.Requests;
using Final_Project.DTOs.Responses;
using Final_Project.Enums;
using Final_Project.Models;
using Microsoft.EntityFrameworkCore;

namespace Final_Project.Services;

public class HotelService(AppDbContext context, ILogger<HotelService> logger) : IHotelService
{
    public async Task<List<HotelResponseDto>> GetHotelsAsync(int page, int pageSize, bool includeInactive = false)
    {
        try
        {
            var hotels = await context.Hotels
                .Where(h => includeInactive || h.IsActive)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(h => new HotelResponseDto
                {
                    Id = h.Id,
                    Name = h.Name,
                    StarRating = h.StarRating,
                    City = h.City.Name,
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

    public async Task<HotelResponseDto> GetHotelByIdAsync(int id)
    {
        var hotel = await context.Hotels
            .Where(h => h.Id == id && h.IsActive)
            .Select(hl => new HotelResponseDto
            {
                Id = hl.Id,
                Name = hl.Name,
                StarRating = hl.StarRating,
                City = hl.City.Name,
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
                CreatedAt = hl.CreatedAt,
                UpdatedAt = hl.UpdatedAt
            })
            .FirstOrDefaultAsync();


        if (ReferenceEquals(hotel, null)) logger.LogWarning("Hotel with ID {HotelId} not found.", id);

        return hotel ?? throw new InvalidOperationException("Hotel not found.");
    }

    public async Task<HotelResponseDto> CreateHotelAsync(CreateHotelRequestDto hotelDto)
    {
        var city = await context.Cities.FirstOrDefaultAsync(c => c.Name == hotelDto.City);
        if (city == null)
        {
            logger.LogError("City '{City}' not found when creating hotel.", hotelDto.City);
            throw new ArgumentException($"City '{hotelDto.City}' not found.");
        }

        HotelImage? mainImage = null;
        if (!string.IsNullOrEmpty(hotelDto.ImageUrl))
        {
            mainImage = await context.HotelImages.FirstOrDefaultAsync(i => i.Url == hotelDto.ImageUrl);
            if (mainImage == null)
            {
                logger.LogError("Main image URL '{ImageUrl}' not found.", hotelDto.ImageUrl);
                throw new ArgumentException($"Main image URL '{hotelDto.ImageUrl}' not found.");
            }
        }

        var hotel = new Hotel
        {
            Name = hotelDto.Name,
            Description = hotelDto.Description,
            StarRating = (int)hotelDto.StarRating,
            Location = hotelDto.Location,
            CityId = city.Id,
            MainImageId = mainImage?.Id,
            Amenities = hotelDto.Amenities,
            CreatedAt = DateTime.UtcNow
        };

        context.Hotels.Add(hotel);
        await context.SaveChangesAsync();

        return new HotelResponseDto
        {
            Id = hotel.Id,
            Name = hotel.Name,
            City = city.Name,
            Location = hotel.Location,
            Description = hotel.Description,
            StarRating = hotel.StarRating,
            ImageUrl = mainImage?.Url,
            Images = hotel.Images.Select(i => i.Url).ToList(),
            Reviews = hotel.Reviews.Select(r => new ReviewDto
            {
                UserName = r.User.FullName,
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt
            }).ToList(),
            CreatedAt = hotel.CreatedAt,
            UpdatedAt = hotel.UpdatedAt,
            Amenities = hotel.Amenities
        };
    }

    public async Task<HotelResponseDto> UpdateHotelAsync(int id, UpdateHotelRequestDto hotelDto)
    {
        var hotel = await context.Hotels
            .Include(h => h.City)
            .Include(h => h.Images)
            .Include(h => h.Reviews)
            .ThenInclude(r => r.User)
            .FirstOrDefaultAsync(h => h.Id == id);

        if (hotel == null)
        {
            logger.LogError("Hotel with ID {HotelId} not found for update.", id);
            throw new ArgumentException($"Hotel with ID {id} not found.");
        }

        var city = await context.Cities.FirstOrDefaultAsync(c => c.Name == hotelDto.City);
        if (city == null)
        {
            logger.LogError("City '{City}' not found when updating hotel.", hotelDto.City);
            throw new ArgumentException($"City '{hotelDto.City}' not found.");
        }

        HotelImage? mainImage = null;
        if (!string.IsNullOrEmpty(hotelDto.ImageUrl))
        {
            mainImage = await context.HotelImages.FirstOrDefaultAsync(i => i.Url == hotelDto.ImageUrl);
            if (mainImage == null)
            {
                logger.LogError("Main image URL '{ImageUrl}' not found.", hotelDto.ImageUrl);
                throw new ArgumentException($"Main image URL '{hotelDto.ImageUrl}' not found.");
            }
        }

        hotel.Name = hotelDto.Name;
        hotel.Description = hotelDto.Description;
        hotel.StarRating = (int)hotelDto.StarRating;
        hotel.Location = hotelDto.Location;
        hotel.CityId = city.Id;
        hotel.MainImageId = mainImage?.Id;
        hotel.Amenities = hotelDto.Amenities;
        hotel.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return new HotelResponseDto
        {
            Id = hotel.Id,
            Name = hotel.Name,
            City = city.Name,
            Location = hotel.Location,
            Description = hotel.Description,
            StarRating = hotel.StarRating,
            ImageUrl = mainImage?.Url,
            Images = hotel.Images.Select(i => i.Url).ToList(),
            Reviews = hotel.Reviews.Select(r => new ReviewDto
            {
                UserName = r.User.FullName,
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt
            }).ToList(),
            CreatedAt = hotel.CreatedAt,
            UpdatedAt = hotel.UpdatedAt,
            Amenities = hotel.Amenities
        };
    }

    public async Task<bool> DeleteHotelAsync(int id)
    {
        var hotel = await context.Hotels.Include(h => h.Rooms).Include(h => h.Reviews)
            .FirstOrDefaultAsync(h => h.Id == id);
        if (hotel == null)
        {
            logger.LogError("Hotel with ID {HotelId} not found for deletion.", id);
            return false;
        }

        var hasActiveBookings = await context.BookingItems.AnyAsync(bi =>
            bi.Room.HotelId == id && bi.Booking.Status != BookingStatus.Cancelled);
        if (hasActiveBookings)
        {
            logger.LogError("Cannot delete hotel with ID {HotelId} because it has active bookings.", id);
            throw new InvalidOperationException("Cannot delete hotel with active bookings.");
        }

        context.Reviews.RemoveRange(hotel.Reviews);
        context.Rooms.RemoveRange(hotel.Rooms);
        context.Hotels.Remove(hotel);
        await context.SaveChangesAsync();
        return true;
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

    public async Task<HotelResponseDto?> GetHotelDetailsAsync(int hotelId, DateTime? checkIn, DateTime? checkOut)
    {
        var hotel = await context.Hotels
            .Include(h => h.Images)
            .Include(h => h.Reviews)
            .ThenInclude(r => r.User)
            .Include(h => h.Rooms).Include(hotel => hotel.City)
            .FirstOrDefaultAsync(h => h.Id == hotelId && h.IsActive);

        if (hotel == null)
        {
            logger.LogWarning("Hotel with ID {HotelId} not found.", hotelId);
            return null;
        }

        return new HotelResponseDto
        {
            Id = hotel.Id,
            Name = hotel.Name,
            StarRating = hotel.StarRating,
            City = hotel.City.Name,
            Location = hotel.Location,
            Description = hotel.Description,
            Images = hotel.Images.Select(i => i.Url).ToList(),
            Reviews = hotel.Reviews.Select(r => new ReviewDto
            {
                UserName = r.User.FullName,
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt
            }).ToList(),
            Rooms = hotel.Rooms.Select(r => new RoomResponseDto
            {
                Id = r.Id,
                RoomType = r.Type.ToString(),
                Price = r.PricePerNight,
                MaxAdults = r.MaxAdults,
                MaxChildren = r.MaxChildren,
                AvailableQuantity = r.Quantity
            }).ToList()
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