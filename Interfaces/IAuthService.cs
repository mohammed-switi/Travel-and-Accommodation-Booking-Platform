using System.Security.Claims;
using Final_Project.DTOs;
using Final_Project.Models;

namespace Final_Project.Services;

public interface IAuthService
{
    Task<User> RegisterAsync(RegisterDto newUser);
    Task<string> LoginAsync(LoginDto loginDto);

    Task ForgotPasswordAsync(ForgotPasswordDto dto);

    Task LogoutAsync(ClaimsPrincipal? user);
    Task ResetPasswordAsync(ResetPasswordDto dto);
}