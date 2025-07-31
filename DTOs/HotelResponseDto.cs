using Final_Project.DTOs.Responses;
using Final_Project.Enums;
using Final_Project.Models;

namespace Final_Project.DTOs;

public class HotelResponseDto
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string City { get; set; }
    public required string Location { get; set; }
    public List<RoomResponseDto>? Rooms { get; set; }
    public required string Description { get; set; }
    public double StarRating { get; set; }
    public string? ImageUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public required ICollection<string> Images { get; set; }
    public  ICollection<ReviewDto>? Reviews { get; set; } = null;
    public Amenities Amenities { get; set; }
}