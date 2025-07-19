using Final_Project.DTOs;
using Final_Project.Services;
using Microsoft.AspNetCore.Mvc;

namespace Final_Project.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
    {
        try
        {
            var newUser = await authService.RegisterAsync(registerDto);
            return Ok(new { message = "User Registered successfully", userId = newUser.Id });
        }
        catch (Exception e)
        {
            return BadRequest(new { message = e.Message });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
    {
        if (!ModelState.IsValid)
            return BadRequest("Invalid login data.");

        try
        {
            var token = await authService.LoginAsync(loginDto);
            return Ok(new { token });
        }
        catch (Exception e)
        {
            return Unauthorized(new { message = e.Message });
        }
    }


    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            await authService.ResetPasswordAsync(dto);
            return Ok(new { Message = "Password reset successful." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }
}