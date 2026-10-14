using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.Application.DTOs;
using AcxiomCRM.Application.Interfaces;
using AcxiomCRM.Application.ViewModels;
using AcxiomCRM.Domain.Entities;
using AcxiomCRM.Domain.Enums;

namespace AcxiomCRM.Web.Controllers.Api;

[ApiController]
[Route("api/customers")]
[Authorize]
public class CustomersApiController : ControllerBase
{
    private readonly ICustomerService _customerService;
    private readonly UserManager<ApplicationUser> _userManager;

    public CustomersApiController(ICustomerService customerService, UserManager<ApplicationUser> userManager)
    {
        _customerService = customerService;
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
    public async Task<IActionResult> GetCustomers([FromQuery] string? search = null)
    {
        var (userId, role) = await GetScopeAsync();
        var dtos = await _customerService.GetCustomerDtosAsync(userId, role, search);
        return Ok(dtos);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetCustomerById(int id)
    {
        var (userId, role) = await GetScopeAsync();
        var dto = await _customerService.GetCustomerDtoByIdAsync(id, userId, role);
        if (dto == null)
        {
            return NotFound(new { message = $"Customer with ID {id} was not found or access is restricted." });
        }
        return Ok(dto);
    }

    [HttpPost]
    public async Task<IActionResult> CreateCustomer([FromBody] CreateCustomerDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var (userId, role) = await GetScopeAsync();
        var result = await _customerService.CreateCustomerFromDtoAsync(dto, userId, role);

        if (!result.Success)
        {
            if (result.Message.Contains("already exists"))
            {
                return Conflict(new { message = result.Message });
            }
            return BadRequest(new { message = result.Message });
        }

        return CreatedAtAction(nameof(GetCustomerById), new { id = result.Customer!.CustomerId }, result.Customer);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateCustomer(int id, [FromBody] CreateCustomerDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var (userId, role) = await GetScopeAsync();
        var canAccess = await _customerService.CanAccessCustomerAsync(id, userId, role);
        if (!canAccess)
        {
            return NotFound(new { message = $"Customer with ID {id} was not found or access is restricted." });
        }

        var vm = new CustomerCreateEditViewModel
        {
            CustomerId = id,
            CustomerName = dto.CustomerName,
            Email = dto.Email,
            Phone = dto.Phone,
            CompanyName = dto.CompanyName,
            Address = dto.Address,
            City = dto.City,
            State = dto.State,
            Notes = dto.Notes,
            AssignedToUserId = dto.AssignedToUserId
        };

        var result = await _customerService.UpdateCustomerAsync(vm, userId, role);
        if (!result.Success)
        {
            if (result.Message.Contains("already exists"))
            {
                return Conflict(new { message = result.Message });
            }
            return BadRequest(new { message = result.Message });
        }

        var updated = await _customerService.GetCustomerDtoByIdAsync(id, userId, role);
        return Ok(updated);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteCustomer(int id)
    {
        var (userId, role) = await GetScopeAsync();
        var result = await _customerService.DeactivateCustomerAsync(id, userId, role);

        if (!result.Success)
        {
            return NotFound(new { message = result.Message });
        }

        return Ok(new { message = result.Message });
    }
}
