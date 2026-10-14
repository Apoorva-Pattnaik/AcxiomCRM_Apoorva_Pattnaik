using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.Application.Interfaces;
using AcxiomCRM.Domain.Entities;
using AcxiomCRM.Domain.Enums;

namespace AcxiomCRM.Web.Controllers.Api;

[ApiController]
[Route("api/reports")]
[Authorize]
public class ReportsApiController : ControllerBase
{
    private readonly IOpportunityService _opportunityService;
    private readonly UserManager<ApplicationUser> _userManager;

    public ReportsApiController(IOpportunityService opportunityService, UserManager<ApplicationUser> userManager)
    {
        _opportunityService = opportunityService;
        _userManager = userManager;
    }

    [HttpGet("pipeline")]
    public async Task<IActionResult> GetPipelineReport()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await _userManager.GetUserAsync(User);
        var role = user?.RoleName ?? AppRoles.SalesExecutive;

        var report = await _opportunityService.GetPipelineSummaryAsync(userId, role);
        return Ok(report);
    }
}
