using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Distributed;
using ILogger = Serilog.ILogger;

namespace Final_Project.Middlewares;

public class JwtBlacklistMiddleware(
    RequestDelegate next,
    IDistributedCache cache,
    ILogger<JwtBlacklistMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        ClaimsPrincipal user = context.User;
        var endpoint = context.GetEndpoint();
        bool isProtectedEndpoint = endpoint?.Metadata?.GetMetadata<AuthorizeAttribute>() != null;
        
        if (isProtectedEndpoint && user?.Identity?.IsAuthenticated == true)
        {
            string? jti = user.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;

            if (!string.IsNullOrEmpty(jti))
            {
                string cacheKey = $"JwtID_{jti}";
                var blacklisted = await cache.GetStringAsync(cacheKey);

                if (!string.IsNullOrEmpty(blacklisted))
                {
                    logger.LogWarning("Rejected request due to blacklisted JWT ID: {Jti}", jti);
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsync("Unauthorized: Token has been revoked.");
                    return;
                }
            }
            else
            {
                logger.LogWarning("No JTI claim found in token.");
            }
        }

        await next(context);
    }
}