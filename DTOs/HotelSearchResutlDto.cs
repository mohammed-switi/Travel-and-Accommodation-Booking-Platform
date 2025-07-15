namespace Final_Project.DTOs;

public class HotelSearchResultDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string City { get; set; }
    public int StarRating { get; set; }
    public string? ImageUrl { get; set; }
    public decimal MinRoomPrice { get; set; }
}
