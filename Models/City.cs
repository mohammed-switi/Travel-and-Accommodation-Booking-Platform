using System.ComponentModel.DataAnnotations;

namespace Final_Project.Models;

public class City
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; }

    public string Country { get; set; }

    public string? PostOffice { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public ICollection<Hotel> Hotels { get; set; }
    
}
