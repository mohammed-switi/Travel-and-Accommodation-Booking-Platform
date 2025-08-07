using Final_Project.DTOs.Requests;
using Final_Project.DTOs.Responses;
using Final_Project.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Final_Project.Controllers;

[ApiController]
[Route("api/reviews")]
[Authorize]
public class ReviewController(IReviewService reviewService, ILogger<ReviewController> logger) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateReview([FromBody] CreateReviewDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var review = await reviewService.CreateReviewAsync(dto, User);
            return CreatedAtAction(nameof(GetReview), new { id = review.Id }, review);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning("Invalid request for creating review: {Message}", ex.Message);
            return BadRequest(new { Message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning("Unauthorized review creation attempt: {Message}", ex.Message);
            return Forbid(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning("Invalid operation for creating review: {Message}", ex.Message);
            return Conflict(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating review for hotel {HotelId}", dto.HotelId);
            return StatusCode(500, new { Message = "An error occurred while creating the review." });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateReview(int id, [FromBody] UpdateReviewDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var review = await reviewService.UpdateReviewAsync(id, dto, User);
            return Ok(review);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning("Invalid request for updating review {ReviewId}: {Message}", id, ex.Message);
            return BadRequest(new { Message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning("Unauthorized review update attempt for review {ReviewId}: {Message}", id, ex.Message);
            return Forbid(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating review {ReviewId}", id);
            return StatusCode(500, new { Message = "An error occurred while updating the review." });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteReview(int id)
    {
        try
        {
            var success = await reviewService.DeleteReviewAsync(id, User);
            return success ? NoContent() : NotFound(new { Message = "Review not found." });
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning("Unauthorized review deletion attempt for review {ReviewId}: {Message}", id, ex.Message);
            return Forbid(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting review {ReviewId}", id);
            return StatusCode(500, new { Message = "An error occurred while deleting the review." });
        }
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetReview(int id)
    {
        try
        {
            var review = await reviewService.GetReviewByIdAsync(id);
            return review != null ? Ok(review) : NotFound(new { Message = "Review not found." });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving review {ReviewId}", id);
            return StatusCode(500, new { Message = "An error occurred while retrieving the review." });
        }
    }

    [HttpGet("hotel/{hotelId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetHotelReviews(int hotelId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            var reviews = await reviewService.GetHotelReviewsAsync(hotelId, page, pageSize);
            
            var response = new
            {
                Reviews = reviews,
                Pagination = new
                {
                    Page = page,
                    PageSize = pageSize,
                    HasMore = reviews.Count == pageSize
                }
            };

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning("Invalid request for hotel reviews: {Message}", ex.Message);
            return BadRequest(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving reviews for hotel {HotelId}", hotelId);
            return StatusCode(500, new { Message = "An error occurred while retrieving hotel reviews." });
        }
    }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetUserReviews(int userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            // Users can only view their own reviews, admins can view any user's reviews
            var currentUserId = GetCurrentUserId();
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            
            if (userId != currentUserId && userRole != "Admin")
            {
                return Forbid("You can only view your own reviews.");
            }

            var reviews = await reviewService.GetUserReviewsAsync(userId, page, pageSize);
            
            var response = new
            {
                Reviews = reviews,
                Pagination = new
                {
                    Page = page,
                    PageSize = pageSize,
                    HasMore = reviews.Count == pageSize
                }
            };

            return Ok(response);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            logger.LogWarning("Invalid pagination parameters: {Message}", ex.Message);
            return BadRequest(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving reviews for user {UserId}", userId);
            return StatusCode(500, new { Message = "An error occurred while retrieving user reviews." });
        }
    }

    [HttpGet("user/my-reviews")]
    public async Task<IActionResult> GetMyReviews([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            var userId = GetCurrentUserId();
            var reviews = await reviewService.GetUserReviewsAsync(userId, page, pageSize);
            
            var response = new
            {
                Reviews = reviews,
                Pagination = new
                {
                    Page = page,
                    PageSize = pageSize,
                    HasMore = reviews.Count == pageSize
                }
            };

            return Ok(response);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            logger.LogWarning("Invalid pagination parameters: {Message}", ex.Message);
            return BadRequest(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving reviews for current user");
            return StatusCode(500, new { Message = "An error occurred while retrieving your reviews." });
        }
    }

    [HttpGet("hotel/{hotelId}/summary")]
    [AllowAnonymous]
    public async Task<IActionResult> GetHotelReviewSummary(int hotelId)
    {
        try
        {
            var summary = await reviewService.GetHotelReviewSummaryAsync(hotelId);
            return Ok(summary);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning("Invalid request for hotel review summary: {Message}", ex.Message);
            return BadRequest(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving review summary for hotel {HotelId}", hotelId);
            return StatusCode(500, new { Message = "An error occurred while retrieving the review summary." });
        }
    }

    [HttpGet("hotel/{hotelId}/can-review")]
    public async Task<IActionResult> CanReviewHotel(int hotelId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var canReview = await reviewService.CanUserReviewHotelAsync(userId, hotelId);
            
            return Ok(new { CanReview = canReview });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking if user can review hotel {HotelId}", hotelId);
            return StatusCode(500, new { Message = "An error occurred while checking review eligibility." });
        }
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid user authentication.");
        }
        return userId;
    }
}
