namespace Final_Project.Models;

public class Review
{
    public int Id { get; set; }

    public int HotelId { get; set; }

    public Hotel Hotel { get; set; }

    public int UserId { get; set; }

    public User User { get; set; }

    public int Rating { get; set; } // 1-5 stars

    public string Comment { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
