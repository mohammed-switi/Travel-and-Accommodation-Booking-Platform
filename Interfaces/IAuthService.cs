using Final_Project.DTOs;
using Final_Project.Models;

namespace Final_Project.Services;

public interface IAuthService
{
    Task<User> RegisterAsync(RegisterDto newUser);
    Task<string> LoginAsync(LoginDto loginDto);
}