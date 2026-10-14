using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.Application.Interfaces;
using AcxiomCRM.Domain.Entities;
using AcxiomCRM.Domain.Enums;

namespace AcxiomCRM.Web.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly IDashboardService _dashboardService;
    private readonly UserManager<ApplicationUser> _userManager;

    public DashboardController(IDashboardService dashboardService, UserManager<ApplicationUser> userManager)
    {
        _dashboardService = dashboardService;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string dateFilter = "ThisMonth", DateTime? customStart = null, DateTime? customEnd = null)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await _userManager.GetUserAsync(User);
        var userRole = user?.RoleName ?? AppRoles.SalesExecutive;

        var model = await _dashboardService.GetDashboardDataAsync(userId, userRole, dateFilter, customStart, customEnd);
        model.UserFullName = user?.FullName ?? "User";

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> GetChartData(string dateFilter = "ThisMonth", DateTime? customStart = null, DateTime? customEnd = null)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await _userManager.GetUserAsync(User);
        var userRole = user?.RoleName ?? AppRoles.SalesExecutive;

        var model = await _dashboardService.GetDashboardDataAsync(userId, userRole, dateFilter, customStart, customEnd);

        return Json(new
        {
            leadStatus = model.LeadStatusChart,
            pipeline = model.OpportunityPipelineChart,
            monthlySales = model.MonthlySalesChart
        });
    }
}
