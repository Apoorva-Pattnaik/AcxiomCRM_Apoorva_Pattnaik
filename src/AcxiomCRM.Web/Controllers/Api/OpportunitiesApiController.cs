using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.Application.DTOs;
using AcxiomCRM.Application.Interfaces;
using AcxiomCRM.Domain.Entities;
using AcxiomCRM.Domain.Enums;

namespace AcxiomCRM.Web.Controllers.Api;

[ApiController]
[Route("api/opportunities")]
[Authorize]
public class OpportunitiesApiController : ControllerBase
{
    private readonly IOpportunityService _opportunityService;
    private readonly UserManager<ApplicationUser> _userManager;

    public OpportunitiesApiController(IOpportunityService opportunityService, UserManager<ApplicationUser> userManager)
    {
        _opportunityService = opportunityService;
        _userManager = userManager;
    }

    private async Task<(string? UserId, string Role)> GetScopeAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await _userManager.GetUserAsync(User);
        var role = user?.RoleName ?? AppRoles.SalesExecutive;
        return (userId, role);
    }

    [HttpGet]
    public async Task<IActionResult> GetOpportunities([FromQuery] string? search = null)
    {
        var (userId, role) = await GetScopeAsync();
        var dtos = await _opportunityService.GetOpportunityDtosAsync(userId, role, search);
        return Ok(dtos);
    }

    [HttpPost]
    public async Task<IActionResult> CreateOpportunity([FromBody] CreateOpportunityDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var (userId, role) = await GetScopeAsync();
        var result = await _opportunityService.CreateOpportunityFromDtoAsync(dto, userId, role);

        if (!result.Success)
        {
            return BadRequest(new { message = result.Message });
        }

        return StatusCode(201, result.Opportunity);
    }
}
