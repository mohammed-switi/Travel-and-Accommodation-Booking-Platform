using Final_Project.Data;
using Final_Project.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Final_Project.Services;

   
public class RoomAvailabilityService(AppDbContext context) : IRoomAvailabilityService
{

    public async Task<List<RoomDto>> GetRoomAvailabilityAsync(int hotelId, DateTime? checkIn, DateTime? checkOut)
    {
        var rooms = await context.Rooms.Where(r => r.HotelId == hotelId).ToListAsync();

        if (checkIn.HasValue && checkOut.HasValue && checkOut > checkIn)
        {
            var overlappingBookings = await context.BookingItems
                .Where(bi => bi.Room.HotelId == hotelId &&
                             bi.CheckOutDate > checkIn &&
                             bi.CheckInDate < checkOut)
                .ToListAsync();

            return rooms.Select(room =>
            {
                var bookedCount = overlappingBookings.Count(b => b.RoomId == room.Id);
                return new RoomDto
                {
                    Id = room.Id,
                    RoomType = room.Type.ToString(),
                    Price = room.PricePerNight,
                    MaxAdults = room.MaxAdults,
                    MaxChildren = room.MaxChildren,
                    AvailableQuantity = room.Quantity - bookedCount
                };
            }).ToList();
        }

        return rooms.Select(room => new RoomDto
        {
            Id = room.Id,
            RoomType = room.Type.ToString(),
            Price = room.PricePerNight,
            MaxAdults = room.MaxAdults,
            MaxChildren = room.MaxChildren,
            AvailableQuantity = room.Quantity
        }).ToList();
    }
    
    
}