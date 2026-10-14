using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.Application.DTOs;
using AcxiomCRM.Application.Interfaces;
using AcxiomCRM.Domain.Entities;

namespace AcxiomCRM.Web.Controllers.Api;

[ApiController]
[Route("api/auth")]
public class AuthApiController : ControllerBase
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditService _auditService;

    public AuthApiController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IAuditService auditService)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _auditService = auditService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "Invalid login payload." });
        }

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null || !user.IsActive)
        {
            await _auditService.LogSecurityEventAsync("ApiFailedLogin", $"API login failed for {request.Email}", "Failed", user?.Id, request.Email);
            return Unauthorized(new { success = false, message = "Invalid email or password." });
        }

        var result = await _signInManager.PasswordSignInAsync(user.UserName!, request.Password, false, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            await _auditService.LogSecurityEventAsync("ApiLogin", $"API user {user.Email} logged in successfully.", "Success", user.Id, user.Email);
            var roles = await _userManager.GetRolesAsync(user);

            return Ok(new LoginResponseDto
            {
                Success = true,
                Message = "Authentication successful.",
                UserId = user.Id,
                UserName = user.UserName,
                FullName = user.FullName,
                Role = roles.FirstOrDefault() ?? user.RoleName
            });
        }

        if (result.IsLockedOut)
        {
            await _auditService.LogSecurityEventAsync("ApiAccountLockout", $"API login blocked: account locked for {user.Email}.", "Failed", user.Id, user.Email);
            return StatusCode(403, new { success = false, message = "Account is temporarily locked out due to multiple failed login attempts." });
        }

        await _auditService.LogSecurityEventAsync("ApiFailedLogin", $"API login failed for {user.Email}.", "Failed", user.Id, user.Email);
        return Unauthorized(new { success = false, message = "Invalid email or password." });
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var userName = User.Identity?.Name;
        await _signInManager.SignOutAsync();
        await _auditService.LogSecurityEventAsync("ApiLogout", $"API user {userName} logged out.", "Success", null, userName);

        return Ok(new LogoutResponseDto
        {
            Success = true,
            Message = "Logged out successfully."
        });
    }
}
