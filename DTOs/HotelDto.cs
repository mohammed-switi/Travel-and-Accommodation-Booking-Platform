namespace Final_Project.DTOs;


public class HotelDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string City { get; set; }
    public string Location { get; set; }
    public double StarRating { get; set; }
    public decimal OriginalPrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal Discount { get; set; }
    public string? ImageUrl { get; set; }
}
