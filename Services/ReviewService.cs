using Final_Project.Data;
using Final_Project.DTOs.Requests;
using Final_Project.DTOs.Responses;
using Final_Project.Interfaces;
using Final_Project.Models;
using Final_Project.Constants;
using Final_Project.Enums;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Final_Project.Services;

public class ReviewService(AppDbContext context, ILogger<ReviewService> logger) : IReviewService
{
    public async Task<ReviewResponseDto> CreateReviewAsync(CreateReviewDto dto, ClaimsPrincipal user)
    {
        var userId = GetUserIdFromClaims(user);
        
        // Validate that hotel exists and is active
        var hotel = await context.Hotels.FirstOrDefaultAsync(h => h.Id == dto.HotelId && h.IsActive);
        if (hotel == null)
        {
            logger.LogWarning("Attempt to review non-existent or inactive hotel {HotelId}", dto.HotelId);
            throw new ArgumentException("Hotel not found or is inactive.");
        }

        // Check if user can review this hotel (has stayed there)
        if (!await CanUserReviewHotelAsync(userId, dto.HotelId))
        {
            logger.LogWarning("User {UserId} attempted to review hotel {HotelId} without staying there", userId, dto.HotelId);
            throw new UnauthorizedAccessException("You can only review hotels where you have completed a stay.");
        }

        // Check if user has already reviewed this hotel
        var existingReview = await context.Reviews
            .FirstOrDefaultAsync(r => r.UserId == userId && r.HotelId == dto.HotelId);
        
        if (existingReview != null)
        {
            logger.LogWarning("User {UserId} attempted to create duplicate review for hotel {HotelId}", userId, dto.HotelId);
            throw new InvalidOperationException("You have already reviewed this hotel. Please update your existing review instead.");
        }

        var review = new Review
        {
            HotelId = dto.HotelId,
            UserId = userId,
            Rating = dto.Rating,
            Comment = dto.Comment.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        context.Reviews.Add(review);
        await context.SaveChangesAsync();

        logger.LogInformation("User {UserId} created review {ReviewId} for hotel {HotelId}", userId, review.Id, dto.HotelId);

        return await GetReviewResponseDtoAsync(review.Id);
    }

    public async Task<ReviewResponseDto> UpdateReviewAsync(int reviewId, UpdateReviewDto dto, ClaimsPrincipal user)
    {
        var userId = GetUserIdFromClaims(user);
        var userRole = user.FindFirst(ClaimTypes.Role)?.Value;

        var review = await context.Reviews
            .Include(r => r.Hotel)
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == reviewId);

        if (review == null)
        {
            logger.LogWarning("Attempt to update non-existent review {ReviewId}", reviewId);
            throw new ArgumentException("Review not found.");
        }

        // Only the review author or admin can update the review
        if (review.UserId != userId && userRole != UserRoles.Admin)
        {
            logger.LogWarning("User {UserId} attempted to update review {ReviewId} without permission", userId, reviewId);
            throw new UnauthorizedAccessException("You can only update your own reviews.");
        }

        review.Rating = dto.Rating;
        review.Comment = dto.Comment.Trim();

        await context.SaveChangesAsync();

        logger.LogInformation("Review {ReviewId} updated by user {UserId}", reviewId, userId);

        return await GetReviewResponseDtoAsync(reviewId);
    }

    public async Task<bool> DeleteReviewAsync(int reviewId, ClaimsPrincipal user)
    {
        var userId = GetUserIdFromClaims(user);
        var userRole = user.FindFirst(ClaimTypes.Role)?.Value;

        var review = await context.Reviews.FirstOrDefaultAsync(r => r.Id == reviewId);
        if (review == null)
        {
            logger.LogWarning("Attempt to delete non-existent review {ReviewId}", reviewId);
            return false;
        }

        // Only the review author or admin can delete the review
        if (review.UserId != userId && userRole != UserRoles.Admin)
        {
            logger.LogWarning("User {UserId} attempted to delete review {ReviewId} without permission", userId, reviewId);
            throw new UnauthorizedAccessException("You can only delete your own reviews.");
        }

        context.Reviews.Remove(review);
        await context.SaveChangesAsync();

        logger.LogInformation("Review {ReviewId} deleted by user {UserId}", reviewId, userId);
        return true;
    }

    public async Task<ReviewResponseDto?> GetReviewByIdAsync(int reviewId)
    {
        var review = await context.Reviews
            .Include(r => r.Hotel)
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == reviewId);

        if (review == null)
        {
            logger.LogWarning("Review {ReviewId} not found", reviewId);
            return null;
        }

        return MapToResponseDto(review);
    }

    public async Task<List<ReviewResponseDto>> GetHotelReviewsAsync(int hotelId, int page = 1, int pageSize = 10)
    {
        if (page < 1) throw new ArgumentOutOfRangeException(nameof(page));
        if (pageSize < 1 || pageSize > 50) throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be between 1 and 50");

        // Verify hotel exists
        var hotelExists = await context.Hotels.AnyAsync(h => h.Id == hotelId && h.IsActive);
        if (!hotelExists)
        {
            logger.LogWarning("Attempt to get reviews for non-existent hotel {HotelId}", hotelId);
            throw new ArgumentException("Hotel not found.");
        }

        var reviews = await context.Reviews
            .Where(r => r.HotelId == hotelId)
            .Include(r => r.Hotel)
            .Include(r => r.User)
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        logger.LogInformation("Retrieved {Count} reviews for hotel {HotelId}, page {Page}", reviews.Count, hotelId, page);

        return reviews.Select(MapToResponseDto).ToList();
    }

    public async Task<List<ReviewResponseDto>> GetUserReviewsAsync(int userId, int page = 1, int pageSize = 10)
    {
        if (page < 1) throw new ArgumentOutOfRangeException(nameof(page));
        if (pageSize < 1 || pageSize > 50) throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be between 1 and 50");

        var reviews = await context.Reviews
            .Where(r => r.UserId == userId)
            .Include(r => r.Hotel)
            .Include(r => r.User)
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        logger.LogInformation("Retrieved {Count} reviews for user {UserId}, page {Page}", reviews.Count, userId, page);

        return reviews.Select(MapToResponseDto).ToList();
    }

    public async Task<ReviewSummaryDto> GetHotelReviewSummaryAsync(int hotelId)
    {
        // Verify hotel exists
        var hotelExists = await context.Hotels.AnyAsync(h => h.Id == hotelId && h.IsActive);
        if (!hotelExists)
        {
            logger.LogWarning("Attempt to get review summary for non-existent hotel {HotelId}", hotelId);
            throw new ArgumentException("Hotel not found.");
        }

        var reviews = await context.Reviews
            .Where(r => r.HotelId == hotelId)
            .Select(r => r.Rating)
            .ToListAsync();

        if (!reviews.Any())
        {
            return new ReviewSummaryDto
            {
                TotalReviews = 0,
                AverageRating = 0,
                RatingDistribution = new int[5]
            };
        }

        var ratingDistribution = new int[5];
        foreach (var rating in reviews)
        {
            ratingDistribution[rating - 1]++; // Convert 1-5 rating to 0-4 index
        }

        return new ReviewSummaryDto
        {
            TotalReviews = reviews.Count,
            AverageRating = Math.Round(reviews.Average(), 2),
            RatingDistribution = ratingDistribution
        };
    }

    public async Task<bool> CanUserReviewHotelAsync(int userId, int hotelId)
    {
        // User can review if they have completed at least one stay at the hotel
        var hasCompletedStay = await context.BookingItems
            .AnyAsync(bi => bi.Room.HotelId == hotelId &&
                           bi.Booking.UserId == userId &&
                           bi.Booking.Status == BookingStatus.Approved &&
                           bi.CheckOutDate < DateTime.UtcNow); // Past checkout date

        return hasCompletedStay;
    }

    private async Task<ReviewResponseDto> GetReviewResponseDtoAsync(int reviewId)
    {
        var review = await context.Reviews
            .Include(r => r.Hotel)
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == reviewId);

        if (review == null)
            throw new InvalidOperationException("Review not found after creation/update.");

        return MapToResponseDto(review);
    }

    private static ReviewResponseDto MapToResponseDto(Review review)
    {
        return new ReviewResponseDto
        {
            Id = review.Id,
            HotelId = review.HotelId,
            HotelName = review.Hotel?.Name ?? "Unknown Hotel",
            UserId = review.UserId,
            UserName = review.User?.FullName ?? "Anonymous",
            Rating = review.Rating,
            Comment = review.Comment ?? string.Empty,
            CreatedAt = review.CreatedAt
        };
    }

    private static int GetUserIdFromClaims(ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid user authentication.");
        }
        return userId;
    }
}
