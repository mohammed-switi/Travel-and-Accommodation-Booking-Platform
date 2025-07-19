using System.ComponentModel.DataAnnotations.Schema;
using Final_Project.Enums;

namespace Final_Project.Models;

public class Room
{
    public int Id { get; set; }

    public string RoomNumber { get; set; }

    public decimal PricePerNight { get; set; }


    public decimal Discount { get; set; }

    public int MaxAdults { get; set; }

    public int MaxChildren { get; set; }
    
    public int Quantity { get; set; } = 1;

    [ForeignKey("Hotel")] public int HotelId { get; set; }

    public Hotel Hotel { get; set; }

    public ICollection<BookingItem> BookingItems { get; set; }
    
    public RoomType Type { get; set; } = RoomType.Standard;
    
    public string ImageUrl { get; set; } 
    
    
}