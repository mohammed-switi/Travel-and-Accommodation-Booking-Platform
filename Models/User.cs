using System.ComponentModel.DataAnnotations;

namespace Final_Project.Models;

public class User
{
    public int Id { get; set; }

    [Required, EmailAddress] public string Email { get; set; }

    [Required] public string PasswordHash { get; set; }

    [Required] public string FullName { get; set; }

    public string Role { get; set; } = "User"; // User or Admin

    public ICollection<Booking> Bookings { get; set; }
    public ICollection<RecentlyViewedHotel> RecentlyViewedHotels { get; set; }


    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetTokenExpiration { get; set; }
}