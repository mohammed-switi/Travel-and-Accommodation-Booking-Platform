
using System.Text.Json.Serialization;

namespace Final_Project.Models;

public class BookingCartItem
{
   public int Id { get; set; }
   public int BookingCartId { get; set; }
   
   [JsonIgnore]
   public BookingCart BookingCart { get; set; }
   public int RoomId { get; set; }
   public Room Room { get; set; }
   public DateTime CheckInDate { get; set; }
   public DateTime CheckOutDate { get; set; }
   public decimal Price { get; set; }
   
}