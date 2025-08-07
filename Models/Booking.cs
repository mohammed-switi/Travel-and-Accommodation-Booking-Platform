using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Final_Project.Enums;

namespace Final_Project.Models;

public class Booking
{
    public int Id { get; set; }

    [ForeignKey("User")]
    public int UserId { get; set; }

    [JsonIgnore]
    public User User { set; get; }
    
    public string BookingReference { get; set; } = Guid.NewGuid().ToString();
    
    
    public ICollection<BookingItem> Items { get; set; } = new List<BookingItem>();

    public decimal TotalPrice { get; set; }
    public DateTime BookingDate { get; set; } = DateTime.UtcNow;

    public string ContactName { get; set; }
    public string ContactPhone { get; set; }
    public string ContactEmail { get; set; }
    public string PaymentMethod { get; set; } 
    public string? SpecialRequests { get; set; }
    public BookingStatus Status { get; set; }
}