using System.ComponentModel.DataAnnotations.Schema;

namespace Final_Project.Models;

public class Room
{
    public int Id { get; set; }

    public string RoomNumber { get; set; }

    public decimal PricePerNight { get; set; }


    public decimal Discount { get; set; }

    public int Adults { get; set; }

    public int Children { get; set; }

    public bool IsAvailable { get; set; } = true;

    [ForeignKey("Hotel")] public int HotelId { get; set; }

    public Hotel Hotel { get; set; }

    public ICollection<BookingItem> BookingItems { get; set; }
}