using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using AcxiomCRM.Application.Interfaces;
using AcxiomCRM.Application.ViewModels;
using AcxiomCRM.Domain.Entities;
using AcxiomCRM.Domain.Enums;

namespace AcxiomCRM.Web.Controllers;

[Authorize]
public class LeadsController : Controller
{
    private readonly ILeadService _leadService;
    private readonly IUserService _userService;
    private readonly UserManager<ApplicationUser> _userManager;

    public LeadsController(
        ILeadService leadService,
        IUserService userService,
        UserManager<ApplicationUser> userManager)
    {
        _leadService = leadService;
        _userService = userService;
        _userManager = userManager;
    }

    private async Task<(string? UserId, string Role)> GetCurrentUserScopeAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await _userManager.GetUserAsync(User);
        var role = user?.RoleName ?? AppRoles.SalesExecutive;
        return (userId, role);
    }

    private async Task PopulateExecutivesDropdownAsync(string? selectedId = null)
    {
        var reps = await _userService.GetActiveSalesExecutivesAsync();
        ViewBag.Executives = new SelectList(reps, "Id", "FullName", selectedId);
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? search, LeadStatus? status, string? source, string? assignedUserId, int page = 1)
    {
        var (userId, role) = await GetCurrentUserScopeAsync();
        await PopulateExecutivesDropdownAsync(assignedUserId);
        var model = await _leadService.GetLeadsAsync(userId, role, search, status, source, assignedUserId, page);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var (userId, role) = await GetCurrentUserScopeAsync();
        var model = await _leadService.GetLeadDetailsAsync(id, userId, role);
        if (model == null)
        {
            return NotFound("Lead not found or access denied.");
        }
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var (userId, role) = await GetCurrentUserScopeAsync();
        await PopulateExecutivesDropdownAsync(role == AppRoles.SalesExecutive ? userId : null);
        return View(new LeadCreateEditViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(LeadCreateEditViewModel model)
    {
        var (userId, role) = await GetCurrentUserScopeAsync();

        if (!ModelState.IsValid)
        {
            await PopulateExecutivesDropdownAsync(model.AssignedToUserId);
            return View(model);
        }

        var result = await _leadService.CreateLeadAsync(model, userId, role);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            await PopulateExecutivesDropdownAsync(model.AssignedToUserId);
            return View(model);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Details), new { id = result.LeadId });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var (userId, role) = await GetCurrentUserScopeAsync();
        var model = await _leadService.GetLeadForEditAsync(id, userId, role);
        if (model == null)
        {
            return NotFound("Lead not found or access denied.");
        }

        await PopulateExecutivesDropdownAsync(model.AssignedToUserId);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(LeadCreateEditViewModel model)
    {
        var (userId, role) = await GetCurrentUserScopeAsync();

        if (!ModelState.IsValid)
        {
            await PopulateExecutivesDropdownAsync(model.AssignedToUserId);
            return View(model);
        }

        var result = await _leadService.UpdateLeadAsync(model, userId, role);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            await PopulateExecutivesDropdownAsync(model.AssignedToUserId);
            return View(model);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Details), new { id = model.LeadId });
    }

    [HttpGet]
    public async Task<IActionResult> Convert(int id)
    {
        var (userId, role) = await GetCurrentUserScopeAsync();
        var model = await _leadService.GetLeadForConversionAsync(id, userId, role);
        if (model == null)
        {
            return NotFound("Lead not found, already converted, or access denied.");
        }

        await PopulateExecutivesDropdownAsync(model.AssignedToUserId);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Convert(LeadConversionViewModel model)
    {
        var (userId, role) = await GetCurrentUserScopeAsync();

        if (!ModelState.IsValid)
        {
            await PopulateExecutivesDropdownAsync(model.AssignedToUserId);
            return View(model);
        }

        var result = await _leadService.ConvertLeadAsync(model, userId, role);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            await PopulateExecutivesDropdownAsync(model.AssignedToUserId);
            return View(model);
        }

        TempData["SuccessMessage"] = result.Message;
        if (result.CustomerId.HasValue)
        {
            return RedirectToAction("Details", "Customers", new { id = result.CustomerId.Value });
        }
        return RedirectToAction(nameof(Details), new { id = model.LeadId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var (userId, role) = await GetCurrentUserScopeAsync();
        var result = await _leadService.DeleteLeadAsync(id, userId, role);

        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.Message;
            return RedirectToAction(nameof(Details), new { id });
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }
}
