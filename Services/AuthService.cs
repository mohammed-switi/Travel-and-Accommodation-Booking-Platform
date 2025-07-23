using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Final_Project.Data;
using Final_Project.DTOs;
using Final_Project.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Final_Project.Services;

public class AuthService(
    AppDbContext context,
    IPasswordHasher<User> passwordHasher,
    IConfiguration configuration,
    ILogger<AuthService> logger)
    : IAuthService
{
    public async Task<User> RegisterAsync(RegisterDto newUser)
    {
        if (await context.Users.AnyAsync(user => user.Email == newUser.Email.ToLower()))
            throw new InvalidOperationException("Email already exists.");
        logger.LogInformation("Registering new user with email: {Email}", newUser.Email);
        var user = new User
        {
            FullName = newUser.FullName,
            Email = newUser.Email.ToLower(),
            Role = "User"
        };

        user.PasswordHash = passwordHasher.HashPassword(user, newUser.Password);

        context.Users.Add(user);
        await context.SaveChangesAsync();
        logger.LogInformation("User registered successfully with ID: {UserId}", user.Id);
        return user;
    }

    public async Task<string> LoginAsync(LoginDto loginDto)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Email == loginDto.Email.ToLower());
        if (user == null)
            throw new UnauthorizedAccessException("Invalid email or password.");

        var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, loginDto.Password);
        if (result == PasswordVerificationResult.Failed)
            throw new UnauthorizedAccessException("Invalid email or password.");

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };


        var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(configuration["Jwt:Key"] ??
                                                                              throw new InvalidOperationException(
                                                                                  "JWT Key is not configured.")));

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.Now.AddMinutes(int.Parse(configuration["Jwt:ExpiryMinutes"]?? "60")),
            signingCredentials: creds
        );


        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task ForgotPasswordAsync(ForgotPasswordDto dto)
    {
        var user = context.Users.FirstOrDefault(u => u.Email == dto.Email.ToLower());
        if (user == null)
            throw new InvalidOperationException("User not found.");


        var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray());

        user.PasswordResetToken = token;
        user.PasswordResetTokenExpiration = DateTime.UtcNow.AddHours(1);

        await context.SaveChangesAsync();

        // Here you would typically send the token to the user's email.
        logger.LogInformation("Password reset token generated for user with email: {Email}", dto.Email);
    }


    public async Task ResetPasswordAsync(ResetPasswordDto dto)
    {
        var user = context.Users.FirstOrDefault(u => u.Email == dto.Email);
        if (user == null)
            throw new Exception("Invalid email or token.");

        if (user.PasswordResetToken != dto.Token || user.PasswordResetTokenExpiration < DateTime.UtcNow)
            throw new Exception("Invalid or expired token.");

        user.PasswordHash = passwordHasher.HashPassword(user, dto.NewPassword);

        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiration = null;

        await context.SaveChangesAsync();
    }
}