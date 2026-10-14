using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using AcxiomCRM.Application.Interfaces;
using AcxiomCRM.Application.ViewModels;
using AcxiomCRM.Domain.Enums;

namespace AcxiomCRM.Web.Controllers;

[Authorize(Roles = AppRoles.Admin)]
public class UsersController : Controller
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    private void PopulateRolesDropdown(string? selectedRole = null)
    {
        ViewBag.Roles = new SelectList(AppRoles.AllRoles, selectedRole);
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? roleFilter, string? search)
    {
        PopulateRolesDropdown(roleFilter);
        var model = await _userService.GetUsersAsync(roleFilter, search);
        return View(model);
    }

    [HttpGet]
    public IActionResult Create()
    {
        PopulateRolesDropdown(AppRoles.SalesExecutive);
        return View(new CreateUserViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateUserViewModel model)
    {
        if (!ModelState.IsValid)
        {
            PopulateRolesDropdown(model.Role);
            return View(model);
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var result = await _userService.CreateUserAsync(model, currentUserId);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            PopulateRolesDropdown(model.Role);
            return View(model);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var model = await _userService.GetUserForEditAsync(id);
        if (model == null)
        {
            return NotFound("User not found.");
        }

        PopulateRolesDropdown(model.Role);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditUserViewModel model)
    {
        if (!ModelState.IsValid)
        {
            PopulateRolesDropdown(model.Role);
            return View(model);
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var result = await _userService.UpdateUserAsync(model, currentUserId);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            PopulateRolesDropdown(model.Role);
            return View(model);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(string id)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var result = await _userService.ToggleUserStatusAsync(id, currentUserId);

        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.Message;
        }
        else
        {
            TempData["SuccessMessage"] = result.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> ResetPassword(string id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        if (user == null)
        {
            return NotFound("User not found.");
        }

        var model = new ResetPasswordViewModel
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var result = await _userService.ResetPasswordAsync(model, currentUserId);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View(model);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unlock(string id)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var result = await _userService.UnlockUserAsync(id, currentUserId);

        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.Message;
        }
        else
        {
            TempData["SuccessMessage"] = result.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}
