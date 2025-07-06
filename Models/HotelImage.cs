namespace Final_Project.Models;

public class HotelImage
{
    public int Id { get; set; }

    public string Url { get; set; }

    public int HotelId { get; set; }

    public Hotel Hotel { get; set; }
}
