using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.Application.Interfaces;
using AcxiomCRM.Application.ViewModels;
using AcxiomCRM.Domain.Entities;
using AcxiomCRM.Domain.Enums;

namespace AcxiomCRM.Web.Controllers;

public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditService _auditService;

    public AccountController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IAuditService auditService)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _auditService = auditService;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Dashboard");
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        ViewData["ReturnUrl"] = model.ReturnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            await _auditService.LogSecurityEventAsync("FailedLogin", $"Login failed: user '{model.Email}' not found.", "Failed", null, model.Email);
            ModelState.AddModelError(string.Empty, "Invalid login credentials.");
            return View(model);
        }

        if (!user.IsActive)
        {
            await _auditService.LogSecurityEventAsync("FailedLogin", $"Login blocked: user '{model.Email}' account is deactivated.", "Failed", user.Id, user.Email);
            ModelState.AddModelError(string.Empty, "Your account has been deactivated. Please contact an administrator.");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(user.UserName!, model.Password, model.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            await _auditService.LogSecurityEventAsync("Login", $"User '{user.Email}' logged in successfully.", "Success", user.Id, user.Email);

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }
            return RedirectToAction("Index", "Dashboard");
        }

        if (result.IsLockedOut)
        {
            await _auditService.LogSecurityEventAsync("AccountLockout", $"User '{user.Email}' account locked out due to multiple failed login attempts.", "Failed", user.Id, user.Email);
            ModelState.AddModelError(string.Empty, "This account has been temporarily locked out due to multiple failed login attempts. Please try again later or contact an administrator.");
            return View(model);
        }

        await _auditService.LogSecurityEventAsync("FailedLogin", $"Login failed: incorrect password for user '{model.Email}'.", "Failed", user.Id, user.Email);
        ModelState.AddModelError(string.Empty, "Invalid login credentials.");
        return View(model);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var userName = User.Identity?.Name;

        await _signInManager.SignOutAsync();
        await _auditService.LogSecurityEventAsync("Logout", $"User '{userName}' logged out.", "Success", userId, userName);

        return RedirectToAction("Login", "Account");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register()
    {
        return View(new RegisterViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var existingUser = await _userManager.FindByEmailAsync(model.Email);
        if (existingUser != null)
        {
            ModelState.AddModelError("Email", "An account with this email address already exists.");
            return View(model);
        }

        var assignedRole = AppRoles.AllRoles.Contains(model.Role) ? model.Role : AppRoles.SalesExecutive;

        var user = new ApplicationUser
        {
            UserName = model.Email.Trim().ToLower(),
            Email = model.Email.Trim().ToLower(),
            FullName = model.FullName.Trim(),
            RoleName = assignedRole,
            EmailConfirmed = true,
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, assignedRole);
            await _auditService.LogSecurityEventAsync("UserRegistration", $"New user '{user.Email}' registered with role {assignedRole}.", "Success", user.Id, user.Email);

            await _signInManager.SignInAsync(user, isPersistent: false);
            return RedirectToAction("Index", "Dashboard");
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(model);
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }
}
