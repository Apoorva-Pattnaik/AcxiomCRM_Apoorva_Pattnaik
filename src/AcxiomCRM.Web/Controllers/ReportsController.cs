using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using AcxiomCRM.Application.Interfaces;
using AcxiomCRM.Domain.Entities;
using AcxiomCRM.Domain.Enums;

namespace AcxiomCRM.Web.Controllers;

[Authorize]
public class ReportsController : Controller
{
    private readonly IReportService _reportService;
    private readonly IUserService _userService;
    private readonly UserManager<ApplicationUser> _userManager;

    public ReportsController(
        IReportService reportService,
        IUserService userService,
        UserManager<ApplicationUser> userManager)
    {
        _reportService = reportService;
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

    private async Task PopulateOwnersDropdownAsync(string? selectedId = null)
    {
        var reps = await _userService.GetActiveSalesExecutivesAsync();
        ViewBag.Owners = new SelectList(reps, "Id", "FullName", selectedId);
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var (_, role) = await GetCurrentUserScopeAsync();
        ViewBag.UserRole = role;
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> CustomerReport(DateTime? fromDate, DateTime? toDate, CustomerStatus? status, string? ownerId)
    {
        var (userId, role) = await GetCurrentUserScopeAsync();
        await PopulateOwnersDropdownAsync(ownerId);
        var model = await _reportService.GetCustomerReportAsync(userId, role, fromDate, toDate, status, ownerId);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> LeadReport(DateTime? fromDate, DateTime? toDate, LeadStatus? status, string? source, string? ownerId)
    {
        var (userId, role) = await GetCurrentUserScopeAsync();
        await PopulateOwnersDropdownAsync(ownerId);
        var model = await _reportService.GetLeadReportAsync(userId, role, fromDate, toDate, status, source, ownerId);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> FollowUpReport(DateTime? fromDate, DateTime? toDate, FollowUpStatus? status, FollowUpType? type, string? ownerId)
    {
        var (userId, role) = await GetCurrentUserScopeAsync();
        await PopulateOwnersDropdownAsync(ownerId);
        var model = await _reportService.GetFollowUpReportAsync(userId, role, fromDate, toDate, status, type, ownerId);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> OpportunityReport(DateTime? fromDate, DateTime? toDate, OpportunityStage? stage, OpportunityStatus? status, string? ownerId)
    {
        var (userId, role) = await GetCurrentUserScopeAsync();
        await PopulateOwnersDropdownAsync(ownerId);
        var model = await _reportService.GetOpportunityReportAsync(userId, role, fromDate, toDate, stage, status, ownerId);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> PipelineReport()
    {
        var (userId, role) = await GetCurrentUserScopeAsync();
        var model = await _reportService.GetPipelineReportAsync(userId, role);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> ConversionReport(DateTime? fromDate, DateTime? toDate)
    {
        var (userId, role) = await GetCurrentUserScopeAsync();
        var model = await _reportService.GetSalesConversionReportAsync(userId, role, fromDate, toDate);
        return View(model);
    }

    [HttpGet]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Manager}")]
    public async Task<IActionResult> UserActivityReport(DateTime? fromDate, DateTime? toDate)
    {
        var (userId, role) = await GetCurrentUserScopeAsync();
        var model = await _reportService.GetUserActivityReportAsync(userId, role, fromDate, toDate);
        return View(model);
    }

    [HttpGet]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> AuditReport(string? filterUserId, string? entityName, string? action, DateTime? fromDate, DateTime? toDate, int page = 1)
    {
        var (userId, role) = await GetCurrentUserScopeAsync();
        var model = await _reportService.GetAuditReportAsync(userId, role, filterUserId, entityName, action, fromDate, toDate, page);
        return View(model);
    }
}
