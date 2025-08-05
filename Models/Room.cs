using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Final_Project.Enums;

namespace Final_Project.Models;

public class Room
{
    public int Id { get; set; }

    [Required]
    public string RoomNumber { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal PricePerNight { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Discount { get; set; }

    [Required]
    public int MaxAdults { get; set; }

    [Required]
    public int MaxChildren { get; set; }
    
    [Required]
    public int Quantity { get; set; } = 1;

    [Required]
    [ForeignKey("Hotel")] 
    public int HotelId { get; set; }
    public Hotel Hotel { get; set; }

    public ICollection<BookingItem> BookingItems { get; set; }
    
    [Required]
    public RoomType Type { get; set; } = RoomType.Standard;
    
    public string ImageUrl { get; set; } 
}
