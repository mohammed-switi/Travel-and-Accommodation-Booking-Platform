using System.ComponentModel.DataAnnotations;

namespace Final_Project.DTOs.Requests;

public class UpdateCityRequestDto
{
    [Required]
    public string Name { get; set; }

    [Required]
    public string Country { get; set; }

    public string PostOffice { get; set; }
}
