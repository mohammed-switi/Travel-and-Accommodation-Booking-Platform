namespace Final_Project.DTOs;

public class BookingCartItemDto
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public string RoomType { get; set; }
    public string? HotelName { get; set; }
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public decimal Price { get; set; }
    public string ImageUrl { get; set; }
}