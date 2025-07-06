namespace Final_Project.Models;

public class RecentlyViewedHotel
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public User User { get; set; }

    public int HotelId { get; set; }

    public Hotel Hotel { get; set; }

    public DateTime ViewedAt { get; set; } = DateTime.UtcNow;
}