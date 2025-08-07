using Final_Project.Enums;

namespace Final_Project.DTOs.Responses;

public class BookingResponseDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string BookingReference { get; set; }
    public decimal TotalPrice { get; set; }
    public DateTime BookingDate { get; set; }
    public string ContactName { get; set; }
    public string ContactPhone { get; set; }
    public string ContactEmail { get; set; }
    public string PaymentMethod { get; set; }
    public string? SpecialRequests { get; set; }
    public string Status { get; set; }
    public List<BookingItemResponseDto> Items { get; set; } = new List<BookingItemResponseDto>();
}

public class BookingItemResponseDto
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public int? HotelId { get; set; }
    public string RoomType { get; set; }
    public decimal RoomPrice { get; set; }
    public string HotelName { get; set; }
    public string HotelCity { get; set; }
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public decimal Price { get; set; }
    public int Nights { get; set; }
}
