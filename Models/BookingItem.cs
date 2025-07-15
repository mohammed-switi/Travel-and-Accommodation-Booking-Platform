using Final_Project.Enums;

namespace Final_Project.Models;

public class BookingItem
{
    public int Id { get; set; }

    public int RoomId { get; set; }

    public Room Room { get; set; }

    public int BookingId { get; set; }

    public Booking Booking { get; set; }

    public decimal Price { get; set; }

    public int Nights { get; set; }
    
}
