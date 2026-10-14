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
public class FollowUpsController : Controller
{
    private readonly IFollowUpService _followUpService;
    private readonly IUserService _userService;
    private readonly IApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public FollowUpsController(
        IFollowUpService followUpService,
        IUserService userService,
        IApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _followUpService = followUpService;
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

    private async Task PopulateDropdownsAsync(string? userId, string role, int? custId = null, int? leadId = null, int? oppId = null, string? execId = null)
    {
        var custQuery = _context.Customers.Where(c => c.IsActive).OrderBy(c => c.CustomerName).AsNoTracking();
        var leadQuery = _context.Leads.Where(l => l.IsActive && l.Status != LeadStatus.Converted).OrderBy(l => l.LeadName).AsNoTracking();
        var oppQuery = _context.Opportunities.Where(o => o.IsActive).OrderBy(o => o.OpportunityName).AsNoTracking();

        if (role == AppRoles.SalesExecutive && !string.IsNullOrEmpty(userId))
        {
            custQuery = custQuery.Where(c => c.AssignedToUserId == userId);
            leadQuery = leadQuery.Where(l => l.AssignedToUserId == userId);
            oppQuery = oppQuery.Where(o => o.AssignedToUserId == userId);
        }

        ViewBag.Customers = new SelectList(await custQuery.ToListAsync(), "CustomerId", "CustomerName", custId);
        ViewBag.Leads = new SelectList(await leadQuery.ToListAsync(), "LeadId", "LeadName", leadId);
        ViewBag.Opportunities = new SelectList(await oppQuery.ToListAsync(), "OpportunityId", "OpportunityName", oppId);

        var reps = await _userService.GetActiveSalesExecutivesAsync();
        ViewBag.Executives = new SelectList(reps, "Id", "FullName", execId);
    }

    [HttpGet]
    public async Task<IActionResult> Index(FollowUpStatus? status, FollowUpType? type, DateTime? date, int? customerId, int? leadId, string? assignedUserId, int page = 1)
    {
        var (userId, role) = await GetCurrentUserScopeAsync();
        await PopulateDropdownsAsync(userId, role, customerId, leadId, null, assignedUserId);
        var model = await _followUpService.GetFollowUpsAsync(userId, role, status, type, date, customerId, leadId, assignedUserId, page);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? customerId = null, int? leadId = null, int? opportunityId = null)
    {
        var (userId, role) = await GetCurrentUserScopeAsync();
        await PopulateDropdownsAsync(userId, role, customerId, leadId, opportunityId, role == AppRoles.SalesExecutive ? userId : null);

        var model = new FollowUpCreateEditViewModel
        {
            CustomerId = customerId,
            LeadId = leadId,
            OpportunityId = opportunityId,
            FollowUpDate = DateTime.UtcNow.Date.AddDays(1).AddHours(10)
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FollowUpCreateEditViewModel model)
    {
        var (userId, role) = await GetCurrentUserScopeAsync();

        if (model.Status == FollowUpStatus.Planned && model.FollowUpDate.Date < DateTime.UtcNow.Date)
        {
            ModelState.AddModelError("FollowUpDate", "Follow-up date cannot be earlier than today for a new/planned follow-up.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync(userId, role, model.CustomerId, model.LeadId, model.OpportunityId, model.AssignedToUserId);
            return View(model);
        }

        var result = await _followUpService.CreateFollowUpAsync(model, userId, role);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            await PopulateDropdownsAsync(userId, role, model.CustomerId, model.LeadId, model.OpportunityId, model.AssignedToUserId);
            return View(model);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var (userId, role) = await GetCurrentUserScopeAsync();
        var model = await _followUpService.GetFollowUpForEditAsync(id, userId, role);
        if (model == null)
        {
            return NotFound("Follow-up not found or access denied.");
        }

        await PopulateDropdownsAsync(userId, role, model.CustomerId, model.LeadId, model.OpportunityId, model.AssignedToUserId);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(FollowUpCreateEditViewModel model)
    {
        var (userId, role) = await GetCurrentUserScopeAsync();

        if (model.Status == FollowUpStatus.Planned && model.FollowUpDate.Date < DateTime.UtcNow.Date)
        {
            ModelState.AddModelError("FollowUpDate", "Follow-up date cannot be earlier than today for a new/planned follow-up.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync(userId, role, model.CustomerId, model.LeadId, model.OpportunityId, model.AssignedToUserId);
            return View(model);
        }

        var result = await _followUpService.UpdateFollowUpAsync(model, userId, role);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            await PopulateDropdownsAsync(userId, role, model.CustomerId, model.LeadId, model.OpportunityId, model.AssignedToUserId);
            return View(model);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(int id, string? remarks)
    {
        var (userId, role) = await GetCurrentUserScopeAsync();
        var result = await _followUpService.CompleteFollowUpAsync(id, remarks, userId, role);

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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var (userId, role) = await GetCurrentUserScopeAsync();
        var result = await _followUpService.DeleteFollowUpAsync(id, userId, role);

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
