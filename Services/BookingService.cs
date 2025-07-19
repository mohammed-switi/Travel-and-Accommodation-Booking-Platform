using Final_Project.Data;
using Final_Project.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Final_Project.Dtos;

namespace Final_Project.Services
{
    public class BookingService(
        AppDbContext context,
        IRoomAvailabilityService availabilityService,
        ILogger<BookingService> logger)
        : IBookingService
    {
        private readonly ILogger<BookingService> _logger = logger;

        public async Task<BookingCart> GetOrCreateCartAsync(int userId)
        {
            var cart = await context.BookingCarts
                .Include(c => c.Items)
                .ThenInclude(i => i.Room)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                cart = new BookingCart { UserId = userId };
                context.BookingCarts.Add(cart);
                await context.SaveChangesAsync();
            }

            return cart;
        }

        public async Task<BookingCart> AddToCartAsync(int userId, int roomId, DateTime checkInDate,
            DateTime checkOutDate)
        {
            // Validate dates
            if (checkInDate >= checkOutDate)
            {
                throw new ArgumentException("Check-in date must be before check-out date");
            }

            // Check if room exists
            var room = await context.Rooms.FindAsync(roomId);
            if (room == null)
            {
                throw new ArgumentException("Room not found");
            }

            // Check if room is available for the requested dates
            bool isAvailable = await availabilityService.IsRoomAvailableAsync(roomId, checkInDate, checkOutDate);
            if (!isAvailable)
            {
                throw new InvalidOperationException("Room is not available for the selected dates");
            }

            // Get or create cart
            var cart = await GetOrCreateCartAsync(userId);

            // Calculate number of nights
            int nights = (int)(checkOutDate - checkInDate).TotalDays;

            // Add item to cart
            var cartItem = new BookingCartItem
            {
                BookingCartId = cart.Id,
                RoomId = roomId,
                Room = room,
                CheckInDate = checkInDate,
                CheckOutDate = checkOutDate,
                Price = room.PricePerNight * nights
            };

            cart.Items.Add(cartItem);
            await context.SaveChangesAsync();

            return cart;
        }

        public async Task<BookingCart> RemoveFromCartAsync(int userId, int cartItemId)
        {
            var cart = await GetCartAsync(userId);
            var itemToRemove = cart.Items.FirstOrDefault(i => i.Id == cartItemId);

            if (itemToRemove != null)
            {
                cart.Items.Remove(itemToRemove);
                context.BookingCartItems.Remove(itemToRemove);
                await context.SaveChangesAsync();
            }

            return cart;
        }

        public async Task<BookingCart> GetCartAsync(int userId)
        {
            var cart = await context.BookingCarts
                .Include(c => c.Items)
                .ThenInclude(i => i.Room)
                .ThenInclude(r => r.Hotel)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                throw new InvalidOperationException("Cart not found");
            }

            return cart;
        }


        public async Task<Booking> CreateBookingAsync(int userId, BookingCheckoutDto checkoutInfo)
        {
            var cart = await GetCartAsync(userId);

            if (!cart.Items.Any())
            {
                throw new InvalidOperationException("Cannot create booking with empty cart");
            }

            // Create booking
            var booking = new Booking
            {
                UserId = userId,
                BookingReference = GenerateBookingReference(),
                ContactName = checkoutInfo.ContactName,
                ContactPhone = checkoutInfo.ContactPhone,
                ContactEmail = checkoutInfo.ContactEmail,
                PaymentMethod = checkoutInfo.PaymentMethod,
                SpecialRequests = checkoutInfo.SpecialRequests
            };

            // Create booking items from cart items
            foreach (var cartItem in cart.Items)
            {
                var bookingItem = new BookingItem
                {
                    RoomId = cartItem.RoomId,
                    CheckInDate = cartItem.CheckInDate,
                    CheckOutDate = cartItem.CheckOutDate,
                    Price = cartItem.Price
                };

                booking.Items.Add(bookingItem);
                booking.TotalPrice += cartItem.Price;
            }

            // Save booking
            context.Bookings.Add(booking);

            // Clear cart
            context.BookingCarts.Remove(cart);

            await context.SaveChangesAsync();

            return booking;
        }

        public async Task<Booking> GetBookingAsync(int bookingId)
        {
            try
            {
                return await context.Bookings
                           .Include(b => b.Items)
                           .ThenInclude(i => i.Room)
                           .ThenInclude(r => r.Hotel)
                           .FirstOrDefaultAsync(b => b.Id == bookingId) ??
                       throw new InvalidOperationException("Booking not found");
            }
            catch (Exception e)
            {
                logger.LogError($"Error fetching booking with ID {bookingId}: {e.Message}");
                throw;
            }
        }

        public async Task<Booking> GetBookingByReferenceAsync(string reference)
        {
            try
            {
                return await context.Bookings
                    .Include(b => b.Items)
                    .ThenInclude(i => i.Room)
                    .ThenInclude(r => r.Hotel)
                    .FirstOrDefaultAsync(b => b.BookingReference == reference) ?? throw new InvalidOperationException();
            }
            catch (Exception e)
            {
                logger.LogError($"Error fetching booking with reference {reference}: {e.Message}");
                throw;
            }
        }

        public async Task<List<Booking>> GetUserBookingsAsync(int userId)
        {
            return await context.Bookings
                .Include(b => b.Items)
                .ThenInclude(i => i.Room)
                .ThenInclude(r => r.Hotel)
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();
        }

        private string GenerateBookingReference()
        {
            string prefix = "BK-";
            string uniquePart = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();
            return $"{prefix}{uniquePart}";
        }
    }
}