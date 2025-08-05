using Final_Project.Constants;

namespace Final_Project.Interfaces;

public interface IOwnershipValidationService
{
    Task<bool> CanUserManageHotelAsync(int userId, string userRole, int hotelId);
    Task<bool> CanUserManageRoomAsync(int userId, string userRole, int roomId);
    Task<bool> IsHotelOwnerAsync(int userId, int hotelId);

    Task<bool> CanUserCreateHotelAsync( string userRole, int ownerId = 0, int userId = 0);
    
   

}
