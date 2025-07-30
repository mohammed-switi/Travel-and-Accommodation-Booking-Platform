namespace Final_Project.DTOs.Responses;

public class RoomResponseDto
{
    public int Id { get; set; }
    public string RoomType { get; set; }
    public decimal Price { get; set; }
    public int MaxAdults { get; set; }
    public int MaxChildren { get; set; }
    public int AvailableQuantity { get; set; }
    public string RoomNumber { get; set; }
    public string ImageUrl { get; set; }
}
