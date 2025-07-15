using Final_Project.Enums;

namespace Final_Project.DTOs;

public class SearchHotelsDto
{
    public string Location { get; set; }
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public int? Adults { get; set; }
    public int? Children { get; set; }
    public int? Rooms { get; set; }

    // Optional filters
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public int? StarRating { get; set; }
    public List<Amenities>? Amenities { get; set; }
    public List<RoomType>? RoomTypes { get; set; }
}
