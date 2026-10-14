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
[Route("api/leads")]
[Authorize]
public class LeadsApiController : ControllerBase
{
    private readonly ILeadService _leadService;
    private readonly UserManager<ApplicationUser> _userManager;

    public LeadsApiController(ILeadService leadService, UserManager<ApplicationUser> userManager)
    {
        _leadService = leadService;
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
    public async Task<IActionResult> GetLeads([FromQuery] string? search = null)
    {
        var (userId, role) = await GetScopeAsync();
        var dtos = await _leadService.GetLeadDtosAsync(userId, role, search);
        return Ok(dtos);
    }

    [HttpPost]
    public async Task<IActionResult> CreateLead([FromBody] CreateLeadDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var (userId, role) = await GetScopeAsync();
        var result = await _leadService.CreateLeadFromDtoAsync(dto, userId, role);

        if (!result.Success)
        {
            return BadRequest(new { message = result.Message });
        }

        return StatusCode(201, result.Lead);
    }
}
