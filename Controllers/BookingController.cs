using Final_Project.Dtos;
using Final_Project.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Final_Project.DTOs;
using Final_Project.DTOs.Responses;

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
                int userId = GetUserId();
                var cart = await bookingService.GetOrCreateCartAsync(userId);

                logger.LogInformation("Retrieved cart for user {UserId} with {ItemCount} items", userId,
                    cart.Items.Count);


                var cartDto = new BookingCartDto
                {
                    Id = cart.Id,
                    Items = cart.Items?.Select(i => new BookingCartItemDto
                    {
                        Id = i.Id,
                        RoomId = i.RoomId,
                        RoomType = i.Room.Type.ToString(),
                        HotelName = i.Room?.Hotel?.Name ?? "Unknown Hotel",
                        CheckInDate = i.CheckInDate,
                        CheckOutDate = i.CheckOutDate,
                        Price = i.Price,
                        ImageUrl = i.Room?.ImageUrl ?? ""
                    }).ToList() ?? new List<BookingCartItemDto>(),
                    TotalAmount = cart.Items?.Sum(i => i.Price) ?? 0m
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
        public async Task<IActionResult> AddToCart([FromBody] AddToCartDto dto)
        {
            try
            {
                logger.LogInformation("Adding room to cart: {@Dto}", dto);
                int userId = GetUserId();
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
                int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                                       throw new InvalidOperationException("User ID not found in claims"));
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
                int userId = GetUserId();
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

                int userId = GetUserId();
                if (booking.UserId != userId && !User.IsInRole("Admin"))
                    return Forbid();

                var bookingDto = new BookingResponseDto
                {
                    Id = booking.Id,
                    UserId = booking.UserId,
                    BookingReference = booking.BookingReference,
                    TotalPrice = booking.TotalPrice,
                    BookingDate = booking.BookingDate,
                    ContactName = booking.ContactName,
                    ContactPhone = booking.ContactPhone,
                    ContactEmail = booking.ContactEmail,
                    PaymentMethod = booking.PaymentMethod,
                    SpecialRequests = booking.SpecialRequests,
                    Status = booking.Status.ToString(),
                    Items = booking.Items?.Select(item => new BookingItemResponseDto
                    {
                        Id = item.Id,
                        RoomId = item.RoomId,
                        RoomType = item.Room?.Type.ToString() ?? "Unknown",
                        RoomPrice = item.Room?.PricePerNight ?? 0,
                        HotelName = item.Room?.Hotel?.Name ?? "Unknown Hotel",
                        HotelCity = item.Room?.Hotel?.City?.Name ?? "Unknown City",
                        HotelId = item.Room?.Hotel?.Id ?? 0,
                        CheckInDate = item.CheckInDate,
                        CheckOutDate = item.CheckOutDate,
                        Price = item.Price,
                        Nights = (item.CheckOutDate - item.CheckInDate).Days
                    }).ToList() ?? new List<BookingItemResponseDto>()
                };

                return Ok(bookingDto);
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

                var bookingDto = new BookingResponseDto
                {
                    Id = booking.Id,
                    UserId = booking.UserId,
                    BookingReference = booking.BookingReference,
                    TotalPrice = booking.TotalPrice,
                    BookingDate = booking.BookingDate,
                    ContactName = booking.ContactName,
                    ContactPhone = booking.ContactPhone,
                    ContactEmail = booking.ContactEmail,
                    PaymentMethod = booking.PaymentMethod,
                    SpecialRequests = booking.SpecialRequests,
                    Status = booking.Status.ToString(),
                    Items = booking.Items?.Select(item => new BookingItemResponseDto
                    {
                        Id = item.Id,
                        RoomId = item.RoomId,
                        HotelId = item.Room?.Hotel?.Id ?? 0,
                        RoomType = item.Room?.Type.ToString() ?? "Unknown",
                        RoomPrice = item.Room?.PricePerNight ?? 0,
                        HotelName = item.Room?.Hotel?.Name ?? "Unknown Hotel",
                        HotelCity = item.Room?.Hotel?.City?.Name ?? "Unknown City",
                        CheckInDate = item.CheckInDate,
                        CheckOutDate = item.CheckOutDate,
                        Price = item.Price,
                        Nights = (item.CheckOutDate - item.CheckInDate).Days
                    }).ToList() ?? new List<BookingItemResponseDto>()
                };

                return Ok(bookingDto);
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
                var userId = GetUserId();
                var bookings = await bookingService.GetUserBookingsAsync(userId);
                
                var bookingDtos = bookings.Select(booking => new BookingResponseDto
                {
                    Id = booking.Id,
                    UserId = booking.UserId,
                    BookingReference = booking.BookingReference,
                    TotalPrice = booking.TotalPrice,
                    BookingDate = booking.BookingDate,
                    ContactName = booking.ContactName,
                    ContactPhone = booking.ContactPhone,
                    ContactEmail = booking.ContactEmail,
                    PaymentMethod = booking.PaymentMethod,
                    SpecialRequests = booking.SpecialRequests,
                    Status = booking.Status.ToString(),
                    Items = booking.Items?.Select(item => new BookingItemResponseDto
                    {
                        Id = item.Id,
                        RoomId = item.RoomId,
                        HotelId = item.Room?.Hotel?.Id,
                        RoomType = item.Room?.Type.ToString() ?? "Unknown",
                        RoomPrice = item.Room?.PricePerNight ?? 0,
                        HotelName = item.Room?.Hotel?.Name ?? "Unknown Hotel",
                        HotelCity = item.Room?.Hotel?.City?.Name ?? "Unknown City",
                        CheckInDate = item.CheckInDate,
                        CheckOutDate = item.CheckOutDate,
                        Price = item.Price,
                        Nights = (item.CheckOutDate - item.CheckInDate).Days
                    }).ToList() ?? new List<BookingItemResponseDto>()
                }).ToList();
                
                return Ok(bookingDtos);
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
        public int RoomId { get; set; }
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
    }
}