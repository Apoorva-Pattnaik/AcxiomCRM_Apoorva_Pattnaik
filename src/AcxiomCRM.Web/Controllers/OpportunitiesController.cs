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
public class OpportunitiesController : Controller
{
    private readonly IOpportunityService _opportunityService;
    private readonly IUserService _userService;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public OpportunitiesController(
        IOpportunityService opportunityService,
        IUserService userService,
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _opportunityService = opportunityService;
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

    private async Task PopulateDropdownsAsync(string? userId, string role, int? selectedCustomerId = null, string? selectedExecutiveId = null)
    {
        var custQuery = _context.Customers.Where(c => c.IsActive).OrderBy(c => c.CustomerName).AsNoTracking();
        if (role == AppRoles.SalesExecutive && !string.IsNullOrEmpty(userId))
        {
            custQuery = custQuery.Where(c => c.AssignedToUserId == userId);
        }
        var customers = await custQuery.ToListAsync();
        ViewBag.Customers = new SelectList(customers, "CustomerId", "CustomerName", selectedCustomerId);

        var reps = await _userService.GetActiveSalesExecutivesAsync();
        ViewBag.Executives = new SelectList(reps, "Id", "FullName", selectedExecutiveId);
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? search, OpportunityStage? stage, OpportunityStatus? status, int? customerId, string? assignedUserId, int page = 1)
    {
        var (userId, role) = await GetCurrentUserScopeAsync();
        await PopulateDropdownsAsync(userId, role, customerId, assignedUserId);
        var model = await _opportunityService.GetOpportunitiesAsync(userId, role, search, stage, status, customerId, assignedUserId, page);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var (userId, role) = await GetCurrentUserScopeAsync();
        var model = await _opportunityService.GetOpportunityDetailsAsync(id, userId, role);
        if (model == null)
        {
            return NotFound("Opportunity not found or access denied.");
        }
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? customerId = null, int? leadId = null)
    {
        var (userId, role) = await GetCurrentUserScopeAsync();
        await PopulateDropdownsAsync(userId, role, customerId, role == AppRoles.SalesExecutive ? userId : null);

        var model = new OpportunityCreateEditViewModel
        {
            CustomerId = customerId ?? 0,
            LeadId = leadId
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(OpportunityCreateEditViewModel model)
    {
        var (userId, role) = await GetCurrentUserScopeAsync();

        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync(userId, role, model.CustomerId, model.AssignedToUserId);
            return View(model);
        }

        var result = await _opportunityService.CreateOpportunityAsync(model, userId, role);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            await PopulateDropdownsAsync(userId, role, model.CustomerId, model.AssignedToUserId);
            return View(model);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Details), new { id = result.OpportunityId });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var (userId, role) = await GetCurrentUserScopeAsync();
        var model = await _opportunityService.GetOpportunityForEditAsync(id, userId, role);
        if (model == null)
        {
            return NotFound("Opportunity not found or access denied.");
        }

        await PopulateDropdownsAsync(userId, role, model.CustomerId, model.AssignedToUserId);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(OpportunityCreateEditViewModel model)
    {
        var (userId, role) = await GetCurrentUserScopeAsync();

        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync(userId, role, model.CustomerId, model.AssignedToUserId);
            return View(model);
        }

        var result = await _opportunityService.UpdateOpportunityAsync(model, userId, role);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            await PopulateDropdownsAsync(userId, role, model.CustomerId, model.AssignedToUserId);
            return View(model);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Details), new { id = model.OpportunityId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var (userId, role) = await GetCurrentUserScopeAsync();
        var result = await _opportunityService.DeleteOpportunityAsync(id, userId, role);

        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.Message;
            return RedirectToAction(nameof(Details), new { id });
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }
}
