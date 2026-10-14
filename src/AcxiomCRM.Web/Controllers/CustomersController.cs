using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using AcxiomCRM.Application.Interfaces;
using AcxiomCRM.Application.ViewModels;
using AcxiomCRM.Domain.Entities;
using AcxiomCRM.Domain.Enums;

namespace AcxiomCRM.Web.Controllers;

[Authorize]
public class CustomersController : Controller
{
    private readonly ICustomerService _customerService;
    private readonly IUserService _userService;
    private readonly UserManager<ApplicationUser> _userManager;

    public CustomersController(
        ICustomerService customerService,
        IUserService userService,
        UserManager<ApplicationUser> userManager)
    {
        _customerService = customerService;
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

    private async Task PopulateExecutivesDropdownAsync(string? selectedId = null)
    {
        var reps = await _userService.GetActiveSalesExecutivesAsync();
        ViewBag.Executives = new SelectList(reps, "Id", "FullName", selectedId);
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? search, CustomerStatus? status, string? city, int page = 1)
    {
        var (userId, role) = await GetCurrentUserScopeAsync();
        var model = await _customerService.GetCustomersAsync(userId, role, search, status, city, page);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var (userId, role) = await GetCurrentUserScopeAsync();
        var model = await _customerService.GetCustomerDetailsAsync(id, userId, role);
        if (model == null)
        {
            return NotFound("Customer not found or you are not authorized to view this record.");
        }
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var (userId, role) = await GetCurrentUserScopeAsync();
        await PopulateExecutivesDropdownAsync(role == AppRoles.SalesExecutive ? userId : null);
        return View(new CustomerCreateEditViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CustomerCreateEditViewModel model)
    {
        var (userId, role) = await GetCurrentUserScopeAsync();

        if (!ModelState.IsValid)
        {
            await PopulateExecutivesDropdownAsync(model.AssignedToUserId);
            return View(model);
        }

        var result = await _customerService.CreateCustomerAsync(model, userId, role);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            await PopulateExecutivesDropdownAsync(model.AssignedToUserId);
            return View(model);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Details), new { id = result.CustomerId });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var (userId, role) = await GetCurrentUserScopeAsync();
        var model = await _customerService.GetCustomerForEditAsync(id, userId, role);
        if (model == null)
        {
            return NotFound("Customer not found or you are not authorized to modify this record.");
        }

        await PopulateExecutivesDropdownAsync(model.AssignedToUserId);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(CustomerCreateEditViewModel model)
    {
        var (userId, role) = await GetCurrentUserScopeAsync();

        if (!ModelState.IsValid)
        {
            await PopulateExecutivesDropdownAsync(model.AssignedToUserId);
            return View(model);
        }

        var result = await _customerService.UpdateCustomerAsync(model, userId, role);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            await PopulateExecutivesDropdownAsync(model.AssignedToUserId);
            return View(model);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Details), new { id = model.CustomerId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var (userId, role) = await GetCurrentUserScopeAsync();
        var result = await _customerService.DeactivateCustomerAsync(id, userId, role);

        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.Message;
            return RedirectToAction(nameof(Details), new { id });
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }
}
