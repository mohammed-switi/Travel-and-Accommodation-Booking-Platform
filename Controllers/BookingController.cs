using Final_Project.Dtos;
using Final_Project.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Final_Project.DTOs;

namespace Final_Project.Controllers
{
    [ApiController]
    [Route("api/bookings")]
    [Authorize] // Base authorization for all booking endpoints
    public class BookingController(IBookingService bookingService, ILogger<BookingController> logger)
        : ControllerBase
    {
        [HttpGet("cart")]
        public async Task<IActionResult> GetCart()
        {
            try
            {
                int userId=GetUserId();
                var cart = await bookingService.GetOrCreateCartAsync(userId);
                
                var cartDto = new BookingCartDto
                {
                    Id = cart.Id,
                    Items = cart.Items.Select(i => new BookingCartItemDto
                    {
                        Id = i.Id,
                        RoomId = i.RoomId,
                        HotelName = i.Room.Hotel.Name,
                        CheckInDate = i.CheckInDate,
                        CheckOutDate = i.CheckOutDate,
                        Price = i.Price,
                        ImageUrl = i.Room.ImageUrl
                    }).ToList(),
                    TotalAmount = cart.Items.Sum(i => i.Price)
                };
                
                return Ok(cartDto);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting cart");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("cart/add")]
        public async Task<IActionResult> AddToCart(AddToCartDto dto)
        {
            try
            {
                int userId=GetUserId();
                await bookingService.AddToCartAsync(userId, dto.RoomId, dto.CheckInDate, dto.CheckOutDate);
                return Ok(new { message = "Room added to cart successfully" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error adding to cart");
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("cart/remove/{itemId}")]
        public async Task<IActionResult> RemoveFromCart(int itemId)
        {
            try
            {
                int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID not found in claims"));
                await bookingService.RemoveFromCartAsync(userId, itemId);
                return Ok(new { message = "Item removed from cart successfully" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error removing from cart");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("checkout")]
        public async Task<IActionResult> Checkout(BookingCheckoutDto dto)
        {
            try
            {
                int userId=GetUserId();
                var booking = await bookingService.CreateBookingAsync(userId, dto);
                
                return Ok(new 
                { 
                    message = "Booking created successfully",
                    bookingReference = booking.BookingReference,
                    bookingId = booking.Id
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during checkout");
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetBooking(int id)
        {
            try
            {
                var booking = await bookingService.GetBookingAsync(id);

                int userId=GetUserId();
                if (booking.UserId != userId && !User.IsInRole("Admin"))
                    return Forbid();
                
                return Ok(booking);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting booking");
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("reference/{reference}")]
        public async Task<IActionResult> GetBookingByReference(string reference)
        {
            try
            {
                var booking = await bookingService.GetBookingByReferenceAsync(reference);
                if (booking == null) return NotFound();
                int userId = GetUserId();
                if (booking.UserId != userId && !User.IsInRole("Admin"))
                    return Forbid();

                return Ok(booking);

            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting booking by reference");
                return BadRequest(ex.Message);
            }
        }

        private int GetUserId()
        {
            return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException());
        }

        [HttpGet("user")]
        public async Task<IActionResult> GetUserBookings()
        {
            try
            {
                var userId=GetUserId();
                var bookings = await bookingService.GetUserBookingsAsync(userId);
                return Ok(bookings);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting user bookings");
                return BadRequest(ex.Message);
            }
        }
    }

    public class AddToCartDto
    {
        public int RoomId { get; }
        public DateTime CheckInDate { get;  }
        public DateTime CheckOutDate { get; }
    }
}