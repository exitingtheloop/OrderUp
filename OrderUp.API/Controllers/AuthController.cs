using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OrderUp.Shared.Contracts.Auth;

namespace OrderUp.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly UserManager<IdentityUser> _userManager;

    public AuthController(
        SignInManager<IdentityUser> signInManager,
        UserManager<IdentityUser> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    /// <summary>
    /// Logs in a user with email and password.
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<AuthStatusResponse>> Login(LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            return Unauthorized(new { error = "Invalid email or password." });
        }

        var result = await _signInManager.PasswordSignInAsync(
            user,
            request.Password,
            isPersistent: request.RememberMe,
            lockoutOnFailure: true);

        if (!result.Succeeded)
        {
            if (result.IsLockedOut)
            {
                return Unauthorized(new { error = "Account is locked. Try again later." });
            }
            return Unauthorized(new { error = "Invalid email or password." });
        }

        var roles = await _userManager.GetRolesAsync(user);

        return Ok(new AuthStatusResponse(
            IsAuthenticated: true,
            Email: user.Email,
            Roles: roles.ToList()
        ));
    }

    /// <summary>
    /// Logs out the current user.
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return Ok(new { message = "Logged out successfully." });
    }

    /// <summary>
    /// Gets the current authentication status.
    /// </summary>
    [HttpGet("status")]
    public async Task<ActionResult<AuthStatusResponse>> GetStatus()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return Ok(new AuthStatusResponse(
                IsAuthenticated: false,
                Email: null,
                Roles: []
            ));
        }

        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Ok(new AuthStatusResponse(
                IsAuthenticated: false,
                Email: null,
                Roles: []
            ));
        }

        var roles = await _userManager.GetRolesAsync(user);

        return Ok(new AuthStatusResponse(
            IsAuthenticated: true,
            Email: user.Email,
            Roles: roles.ToList()
        ));
    }
}
