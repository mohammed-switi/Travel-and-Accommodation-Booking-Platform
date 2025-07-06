using System.ComponentModel.DataAnnotations.Schema;

namespace Final_Project.Models;

public class Booking
{
    public int Id { get; set; }

    public DateTime BookingDate { get; set; } = DateTime.UtcNow;

    public DateTime CheckInDate { get; set; }

    public DateTime CheckOutDate { get; set; }

    public decimal TotalPrice { get; set; }

    public string Status { get; set; } = "Pending";

    [ForeignKey("User")]
    public int UserId { get; set; }

    public User User { get; set; }

    public ICollection<BookingItem> BookingItems { get; set; }
}
