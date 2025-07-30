using System.ComponentModel.DataAnnotations;

namespace Final_Project.DTOs.Requests;

public class CreateRoomRequestDto
{
    
    [Required]
    public int HotelId { get; set; }
    [Required]
    public string RoomType { get; set; }

    [Required]
    public decimal Price { get; set; }

    [Required]
    public int MaxAdults { get; set; }

    [Required]
    public int MaxChildren { get; set; }

    [Required]
    public int AvailableQuantity { get; set; }
}
