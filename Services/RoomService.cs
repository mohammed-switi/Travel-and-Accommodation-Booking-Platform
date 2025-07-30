using Final_Project.Data;
using Final_Project.DTOs.Requests;
using Final_Project.DTOs.Responses;
using Final_Project.Enums;
using Final_Project.Interfaces;
using Final_Project.Models;
using Microsoft.EntityFrameworkCore;

namespace Final_Project.Services;

public class RoomService(AppDbContext context, ILogger<RoomService> logger) : IRoomService
{
    public async Task<RoomResponseDto> GetRoomByIdAsync(int id)
    {
        var room = await context.Rooms.Include(r => r.Hotel).FirstOrDefaultAsync(r => r.Id == id);
        if (room == null) return null;

        return new RoomResponseDto
        {
            Id = room.Id,
            RoomType = room.Type.ToString(),
            Price = room.Discount,
            MaxAdults = room.MaxAdults,
            MaxChildren = room.MaxChildren,
            AvailableQuantity = room.Quantity
        };
    }

    public async Task<List<RoomResponseDto>> GetRoomsAsync(int page, int pageSize, bool includeInactive = false)
    {
        var rooms = await context.Rooms
            .Where(r => includeInactive || r.Hotel.IsActive)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new RoomResponseDto 
            {
                Id = r.Id,
                RoomType = r.Type.ToString(),
                Price = r.Discount,
                MaxAdults = r.MaxAdults,
                MaxChildren = r.MaxChildren,
                AvailableQuantity = r.Quantity
            })
            .ToListAsync();

        return rooms;
    }

    public async Task<RoomResponseDto> CreateRoomAsync(CreateRoomRequestDto roomDto)
    {
        var hotel = await context.Hotels.FirstOrDefaultAsync(h => h.Id == roomDto.HotelId);
        if (hotel == null)
        {
            logger.LogError("Hotel with ID {HotelId} not found when creating room.", roomDto.HotelId);
            throw new ArgumentException($"Hotel with ID {roomDto.HotelId} not found.");
        }

        var room = new Room
        {
            Type = Enum.Parse<RoomType>(roomDto.RoomType),
            Discount = roomDto.Price,
            MaxAdults = roomDto.MaxAdults,
            MaxChildren = roomDto.MaxChildren,
            Quantity = roomDto.AvailableQuantity,
            HotelId = hotel.Id
        };

        context.Rooms.Add(room);
        await context.SaveChangesAsync();

        return new RoomResponseDto
        {
            Id = room.Id,
            RoomType = room.Type.ToString(),
            Price = room.Discount,
            MaxAdults = room.MaxAdults,
            MaxChildren = room.MaxChildren,
            AvailableQuantity = room.Quantity
        };
    }

    public async Task<RoomResponseDto> UpdateRoomAsync(int id, UpdateRoomRequestDto roomDto)
    {
        var room = await context.Rooms.FirstOrDefaultAsync(r => r.Id == id);
        if (room == null)
        {
            logger.LogError("Room with ID {RoomId} not found for update.", id);
            throw new ArgumentException($"Room with ID {id} not found.");
        }

        room.Type = Enum.Parse<RoomType>(roomDto.RoomType);
        room.Discount = roomDto.Price;
        room.MaxAdults = roomDto.MaxAdults;
        room.MaxChildren = roomDto.MaxChildren;
        room.Quantity = roomDto.AvailableQuantity;

        await context.SaveChangesAsync();

        return new RoomResponseDto
        {
            Id = room.Id,
            RoomType = room.Type.ToString(),
            Price = room.Discount,
            MaxAdults = room.MaxAdults,
            MaxChildren = room.MaxChildren,
            AvailableQuantity = room.Quantity
        };
    }

    public async Task<bool> DeleteRoomAsync(int id)
    {
        var room = await context.Rooms.Include(r => r.BookingItems).FirstOrDefaultAsync(r => r.Id == id);
        if (room == null)
        {
            logger.LogError("Room with ID {RoomId} not found for deletion.", id);
            return false;
        }

        var hasActiveBookings = await context.BookingItems.AnyAsync(bi => bi.RoomId == id && bi.Booking.Status != Final_Project.Enums.BookingStatus.Cancelled);
        if (hasActiveBookings)
        {
            logger.LogError("Cannot delete room with ID {RoomId} because it has active bookings.", id);
            throw new InvalidOperationException("Cannot delete room with active bookings.");
        }

        context.Rooms.Remove(room);
        await context.SaveChangesAsync();
        return true;
    }
}
