using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Application.Interfaces;
using AcxiomCRM.Application.ViewModels;
using AcxiomCRM.Domain.Entities;
using AcxiomCRM.Domain.Enums;

namespace AcxiomCRM.Application.Services;

public class UserService : IUserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IAuditService _auditService;

    public UserService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IAuditService auditService)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _auditService = auditService;
    }

    public async Task<UserListViewModel> GetUsersAsync(string? roleFilter, string? search)
    {
        var query = _userManager.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(roleFilter))
        {
            query = query.Where(u => u.RoleName == roleFilter);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(u => u.FullName.ToLower().Contains(s) ||
                                     u.Email!.ToLower().Contains(s) ||
                                     (u.Department != null && u.Department.ToLower().Contains(s)));
        }

        var users = await query.OrderByDescending(u => u.CreatedDate).ToListAsync();
        var now = DateTimeOffset.UtcNow;

        var items = users.Select(u => new UserItemViewModel
        {
            Id = u.Id,
            FullName = u.FullName,
            Email = u.Email ?? string.Empty,
            UserName = u.UserName ?? string.Empty,
            RoleName = u.RoleName,
            Department = u.Department,
            IsActive = u.IsActive,
            IsLockedOut = u.LockoutEnd.HasValue && u.LockoutEnd > now,
            LockoutEnd = u.LockoutEnd,
            AccessFailedCount = u.AccessFailedCount,
            CreatedDate = u.CreatedDate
        }).ToList();

        return new UserListViewModel
        {
            Users = items,
            RoleFilter = roleFilter,
            SearchTerm = search
        };
    }

    public async Task<List<ApplicationUser>> GetActiveSalesExecutivesAsync()
    {
        return await _userManager.Users
            .Where(u => u.IsActive && (u.RoleName == AppRoles.SalesExecutive || u.RoleName == AppRoles.Manager))
            .OrderBy(u => u.FullName)
            .ToListAsync();
    }

    public async Task<ApplicationUser?> GetUserByIdAsync(string userId)
    {
        return await _userManager.FindByIdAsync(userId);
    }

    public async Task<EditUserViewModel?> GetUserForEditAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return null;

        var isLocked = await _userManager.IsLockedOutAsync(user);

        return new EditUserViewModel
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            Role = user.RoleName,
            Department = user.Department,
            PhoneNumber = user.PhoneNumber,
            IsActive = user.IsActive,
            LockoutEnabled = user.LockoutEnabled,
            IsCurrentlyLockedOut = isLocked
        };
    }

    public async Task<(bool Success, string Message)> CreateUserAsync(CreateUserViewModel model, string? currentUserId)
    {
        var existing = await _userManager.FindByEmailAsync(model.Email);
        if (existing != null)
            return (false, $"User with email '{model.Email}' already exists.");

        if (!AppRoles.AllRoles.Contains(model.Role))
            return (false, "Invalid role specified.");

        var user = new ApplicationUser
        {
            UserName = model.Email.Trim().ToLower(),
            Email = model.Email.Trim().ToLower(),
            FullName = model.FullName.Trim(),
            RoleName = model.Role,
            Department = model.Department?.Trim(),
            PhoneNumber = model.PhoneNumber?.Trim(),
            EmailConfirmed = true,
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            return (false, $"Failed to create user: {errors}");
        }

        await _userManager.AddToRoleAsync(user, model.Role);

        await _auditService.LogAsync("CreateUser", "User", user.Id, null, user.Email, "Success", $"Created user account {user.FullName} ({user.Email}) with role {user.RoleName}");

        return (true, "User created successfully.");
    }

    public async Task<(bool Success, string Message)> UpdateUserAsync(EditUserViewModel model, string? currentUserId)
    {
        var user = await _userManager.FindByIdAsync(model.Id);
        if (user == null)
            return (false, "User not found.");

        var oldRole = user.RoleName;
        var oldActive = user.IsActive;

        user.FullName = model.FullName.Trim();
        user.Email = model.Email.Trim().ToLower();
        user.UserName = model.Email.Trim().ToLower();
        user.Department = model.Department?.Trim();
        user.PhoneNumber = model.PhoneNumber?.Trim();
        user.IsActive = model.IsActive;

        // Role update
        if (model.Role != oldRole && AppRoles.AllRoles.Contains(model.Role))
        {
            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, model.Role);
            user.RoleName = model.Role;

            await _auditService.LogAsync("RoleChange", "User", user.Id, oldRole, model.Role, "Success", $"Changed role for user {user.FullName} from {oldRole} to {model.Role}");
        }

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            return (false, $"Failed to update user: {errors}");
        }

        if (oldActive != user.IsActive)
        {
            await _auditService.LogAsync(user.IsActive ? "ActivateUser" : "DeactivateUser", "User", user.Id, oldActive.ToString(), user.IsActive.ToString(), "Success", $"User {user.FullName} active status changed to {user.IsActive}");
        }

        return (true, "User updated successfully.");
    }

    public async Task<(bool Success, string Message)> ToggleUserStatusAsync(string userId, string? currentUserId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return (false, "User not found.");

        if (user.Id == currentUserId)
            return (false, "You cannot deactivate your own administrative account.");

        user.IsActive = !user.IsActive;
        await _userManager.UpdateAsync(user);

        await _auditService.LogAsync(user.IsActive ? "ActivateUser" : "DeactivateUser", "User", user.Id, (!user.IsActive).ToString(), user.IsActive.ToString(), "Success", $"User {user.FullName} status set to Active={user.IsActive}");

        return (true, $"User status updated to {(user.IsActive ? "Active" : "Inactive")}.");
    }

    public async Task<(bool Success, string Message)> ResetPasswordAsync(ResetPasswordViewModel model, string? currentUserId)
    {
        var user = await _userManager.FindByIdAsync(model.UserId);
        if (user == null)
            return (false, "User not found.");

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);

        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            return (false, $"Password reset failed: {errors}");
        }

        // Reset lockout if locked
        await _userManager.SetLockoutEndDateAsync(user, null);
        await _userManager.ResetAccessFailedCountAsync(user);

        await _auditService.LogSecurityEventAsync("AdminPasswordReset", $"Administrator reset password for user {user.Email}", "Success", user.Id, user.UserName);

        return (true, "Password has been reset successfully.");
    }

    public async Task<(bool Success, string Message)> UnlockUserAsync(string userId, string? currentUserId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return (false, "User not found.");

        await _userManager.SetLockoutEndDateAsync(user, null);
        await _userManager.ResetAccessFailedCountAsync(user);

        await _auditService.LogSecurityEventAsync("AdminUnlockAccount", $"Administrator unlocked account for user {user.Email}", "Success", user.Id, user.UserName);

        return (true, "Account unlocked successfully.");
    }
}
