using System.ComponentModel.DataAnnotations;

namespace Final_Project.DTOs;

public class ForgotPasswordDto
{
    [Required]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    public string Email { get; set; }
}