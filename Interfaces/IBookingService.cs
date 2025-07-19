using Final_Project.Dtos;
using Final_Project.Models;

namespace Final_Project.Services
{
    public interface IBookingService
    {
        Task<BookingCart> GetOrCreateCartAsync(string userId);
        Task<BookingCart> AddToCartAsync(string userId, int roomId, DateTime checkInDate, DateTime checkOutDate);
        Task<BookingCart> RemoveFromCartAsync(string userId, int cartItemId);
        Task<BookingCart> GetCartAsync(string userId);
        Task<Booking> CreateBookingAsync(string userId, BookingCheckoutDto checkoutInfo);
        Task<Booking> GetBookingAsync(int bookingId);
        Task<Booking> GetBookingByReferenceAsync(string reference);
        Task<List<Booking>> GetUserBookingsAsync(string userId);
    }
}