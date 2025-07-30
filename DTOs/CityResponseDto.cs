using System.ComponentModel.DataAnnotations;

namespace Final_Project.DTOs.Responses;

public class CityResponseDto
{
    public int Id { get; set; }
    [Required]
    public string Name { get; set; }
    [Required]
    public string Country { get; set; }
    public string PostOffice { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}