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
[Route("api/followups")]
[Authorize]
public class FollowUpsApiController : ControllerBase
{
    private readonly IFollowUpService _followUpService;
    private readonly UserManager<ApplicationUser> _userManager;

    public FollowUpsApiController(IFollowUpService followUpService, UserManager<ApplicationUser> userManager)
    {
        _followUpService = followUpService;
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
    public async Task<IActionResult> GetFollowUps()
    {
        var (userId, role) = await GetScopeAsync();
        var dtos = await _followUpService.GetFollowUpDtosAsync(userId, role);
        return Ok(dtos);
    }

    [HttpPost]
    public async Task<IActionResult> CreateFollowUp([FromBody] CreateFollowUpDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var (userId, role) = await GetScopeAsync();
        var result = await _followUpService.CreateFollowUpFromDtoAsync(dto, userId, role);

        if (!result.Success)
        {
            return BadRequest(new { message = result.Message });
        }

        return StatusCode(201, result.FollowUp);
    }
}
