using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Final_Project.Models;

public class BookingItem
{
    public int Id { get; set; }

    [Required]
    public int BookingId { get; set; }
    
    [JsonIgnore]    
    public Booking Booking { get; set; }

    [Required]
    public int RoomId { get; set; }
    public Room Room { get; set; }

    [Required]
    public DateTime CheckInDate { get; set; }

    [Required]
    public DateTime CheckOutDate { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }
}
