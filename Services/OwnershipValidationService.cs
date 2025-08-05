using Final_Project.Constants;
using Final_Project.Data;
using Final_Project.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Final_Project.Services;

public class OwnershipValidationService(AppDbContext context, ILogger<OwnershipValidationService> logger) : IOwnershipValidationService
{
    
   
    
    private async Task<bool> IsHotelOwnerCreatingHotelAsync(int ownerId, int userId)
    {
        return ownerId == userId;
     
    }
    
    public async Task<bool> CanUserCreateHotelAsync( string userRole, int ownerId = 0, int userId =0 ){

        if(userRole == UserRoles.Admin)
        {
            return true;
        }
        if (userRole == UserRoles.HotelOwner)
        {
            return await IsHotelOwnerCreatingHotelAsync(ownerId, userId);
        }
        
        return false;
    }
    public async Task<bool> CanUserManageHotelAsync(int userId, string userRole, int hotelId)
    {
        if (userRole == UserRoles.Admin)
        {
            return true;
        }
        
        if (userRole == UserRoles.HotelOwner)
        {
            return await IsHotelOwnerAsync(userId, hotelId);
        }
        
        return false;
    }

    public async Task<bool> CanUserManageRoomAsync(int userId, string userRole, int roomId)
    {
        if (userRole == UserRoles.Admin)
        {
            return true;
        }
        
        var room = await context.Rooms
            .Where(r => r.Id == roomId)
            .Select(r => new { r.HotelId })
            .FirstOrDefaultAsync();
            
        if (room == null)
        {
            return false;
        }
        
        if (userRole == UserRoles.HotelOwner)
        {
            return await IsHotelOwnerAsync(userId, room.HotelId);
        }
        
        return false;
    }

    public async Task<bool> IsHotelOwnerAsync(int userId, int hotelId)
    {
        var hotel = await context.Hotels
            .Where(h => h.Id == hotelId)
            .Select(h => new { h.OwnerId })
            .FirstOrDefaultAsync();
            
        return hotel != null && hotel.OwnerId == userId;
    }
}
