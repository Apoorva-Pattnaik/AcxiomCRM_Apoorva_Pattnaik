using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Application.Interfaces;
using AcxiomCRM.Application.ViewModels;
using AcxiomCRM.Domain.Entities;
using AcxiomCRM.Domain.Enums;
using AcxiomCRM.Infrastructure.Data;

namespace AcxiomCRM.Web.Controllers;

[Authorize]
public class ActivitiesController : Controller
{
    private readonly IActivityService _activityService;
    private readonly IUserService _userService;
    private readonly IApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public ActivitiesController(
        IActivityService activityService,
        IUserService userService,
        IApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _activityService = activityService;
        _userService = userService;
        _context = context;
        _userManager = userManager;
    }

    private async Task<(string? UserId, string Role)> GetCurrentUserScopeAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await _userManager.GetUserAsync(User);
        var role = user?.RoleName ?? AppRoles.SalesExecutive;
        return (userId, role);
    }

    private async Task PopulateDropdownsAsync(string? userId, string role, int? custId = null, int? leadId = null, string? execId = null)
    {
        var custQuery = _context.Customers.Where(c => c.IsActive).OrderBy(c => c.CustomerName).AsNoTracking();
        var leadQuery = _context.Leads.Where(l => l.IsActive).OrderBy(l => l.LeadName).AsNoTracking();

        if (role == AppRoles.SalesExecutive && !string.IsNullOrEmpty(userId))
        {
            custQuery = custQuery.Where(c => c.AssignedToUserId == userId);
            leadQuery = leadQuery.Where(l => l.AssignedToUserId == userId);
        }

        ViewBag.Customers = new SelectList(await custQuery.ToListAsync(), "CustomerId", "CustomerName", custId);
        ViewBag.Leads = new SelectList(await leadQuery.ToListAsync(), "LeadId", "LeadName", leadId);

        var reps = await _userService.GetActiveSalesExecutivesAsync();
        ViewBag.Executives = new SelectList(reps, "Id", "FullName", execId);
    }

    [HttpGet]
    public async Task<IActionResult> Index(ActivityType? type, ActivityStatus? status, DateTime? date, string? assignedUserId, string? search, int page = 1)
    {
        var (userId, role) = await GetCurrentUserScopeAsync();
        await PopulateDropdownsAsync(userId, role, null, null, assignedUserId);
        var model = await _activityService.GetActivitiesAsync(userId, role, type, status, date, assignedUserId, search, page);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? customerId = null, int? leadId = null)
    {
        var (userId, role) = await GetCurrentUserScopeAsync();
        await PopulateDropdownsAsync(userId, role, customerId, leadId, role == AppRoles.SalesExecutive ? userId : null);

        var model = new ActivityCreateEditViewModel
        {
            CustomerId = customerId,
            LeadId = leadId,
            ActivityDate = DateTime.UtcNow
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ActivityCreateEditViewModel model)
    {
        var (userId, role) = await GetCurrentUserScopeAsync();

        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync(userId, role, model.CustomerId, model.LeadId, model.AssignedToUserId);
            return View(model);
        }

        var result = await _activityService.CreateActivityAsync(model, userId, role);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            await PopulateDropdownsAsync(userId, role, model.CustomerId, model.LeadId, model.AssignedToUserId);
            return View(model);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var (userId, role) = await GetCurrentUserScopeAsync();
        var model = await _activityService.GetActivityForEditAsync(id, userId, role);
        if (model == null)
        {
            return NotFound("Activity not found or access denied.");
        }

        await PopulateDropdownsAsync(userId, role, model.CustomerId, model.LeadId, model.AssignedToUserId);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ActivityCreateEditViewModel model)
    {
        var (userId, role) = await GetCurrentUserScopeAsync();

        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync(userId, role, model.CustomerId, model.LeadId, model.AssignedToUserId);
            return View(model);
        }

        var result = await _activityService.UpdateActivityAsync(model, userId, role);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            await PopulateDropdownsAsync(userId, role, model.CustomerId, model.LeadId, model.AssignedToUserId);
            return View(model);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var (userId, role) = await GetCurrentUserScopeAsync();
        var result = await _activityService.DeleteActivityAsync(id, userId, role);

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
