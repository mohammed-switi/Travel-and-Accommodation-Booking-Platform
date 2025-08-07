using Final_Project.Data;
using Final_Project.DTOs;
using Final_Project.DTOs.Requests;
using Final_Project.DTOs.Responses;
using Final_Project.Interfaces;
using Final_Project.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Final_Project.Constants;
using Final_Project.Enums;

namespace Final_Project.Services;

public class HotelService(
    AppDbContext context,
    IOwnershipValidationService ownershipValidationService,
    ILogger<HotelService> logger) : IHotelService
{
    public async Task<List<HotelResponseDto>> GetHotelsAsync(int page, int pageSize, bool includeInactive = false)
    {
        try
        {
            
            if (page < 1) throw new ArgumentOutOfRangeException(nameof(page));
            if (pageSize < 1) throw new ArgumentOutOfRangeException(nameof(pageSize));
                    
                    
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
                    Amenities =h.Amenities, 
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
            
            foreach (var hotel in hotels)
            {
                var enumValue = (Amenities)hotel.Amenities;
                hotel.AmenitiesList = Enum.GetValues(typeof(Amenities))
                    .Cast<Amenities>()
                    .Where(a => a != Amenities.None && enumValue.HasFlag(a))
                    .Select(a => a.ToString())
                    .ToList();
            }

            if (!hotels.Any()) logger.LogWarning("No active hotels found in the database.");

            return hotels;
        }
        catch (Exception e)
        {
            logger.LogError(e, "An error occurred while retrieving hotels.");
            throw;
        }
    }

    private async Task<HotelResponseDto> GetHotelByIdAsync(int id)
    {
        var hotel = await context.Hotels
            .Where(h => h.Id == id && h.IsActive)
            .Select(hl => new HotelResponseDto
            {
                Id = hl.Id,
                Name = hl.Name,
                StarRating = hl.StarRating,
                City = hl.City != null ? hl.City.Name : "Unknown City",
                Location = hl.City != null ? hl.City.Name : "Unknown Location",
                Description = hl.Description ?? string.Empty,
                ImageUrl = hl.MainImage != null ? hl.MainImage.Url : string.Empty,
                Images = hl.Images != null ? hl.Images.Select(i => i.Url ?? string.Empty).ToList() : new List<string>(),
                Amenities = hl.Amenities,
                AmenitiesList = Enum.GetValues(typeof(Amenities))
                                    .Cast<Amenities>()
                                    .Where(a => a != Amenities.None && hl.Amenities.HasFlag(a))
                                    .Select(a => a.ToString())
                                    .ToList(),
                Reviews = hl.Reviews != null ? hl.Reviews.Select(r => new ReviewDto
                {
                    UserName = r.User != null ? r.User.FullName ?? "Anonymous" : "Anonymous",
                    Rating = r.Rating,
                    Comment = r.Comment ?? string.Empty,
                    CreatedAt = r.CreatedAt
                }).ToList() : new List<ReviewDto>(),
                CreatedAt = hl.CreatedAt,
                UpdatedAt = hl.UpdatedAt
            })
            .FirstOrDefaultAsync();


        if (ReferenceEquals(hotel, null)) logger.LogWarning("Hotel with ID {HotelId} not found.", id);

        return hotel ?? throw new InvalidOperationException("Hotel not found.");
    }

    public async Task<HotelResponseDto> GetHotelByIdAsync(int id, ClaimsPrincipal? user = null)
    {
        var hotel = await GetHotelByIdAsync(id);
        
        // Add to recently viewed if user is authenticated and not an admin
        if (user != null && user.Identity!.IsAuthenticated)
        {
            var userRole = user.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole != UserRoles.Admin)
            {
                var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var userId))
                {
                    await AddToRecentlyViewedAsync(userId, id);
                }
            }
        }
        
        return hotel;
    }

    private async Task AddToRecentlyViewedAsync(int userId, int hotelId)
    {
        try
        {
            // Check if this hotel is already in the user's recently viewed list
            var existingEntry = await context.RecentlyViewedHotels
                .FirstOrDefaultAsync(rv => rv.UserId == userId && rv.HotelId == hotelId);

            if (existingEntry != null)
            {
                // Update the viewed time if entry already exists
                existingEntry.ViewedAt = DateTime.UtcNow;
                context.RecentlyViewedHotels.Update(existingEntry);
                logger.LogInformation("Updated recently viewed timestamp for user {UserId} and hotel {HotelId}", userId, hotelId);
            }
            else
            {
                // Add new entry
                var recentlyViewedHotel = new RecentlyViewedHotel
                {
                    UserId = userId,
                    HotelId = hotelId,
                    ViewedAt = DateTime.UtcNow
                };

                context.RecentlyViewedHotels.Add(recentlyViewedHotel);
                logger.LogInformation("Added hotel {HotelId} to recently viewed for user {UserId}", hotelId, userId);
            }

            // Keep only the last 10 recently viewed hotels per user
            var userViewedHotels = await context.RecentlyViewedHotels
                .Where(rv => rv.UserId == userId)
                .OrderByDescending(rv => rv.ViewedAt)
                .Skip(10)
                .ToListAsync();

            if (userViewedHotels.Any())
            {
                context.RecentlyViewedHotels.RemoveRange(userViewedHotels);
                logger.LogInformation("Removed {Count} old recently viewed entries for user {UserId}", userViewedHotels.Count, userId);
            }

            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error adding hotel {HotelId} to recently viewed for user {UserId}", hotelId, userId);
            // Don't throw the exception as this is a non-critical feature
        }
    }

    public async Task<HotelResponseDto> CreateHotelAsync(CreateHotelRequestDto hotelDto, int userId, string userRole)
    {
        
      
        
        if (!await ownershipValidationService.CanUserCreateHotelAsync(userRole, hotelDto.OwnerId,userId))
        {
            logger.LogWarning(
                "User {UserId} with role {UserRole} attempted to create hotel  without permission", userId,
                userRole );
            throw new UnauthorizedAccessException($"You don't have permission to create an hotel. with this role {userRole}");
        }

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
            if (mainImage == null) logger.LogError("Main image URL '{ImageUrl}' not found.", hotelDto.ImageUrl);
            
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
            CreatedAt = DateTime.UtcNow,
            OwnerId = userRole == UserRoles.Admin ? hotelDto.OwnerId : userId // Set owner if hotel owner
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
            Images = hotel.Images?.Select(i => i.Url).ToList(),
            CreatedAt = hotel.CreatedAt,
            UpdatedAt = hotel.UpdatedAt,
            Amenities = hotel.Amenities,
              AmenitiesList = Enum.GetValues(typeof(Amenities))
                                                .Cast<Amenities>()
                                                .Where(a => a != Amenities.None && hotel.Amenities.HasFlag(a))
                                                .Select(a => a.ToString())
                                                .ToList(),
        };
    }

    public async Task<HotelResponseDto> UpdateHotelAsync(int id, UpdateHotelRequestDto hotelDto, int userId,
        string userRole)
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

        // Check if user can manage this hotel
        if (!await ownershipValidationService.CanUserManageHotelAsync(userId, userRole, id))
        {
            logger.LogWarning(
                "User {UserId} with role {UserRole} attempted to update hotel {HotelId} without permission", userId,
                userRole, id);
            throw new UnauthorizedAccessException("You don't have permission to update this hotel.");
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
            Amenities = hotel.Amenities,
            
              AmenitiesList = Enum.GetValues(typeof(Amenities))
                                                .Cast<Amenities>()
                                                .Where(a => a != Amenities.None && hotel.Amenities.HasFlag(a))
                                                .Select(a => a.ToString())
                                                .ToList(),
        };
    }

    public async Task<bool> DeleteHotelAsync(int id, int userId, string userRole)
    {
        var hotel = await context.Hotels.Include(h => h.Rooms).Include(h => h.Reviews)
            .FirstOrDefaultAsync(h => h.Id == id);
        if (hotel == null)
        {
            logger.LogError("Hotel with ID {HotelId} not found for deletion.", id);
            return false;
        }

        // Check if user can manage this hotel
        if (!await ownershipValidationService.CanUserManageHotelAsync(userId, userRole, id))
        {
            logger.LogWarning(
                "User {UserId} with role {UserRole} attempted to delete hotel {HotelId} without permission", userId,
                userRole, id);
            throw new UnauthorizedAccessException("You don't have permission to delete this hotel.");
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

    public async Task<HotelResponseDto?> GetHotelByIdAsync(int hotelId, DateTime? checkIn, DateTime? checkOut, ClaimsPrincipal? user = null)
    {
        var hotel = await context.Hotels
            .Include(h => h.Images)
            .Include(h => h.Reviews)
            .ThenInclude(r => r.User)
            .Include(h => h.Rooms)
            .Include(h => h.City)
            .Include(h => h.MainImage)
            .FirstOrDefaultAsync(h => h.Id == hotelId && h.IsActive);

        if (hotel == null)
        {
            logger.LogWarning("Hotel with ID {HotelId} not found.", hotelId);
            return null;
        }

        // Add to recently viewed if user is authenticated and not an admin
        if (user != null && user.Identity!.IsAuthenticated)
        {
            var userRole = user.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole != UserRoles.Admin)
            {
                var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var userId))
                {
                    await AddToRecentlyViewedAsync(userId, hotelId);
                }
            }
        }

        // Filter rooms based on availability if check-in and check-out dates are provided
        var availableRooms = hotel.Rooms.AsEnumerable();
        if (checkIn.HasValue && checkOut.HasValue)
        {
            // Validate dates
            if (checkIn >= checkOut)
            {
                logger.LogError("Check-in date must be before check-out date");
                throw new ArgumentException("Check-in date must be before check-out date.");
            }

            // Filter rooms that are available for the specified dates
            availableRooms = await GetAvailableRoomsForDatesAsync(hotel.Rooms, checkIn.Value, checkOut.Value);
        }

        return new HotelResponseDto
        {
            Id = hotel.Id,
            Name = hotel.Name ?? string.Empty,
            StarRating = hotel.StarRating,
            City = hotel.City?.Name ?? "Unknown City",
            Location = hotel.Location ?? "Unknown Location",
            Description = hotel.Description ?? string.Empty,
            ImageUrl = hotel.MainImage?.Url ?? string.Empty,
            Images = hotel.Images?.Select(i => i.Url ?? string.Empty).ToList() ?? new List<string>(),
            Amenities = hotel.Amenities,
            AmenitiesList = Enum.GetValues(typeof(Amenities))
                .Cast<Amenities>()
                .Where(a => a != Amenities.None && hotel.Amenities.HasFlag(a))
                .Select(a => a.ToString())
                .ToList(),
            Reviews = hotel.Reviews?.Select(r => new ReviewDto
            {
                UserName = r.User?.FullName ?? "Anonymous",
                Rating = r.Rating,
                Comment = r.Comment ?? string.Empty,
                CreatedAt = r.CreatedAt
            }).ToList() ?? new List<ReviewDto>(),
            Rooms = availableRooms.Select(r => new RoomResponseDto
            {
                Id = r.Id,
                RoomType = r.Type.ToString(),
                Price = r.PricePerNight,
                MaxAdults = r.MaxAdults,
                MaxChildren = r.MaxChildren,
                AvailableQuantity = checkIn.HasValue && checkOut.HasValue 
                    ? GetAvailableRoomQuantity(r, checkIn.Value, checkOut.Value)
                    : r.Quantity,
                ImageUrl = r.ImageUrl ?? string.Empty
            }).ToList(),
            CreatedAt = hotel.CreatedAt,
            UpdatedAt = hotel.UpdatedAt
        };
    }

    private async Task<IEnumerable<Room>> GetAvailableRoomsForDatesAsync(ICollection<Room> rooms, DateTime checkIn, DateTime checkOut)
    {
        var availableRooms = new List<Room>();

        foreach (var room in rooms)
        {
            var availableQuantity = GetAvailableRoomQuantity(room, checkIn, checkOut);
            if (availableQuantity > 0)
            {
                availableRooms.Add(room);
            }
        }

        return availableRooms;
    }

    private int GetAvailableRoomQuantity(Room room, DateTime checkIn, DateTime checkOut)
    {
        // Get all confirmed bookings for this room that overlap with the requested dates
        var overlappingBookings = context.BookingItems
            .Count(bi => bi.RoomId == room.Id &&
                         bi.Booking.Status != BookingStatus.Cancelled &&
                         bi.CheckInDate < checkOut &&
                         bi.CheckOutDate > checkIn);

        return Math.Max(0, room.Quantity - overlappingBookings);
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
        if (string.IsNullOrWhiteSpace(dto.Location))
        {
            logger.LogError("Location is required for hotel search.");
            throw new ArgumentException("Location cannot be null or empty.");
        }

        logger.LogInformation($"Filtering hotels by location: {dto.Location}");
        

        var query = context.Hotels
            .Include(h => h.Rooms)
            .Where(h => EF.Functions.Like(h.City.Name, $"%{dto.Location}%") && h.IsActive)
            .AsQueryable();

        logger.LogInformation($"Query after location filter: {query.ToQueryString()}");
        logger.LogInformation($"Count after location filter: {query.Count()}");

        // Reorder filters for better performance
        query = ApplyStarRatingFilter(query, dto);
        logger.LogInformation($"Count after star rating filter: {query.Count()}");

        query = ApplyPriceFilters(query, dto);
        logger.LogInformation($"Count after price filter: {query.Count()}");

        query = ApplyAmenitiesFilter(query, dto);
        logger.LogInformation($"Count after amenities filter: {query.Count()}");

        query = ApplyRoomTypesFilter(query, dto);
        logger.LogInformation($"Count after room types filter: {query.Count()}");

        query = ApplyRoomCapacityFilter(query, dto);
        logger.LogInformation($"Count after room capacity filter: {query.Count()}");

        query = ApplyBookingDateFilter(query, dto);
        logger.LogInformation($"Count after booking date filter: {query.Count()}");

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
        if (dto.CheckInDate == default || dto.CheckOutDate == default)
            return query;
    
        // Validate dates again to ensure they're correct
        if (dto.CheckInDate >= dto.CheckOutDate)
        {
            logger.LogError("Check-out date must be after check-in date");
            throw new ArgumentException("Check-out date must be after check-in date.");
        }
    
        // Find hotels that don't have conflicting bookings for the specified dates
        query = query.Where(h => !context.BookingItems
            .Where(b => b.CheckOutDate > dto.CheckInDate && 
                       b.CheckInDate < dto.CheckOutDate && 
                       b.Booking.Status != BookingStatus.Cancelled)
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
        //print the result of the query here 
// First await the result, then log it
       var hotels = await query.ToListAsync();
       logger.LogInformation($"Executing hotel search query, found {hotels.Count} hotels"); 
       
        return await query
            .Select(h => new HotelSearchResultDto
            {
                Id = h.Id,
                Name = h.Name,
                City = h.City.Name,
                StarRating = h.StarRating,
                ImageUrl = h.MainImage.Url,
                MinRoomPrice = h.Rooms.Any() ? h.Rooms.Min(r => r.PricePerNight) : 0
            })
            .ToListAsync();
    }
}