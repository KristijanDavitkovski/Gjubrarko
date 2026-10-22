using Gjubrarko.Api.Auth;
using Gjubrarko.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Gjubrarko.Api.Controllers;

public sealed record RegisterRequest(string Email, string Password, string DisplayName, string? PreferredLanguage, string? CaptchaToken);
public sealed record LoginRequest(string Email, string Password);
public sealed record AuthResponse(string AccessToken, DateTimeOffset ExpiresAt, string DisplayName, IList<string> Roles);

[ApiController]
[Route("api/v1/auth")]
[EnableRateLimiting("auth")]
public class AuthController(UserManager<AppUser> users, SignInManager<AppUser> signIn, JwtTokenService tokens, TurnstileVerifier captcha) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken ct)
    {
        if (!await captcha.VerifyAsync(request.CaptchaToken, HttpContext.Connection.RemoteIpAddress?.ToString(), ct))
            return Problem(statusCode: 400, detail: "Captcha check failed.");

        var user = new AppUser
        {
            UserName = request.Email, Email = request.Email,
            DisplayName = request.DisplayName.Trim(), PreferredLanguage = request.PreferredLanguage ?? "mk",
        };
        var result = await users.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return ValidationProblem(new ValidationProblemDetails(result.Errors.GroupBy(e => e.Code).ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray())));

        await users.AddToRoleAsync(user, Roles.Citizen);
        return await Issue(user);
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var user = await users.FindByEmailAsync(request.Email);
        if (user is null) return Unauthorized();
        var check = await signIn.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!check.Succeeded) return Unauthorized();
        return await Issue(user);
    }

    private async Task<AuthResponse> Issue(AppUser user)
    {
        var roles = await users.GetRolesAsync(user);
        var (token, expires) = tokens.Create(user, roles);
        return new AuthResponse(token, expires, user.DisplayName, roles);
    }
}
