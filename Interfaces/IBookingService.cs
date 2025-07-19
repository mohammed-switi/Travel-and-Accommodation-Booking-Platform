using Final_Project.Dtos;
using Final_Project.Models;

namespace Final_Project.Services
{
    public interface IBookingService
    {
        Task<BookingCart> GetOrCreateCartAsync(int userId);
        Task<BookingCart> AddToCartAsync(int userId, int roomId, DateTime checkInDate, DateTime checkOutDate);
        Task<BookingCart> RemoveFromCartAsync(int userId, int cartItemId);
        Task<BookingCart> GetCartAsync(int userId);
        Task<Booking> CreateBookingAsync(int userId, BookingCheckoutDto checkoutInfo);
        Task<Booking> GetBookingAsync(int bookingId);
        Task<Booking> GetBookingByReferenceAsync(string reference);
        Task<List<Booking>> GetUserBookingsAsync(int userId);
    }
}