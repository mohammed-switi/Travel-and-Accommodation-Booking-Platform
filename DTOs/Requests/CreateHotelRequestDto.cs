using System.ComponentModel.DataAnnotations;
using Final_Project.Enums;

namespace Final_Project.DTOs.Requests;

public class CreateHotelRequestDto
{
    [Required]
    public string Name { get; set; }

    [Required]
    public string City { get; set; }

    public string Location { get; set; }

    public string Description { get; set; }

    [Required]
    public double StarRating { get; set; }

    public string? ImageUrl { get; set; }

    public Amenities Amenities { get; set; }
    
    public int OwnerId { get; set; }  
    
}
