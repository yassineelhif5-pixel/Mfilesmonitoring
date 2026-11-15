using MFCMP.Api.Domain;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace MFCMP.Api.Controllers;

[ApiController]
[Route("api/auth")]
[AutoValidateAntiforgeryToken]
public sealed class AuthController(
    IAntiforgery antiforgery,
    SignInManager<PortalUser> signInManager,
    UserManager<PortalUser> userManager) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("csrf")]
    public IActionResult GetCsrf()
    {
        var tokens = antiforgery.GetAndStoreTokens(HttpContext);
        return Ok(new { requestToken = tokens.RequestToken });
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var result = await signInManager.PasswordSignInAsync(request.Username, request.Password, true, true);
        if (!result.Succeeded)
            return Unauthorized(new { message = "Invalid credentials or account temporarily locked." });

        var user = await userManager.FindByNameAsync(request.Username);
        if (user is null)
        {
            await signInManager.SignOutAsync();
            return Unauthorized();
        }
        return Ok(new { username = user.UserName, mustChangePassword = user.MustChangePassword });
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var user = await userManager.GetUserAsync(User);
        return user is null ? Unauthorized() : Ok(new { username = user.UserName, mustChangePassword = user.MustChangePassword });
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
    {
        if (request.NewPassword != request.ConfirmPassword)
            return BadRequest(new { message = "Password confirmation does not match." });
        if (!IsStrongPassword(request.NewPassword))
            return BadRequest(new { message = "Use at least 12 characters including uppercase, lowercase, a digit and a symbol." });

        var user = await userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();
        var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        user.MustChangePassword = false;
        await userManager.UpdateAsync(user);
        await signInManager.RefreshSignInAsync(user);
        return Ok(new { mustChangePassword = false });
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return NoContent();
    }

    private static bool IsStrongPassword(string value) =>
        value.Length >= 12 &&
        value.Any(char.IsUpper) &&
        value.Any(char.IsLower) &&
        value.Any(char.IsDigit) &&
        value.Any(c => !char.IsLetterOrDigit(c));

    public sealed record LoginRequest(string Username, string Password);
    public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword, string ConfirmPassword);
}
