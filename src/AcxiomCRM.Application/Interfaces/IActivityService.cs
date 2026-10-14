using AcxiomCRM.Application.ViewModels;
using AcxiomCRM.Domain.Enums;

namespace AcxiomCRM.Application.Interfaces;

public interface IActivityService
{
    Task<ActivityListViewModel> GetActivitiesAsync(string? userId, string userRole, ActivityType? type, ActivityStatus? status, DateTime? date, string? assignedUserId, string? search, int page = 1, int pageSize = 10);
    Task<ActivityCreateEditViewModel?> GetActivityForEditAsync(int activityId, string? userId, string userRole);
    Task<(bool Success, string Message, int ActivityId)> CreateActivityAsync(ActivityCreateEditViewModel model, string? currentUserId, string currentUserRole);
    Task<(bool Success, string Message)> UpdateActivityAsync(ActivityCreateEditViewModel model, string? currentUserId, string currentUserRole);
    Task<(bool Success, string Message)> DeleteActivityAsync(int activityId, string? currentUserId, string currentUserRole);
    Task<bool> CanAccessActivityAsync(int activityId, string? userId, string userRole);
}
