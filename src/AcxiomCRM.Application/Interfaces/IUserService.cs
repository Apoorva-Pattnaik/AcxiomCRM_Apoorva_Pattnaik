using AcxiomCRM.Application.ViewModels;
using AcxiomCRM.Domain.Entities;

namespace AcxiomCRM.Application.Interfaces;

public interface IUserService
{
    Task<UserListViewModel> GetUsersAsync(string? roleFilter, string? search);
    Task<List<ApplicationUser>> GetActiveSalesExecutivesAsync();
    Task<ApplicationUser?> GetUserByIdAsync(string userId);
    Task<EditUserViewModel?> GetUserForEditAsync(string userId);
    Task<(bool Success, string Message)> CreateUserAsync(CreateUserViewModel model, string? currentUserId);
    Task<(bool Success, string Message)> UpdateUserAsync(EditUserViewModel model, string? currentUserId);
    Task<(bool Success, string Message)> ToggleUserStatusAsync(string userId, string? currentUserId);
    Task<(bool Success, string Message)> ResetPasswordAsync(ResetPasswordViewModel model, string? currentUserId);
    Task<(bool Success, string Message)> UnlockUserAsync(string userId, string? currentUserId);
}
