namespace Final_Project.DTOs;
public class BookingCartDto
{
    public int Id { get; set; }
    public List<BookingCartItemDto> Items { get; set; }
    public decimal TotalAmount { get; set; }
}