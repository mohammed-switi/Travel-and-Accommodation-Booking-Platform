using Final_Project.DTOs.Requests;
using Final_Project.DTOs.Responses;
using System.Security.Claims;

namespace Final_Project.Interfaces;

public interface IReviewService
{
    Task<ReviewResponseDto> CreateReviewAsync(CreateReviewDto dto, ClaimsPrincipal user);
    Task<ReviewResponseDto> UpdateReviewAsync(int reviewId, UpdateReviewDto dto, ClaimsPrincipal user);
    Task<bool> DeleteReviewAsync(int reviewId, ClaimsPrincipal user);
    Task<ReviewResponseDto?> GetReviewByIdAsync(int reviewId);
    Task<List<ReviewResponseDto>> GetHotelReviewsAsync(int hotelId, int page = 1, int pageSize = 10);
    Task<List<ReviewResponseDto>> GetUserReviewsAsync(int userId, int page = 1, int pageSize = 10);
    Task<ReviewSummaryDto> GetHotelReviewSummaryAsync(int hotelId);
    Task<bool> CanUserReviewHotelAsync(int userId, int hotelId);
}
