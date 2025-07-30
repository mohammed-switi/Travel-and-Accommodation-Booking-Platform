using System.ComponentModel.DataAnnotations;

namespace Final_Project.DTOs.Requests;

public class CreateCityRequestDto
{
    [Required]
    public string Name { get; set; }

    [Required]
    public string Country { get; set; }

    public string PostOffice { get; set; }
}
