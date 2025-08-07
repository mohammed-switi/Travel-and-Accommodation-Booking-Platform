using System.ComponentModel.DataAnnotations;

namespace Final_Project.DTOs.Requests;

public class CreateReviewDto
{
    [Required]
    public int HotelId { get; set; }

    [Required]
    [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5 stars")]
    public int Rating { get; set; }

    [Required]
    [StringLength(1000, MinimumLength = 10, ErrorMessage = "Comment must be between 10 and 1000 characters")]
    public string Comment { get; set; } = string.Empty;
}

public class UpdateReviewDto
{
    [Required]
    [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5 stars")]
    public int Rating { get; set; }

    [Required]
    [StringLength(1000, MinimumLength = 10, ErrorMessage = "Comment must be between 10 and 1000 characters")]
    public string Comment { get; set; } = string.Empty;
}
