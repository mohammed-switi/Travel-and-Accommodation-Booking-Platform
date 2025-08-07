using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Final_Project.Enums;
using Final_Project.Constants;

namespace Final_Project.Models;

public class Hotel
{
    public int Id { get; set; }

    [Required] public string Name { get; set; }

    public string Description { get; set; }

    public int StarRating { get; set; }


    public string Location { get; set; }

    [ForeignKey("City")] public int CityId { get; set; }

    public City City { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public ICollection<Room> Rooms { get; set; }

    public int? MainImageId { get; set; }

    [ForeignKey("MainImageId")] 
    public HotelImage MainImage { get; set; }
    public ICollection<HotelImage> Images { get; set; }

    public ICollection<Review> Reviews { get; set; }

    public bool IsActive { get; set; } = true;

    public Amenities Amenities { get; set; }

    // Track hotel ownership
    public int OwnerId { get; set; }
    [ForeignKey("OwnerId")]
    public User? Owner { get; set; }
}