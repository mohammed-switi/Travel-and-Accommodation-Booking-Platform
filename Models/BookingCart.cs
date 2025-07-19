namespace Final_Project.Models;

public class BookingCart
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public List<BookingCartItem> Items { get; set; } = new List<BookingCartItem>();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}