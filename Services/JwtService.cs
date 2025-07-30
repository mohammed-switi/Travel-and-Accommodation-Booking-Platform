using Final_Project.Interfaces;
using System.Security.Claims;

namespace Final_Project.Services;

public class JwtService : IJwtService
{
    public int GetUserIdFromClaims(ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim))
        {
            throw new InvalidOperationException("User ID not found in claims");
        }
        
        if (!int.TryParse(userIdClaim, out int userId))
        {
            throw new InvalidOperationException("Invalid user ID format in claims");
        }
        
        return userId;
    }

    public string GetUserRoleFromClaims(ClaimsPrincipal user)
    {
        var userRole = user.FindFirstValue(ClaimTypes.Role);
        if (string.IsNullOrEmpty(userRole))
        {
            throw new InvalidOperationException("User role not found in claims");
        }
        
        return userRole;
    }

    public (int UserId, string UserRole) GetUserInfoFromClaims(ClaimsPrincipal user)
    {
        return (GetUserIdFromClaims(user), GetUserRoleFromClaims(user));
    }
}
