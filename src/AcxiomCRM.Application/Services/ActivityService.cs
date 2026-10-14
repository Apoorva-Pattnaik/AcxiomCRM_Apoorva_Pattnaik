using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Application.Interfaces;
using AcxiomCRM.Application.ViewModels;
using AcxiomCRM.Domain.Entities;
using AcxiomCRM.Domain.Enums;
using AcxiomCRM.Infrastructure.Data;

namespace AcxiomCRM.Application.Services;

public class ActivityService : IActivityService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public ActivityService(ApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    private IQueryable<Activity> ApplyScope(IQueryable<Activity> query, string? userId, string userRole)
    {
        if (userRole == AppRoles.SalesExecutive && !string.IsNullOrEmpty(userId))
        {
            return query.Where(a => a.AssignedToUserId == userId);
        }
        return query;
    }

    public async Task<ActivityListViewModel> GetActivitiesAsync(string? userId, string userRole, ActivityType? type, ActivityStatus? status, DateTime? date, string? assignedUserId, string? search, int page = 1, int pageSize = 10)
    {
        var query = _context.Activities
            .Include(a => a.Customer)
            .Include(a => a.Lead)
            .Include(a => a.AssignedToUser)
            .Where(a => a.IsActive)
            .AsNoTracking();

        query = ApplyScope(query, userId, userRole);

        if (type.HasValue)
        {
            query = query.Where(a => a.ActivityType == type.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(a => a.Status == status.Value);
        }

        if (date.HasValue)
        {
            query = query.Where(a => a.ActivityDate.Date == date.Value.Date);
        }

        if (!string.IsNullOrWhiteSpace(assignedUserId) && userRole != AppRoles.SalesExecutive)
        {
            query = query.Where(a => a.AssignedToUserId == assignedUserId);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(a => a.Subject.ToLower().Contains(s) || (a.Description != null && a.Description.ToLower().Contains(s)));
        }

        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
        if (totalPages < 1) totalPages = 1;
        if (page < 1) page = 1;

        var items = await query.OrderByDescending(a => a.ActivityDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new ActivityItemViewModel
            {
                ActivityId = a.ActivityId,
                ActivityType = a.ActivityType,
                Subject = a.Subject,
                Description = a.Description,
                ActivityDate = a.ActivityDate,
                CustomerId = a.CustomerId,
                CustomerName = a.Customer != null ? a.Customer.CustomerName : null,
                LeadId = a.LeadId,
                LeadName = a.Lead != null ? a.Lead.LeadName : null,
                AssignedToUserName = a.AssignedToUser != null ? a.AssignedToUser.FullName : "Unassigned",
                Status = a.Status,
                CreatedDate = a.CreatedDate
            })
            .ToListAsync();

        return new ActivityListViewModel
        {
            Activities = items,
            TypeFilter = type,
            StatusFilter = status,
            DateFilter = date,
            AssignedUserFilter = assignedUserId,
            SearchTerm = search,
            CurrentPage = page,
            TotalPages = totalPages,
            TotalItems = totalItems,
            PageSize = pageSize
        };
    }

    public async Task<ActivityCreateEditViewModel?> GetActivityForEditAsync(int activityId, string? userId, string userRole)
    {
        var query = _context.Activities.Where(a => a.ActivityId == activityId && a.IsActive);
        query = ApplyScope(query, userId, userRole);
        var a = await query.FirstOrDefaultAsync();
        if (a == null) return null;

        return new ActivityCreateEditViewModel
        {
            ActivityId = a.ActivityId,
            ActivityType = a.ActivityType,
            Subject = a.Subject,
            Description = a.Description,
            ActivityDate = a.ActivityDate,
            CustomerId = a.CustomerId,
            LeadId = a.LeadId,
            Status = a.Status,
            AssignedToUserId = a.AssignedToUserId
        };
    }

    public async Task<(bool Success, string Message, int ActivityId)> CreateActivityAsync(ActivityCreateEditViewModel model, string? currentUserId, string currentUserRole)
    {
        if (string.IsNullOrWhiteSpace(model.Subject))
            return (false, "Subject is required.", 0);

        var activity = new Activity
        {
            ActivityType = model.ActivityType,
            Subject = model.Subject.Trim(),
            Description = model.Description?.Trim(),
            ActivityDate = model.ActivityDate,
            CustomerId = model.CustomerId > 0 ? model.CustomerId : null,
            LeadId = model.LeadId > 0 ? model.LeadId : null,
            Status = model.Status,
            AssignedToUserId = currentUserRole == AppRoles.SalesExecutive ? currentUserId : model.AssignedToUserId,
            CreatedDate = DateTime.UtcNow,
            IsActive = true
        };

        _context.Activities.Add(activity);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync("Create", "Activity", activity.ActivityId.ToString(), null, activity.Subject, "Success", $"Logged {activity.ActivityType} activity '{activity.Subject}'");

        return (true, "Activity logged successfully.", activity.ActivityId);
    }

    public async Task<(bool Success, string Message)> UpdateActivityAsync(ActivityCreateEditViewModel model, string? currentUserId, string currentUserRole)
    {
        var query = _context.Activities.Where(a => a.ActivityId == model.ActivityId && a.IsActive);
        query = ApplyScope(query, currentUserId, currentUserRole);
        var activity = await query.FirstOrDefaultAsync();

        if (activity == null)
            return (false, "Activity not found or access denied.");

        activity.ActivityType = model.ActivityType;
        activity.Subject = model.Subject.Trim();
        activity.Description = model.Description?.Trim();
        activity.ActivityDate = model.ActivityDate;
        activity.CustomerId = model.CustomerId > 0 ? model.CustomerId : null;
        activity.LeadId = model.LeadId > 0 ? model.LeadId : null;
        activity.Status = model.Status;

        if (currentUserRole != AppRoles.SalesExecutive)
        {
            activity.AssignedToUserId = model.AssignedToUserId;
        }

        await _context.SaveChangesAsync();

        await _auditService.LogAsync("Update", "Activity", activity.ActivityId.ToString(), null, activity.Subject, "Success", $"Updated {activity.ActivityType} activity '{activity.Subject}'");

        return (true, "Activity updated successfully.");
    }

    public async Task<(bool Success, string Message)> DeleteActivityAsync(int activityId, string? currentUserId, string currentUserRole)
    {
        var query = _context.Activities.Where(a => a.ActivityId == activityId && a.IsActive);
        query = ApplyScope(query, currentUserId, currentUserRole);
        var activity = await query.FirstOrDefaultAsync();

        if (activity == null)
            return (false, "Activity not found or access denied.");

        activity.IsActive = false;
        activity.Status = ActivityStatus.Cancelled;

        await _context.SaveChangesAsync();

        await _auditService.LogAsync("Delete", "Activity", activity.ActivityId.ToString(), activity.Subject, "Deleted", "Success", $"Deleted activity '{activity.Subject}'");

        return (true, "Activity removed successfully.");
    }

    public async Task<bool> CanAccessActivityAsync(int activityId, string? userId, string userRole)
    {
        if (userRole == AppRoles.Admin || userRole == AppRoles.Manager)
            return await _context.Activities.AnyAsync(a => a.ActivityId == activityId && a.IsActive);

        return await _context.Activities.AnyAsync(a => a.ActivityId == activityId && a.IsActive && a.AssignedToUserId == userId);
    }
}
