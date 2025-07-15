namespace Final_Project.DTOs;

public class HotelDetailsDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int StarRating { get; set; }
    public string Location { get; set; }
    public string Description { get; set; }
    public List<string> ImageUrls { get; set; }
    public List<ReviewDto> Reviews { get; set; }
    public List<RoomDto> Rooms { get; set; }
}




