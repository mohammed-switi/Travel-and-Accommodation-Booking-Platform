namespace Final_Project.DTOs.Responses;

public class ReviewResponseDto
{
    public int Id { get; set; }
    public int HotelId { get; set; }
    public string HotelName { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class ReviewSummaryDto
{
    public int TotalReviews { get; set; }
    public double AverageRating { get; set; }
    public int[] RatingDistribution { get; set; } = new int[5]; // Index 0 = 1-star, Index 4 = 5-star
}
