using System.ComponentModel.DataAnnotations;

namespace Final_Project.DTOs.Requests;

public class CreateRoomRequestDto
{
    
    [Required]
    public int HotelId { get; set; }
    [Required]
    public string RoomType { get; set; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than zero.")]
    public decimal Price { get; set; }

    [Required]
    public int MaxAdults { get; set; }

    [Required]
    public int MaxChildren { get; set; }

    [Required]
    public int AvailableQuantity { get; set; }
    
    public string RoomNumber { get; set; }
    public string ImageUrl { get; set; }
}
