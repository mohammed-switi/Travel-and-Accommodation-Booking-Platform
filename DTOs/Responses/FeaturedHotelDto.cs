namespace Final_Project.DTOs.Responses;

public class FeaturedHotelDto
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public int StarRating { get; set; }
    public required string Location { get; set; }
    public string? Description { get; set; }
    public required List<string> ImageUrls { get; set; }
    public decimal DiscountedPrice { get; set; }
    public List<ReviewDto>? Reviews { get; set; }
}
