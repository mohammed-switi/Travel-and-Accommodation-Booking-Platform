using Final_Project.Enums;
using Final_Project.Models;

namespace Final_Project.DTOs;

public class HotelDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string City { get; set; }
    public string Location { get; set; }
    public string Description { get; set; }
    public double StarRating { get; set; }
    public decimal OriginalPrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal Discount { get; set; }
    public string? ImageUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public ICollection<string> Images { get; set; }
    public ICollection<ReviewDto> Reviews { get; set; }
    public Boolean IsActive { get; set; } = true;
    public Amenities Amenities { get; set; }
}