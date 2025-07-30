using System.Security.Claims;

namespace Final_Project.Interfaces;

public interface IJwtService
{
    /// <summary>
    /// Extracts user ID from the current user claims
    /// </summary>
    /// <param name="user">ClaimsPrincipal from controller context</param>
    /// <returns>User ID as integer</returns>
    /// <exception cref="InvalidOperationException">Thrown when user ID is not found in claims</exception>
    int GetUserIdFromClaims(ClaimsPrincipal user);
    
    /// <summary>
    /// Extracts user role from the current user claims
    /// </summary>
    /// <param name="user">ClaimsPrincipal from controller context</param>
    /// <returns>User role as string</returns>
    /// <exception cref="InvalidOperationException">Thrown when user role is not found in claims</exception>
    string GetUserRoleFromClaims(ClaimsPrincipal user);
    
    /// <summary>
    /// Extracts both user ID and role from claims in one call
    /// </summary>
    /// <param name="user">ClaimsPrincipal from controller context</param>
    /// <returns>Tuple containing user ID and role</returns>
    /// <exception cref="InvalidOperationException">Thrown when user ID or role is not found in claims</exception>
    (int UserId, string UserRole) GetUserInfoFromClaims(ClaimsPrincipal user);
}
