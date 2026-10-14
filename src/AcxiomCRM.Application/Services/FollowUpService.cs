using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Application.DTOs;
using AcxiomCRM.Application.Interfaces;
using AcxiomCRM.Application.ViewModels;
using AcxiomCRM.Domain.Entities;
using AcxiomCRM.Domain.Enums;

namespace AcxiomCRM.Application.Services;

public class FollowUpService : IFollowUpService
{
    private readonly IApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public FollowUpService(IApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    private IQueryable<FollowUp> ApplyScope(IQueryable<FollowUp> query, string? userId, string userRole)
    {
        if (userRole == AppRoles.SalesExecutive && !string.IsNullOrEmpty(userId))
        {
            return query.Where(f => f.AssignedToUserId == userId);
        }
        return query;
    }

    public async Task<FollowUpListViewModel> GetFollowUpsAsync(string? userId, string userRole, FollowUpStatus? status, FollowUpType? type, DateTime? date, int? customerId, int? leadId, string? assignedUserId, int page = 1, int pageSize = 10)
    {
        var query = _context.FollowUps
            .Include(f => f.Customer)
            .Include(f => f.Lead)
            .Include(f => f.Opportunity)
            .Include(f => f.AssignedToUser)
            .Where(f => f.IsActive)
            .AsNoTracking();

        query = ApplyScope(query, userId, userRole);

        if (status.HasValue)
        {
            query = query.Where(f => f.Status == status.Value);
        }

        if (type.HasValue)
        {
            query = query.Where(f => f.FollowUpType == type.Value);
        }

        if (date.HasValue)
        {
            query = query.Where(f => f.FollowUpDate.Date == date.Value.Date);
        }

        if (customerId.HasValue)
        {
            query = query.Where(f => f.CustomerId == customerId.Value);
        }

        if (leadId.HasValue)
        {
            query = query.Where(f => f.LeadId == leadId.Value);
        }

        if (!string.IsNullOrWhiteSpace(assignedUserId) && userRole != AppRoles.SalesExecutive)
        {
            query = query.Where(f => f.AssignedToUserId == assignedUserId);
        }

        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
        if (totalPages < 1) totalPages = 1;
        if (page < 1) page = 1;

        var items = await query.OrderByDescending(f => f.FollowUpDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(f => new FollowUpItemViewModel
            {
                FollowUpId = f.FollowUpId,
                Subject = f.Subject,
                CustomerId = f.CustomerId,
                CustomerName = f.Customer != null ? f.Customer.CustomerName : null,
                LeadId = f.LeadId,
                LeadName = f.Lead != null ? f.Lead.LeadName : null,
                OpportunityId = f.OpportunityId,
                OpportunityName = f.Opportunity != null ? f.Opportunity.OpportunityName : null,
                FollowUpDate = f.FollowUpDate,
                FollowUpType = f.FollowUpType,
                Status = f.Status,
                Remarks = f.Remarks,
                AssignedToUserName = f.AssignedToUser != null ? f.AssignedToUser.FullName : "Unassigned",
                CreatedDate = f.CreatedDate
            })
            .ToListAsync();

        // Calculate upcoming and overdue
        var baseScopeQuery = _context.FollowUps
            .Include(f => f.Customer)
            .Include(f => f.Lead)
            .Include(f => f.Opportunity)
            .Include(f => f.AssignedToUser)
            .Where(f => f.IsActive && f.Status == FollowUpStatus.Planned)
            .AsNoTracking();

        baseScopeQuery = ApplyScope(baseScopeQuery, userId, userRole);

        var today = DateTime.UtcNow.Date;

        var upcoming = await baseScopeQuery
            .Where(f => f.FollowUpDate.Date >= today)
            .OrderBy(f => f.FollowUpDate)
            .Take(5)
            .Select(f => new FollowUpItemViewModel
            {
                FollowUpId = f.FollowUpId,
                Subject = f.Subject,
                CustomerId = f.CustomerId,
                CustomerName = f.Customer != null ? f.Customer.CustomerName : null,
                LeadId = f.LeadId,
                LeadName = f.Lead != null ? f.Lead.LeadName : null,
                OpportunityId = f.OpportunityId,
                OpportunityName = f.Opportunity != null ? f.Opportunity.OpportunityName : null,
                FollowUpDate = f.FollowUpDate,
                FollowUpType = f.FollowUpType,
                Status = f.Status,
                Remarks = f.Remarks,
                AssignedToUserName = f.AssignedToUser != null ? f.AssignedToUser.FullName : "Unassigned",
                CreatedDate = f.CreatedDate
            })
            .ToListAsync();

        var overdue = await baseScopeQuery
            .Where(f => f.FollowUpDate.Date < today)
            .OrderBy(f => f.FollowUpDate)
            .Take(5)
            .Select(f => new FollowUpItemViewModel
            {
                FollowUpId = f.FollowUpId,
                Subject = f.Subject,
                CustomerId = f.CustomerId,
                CustomerName = f.Customer != null ? f.Customer.CustomerName : null,
                LeadId = f.LeadId,
                LeadName = f.Lead != null ? f.Lead.LeadName : null,
                OpportunityId = f.OpportunityId,
                OpportunityName = f.Opportunity != null ? f.Opportunity.OpportunityName : null,
                FollowUpDate = f.FollowUpDate,
                FollowUpType = f.FollowUpType,
                Status = f.Status,
                Remarks = f.Remarks,
                AssignedToUserName = f.AssignedToUser != null ? f.AssignedToUser.FullName : "Unassigned",
                CreatedDate = f.CreatedDate
            })
            .ToListAsync();

        return new FollowUpListViewModel
        {
            FollowUps = items,
            UpcomingFollowUps = upcoming,
            OverdueFollowUps = overdue,
            StatusFilter = status,
            TypeFilter = type,
            DateFilter = date,
            CustomerFilter = customerId,
            LeadFilter = leadId,
            AssignedUserFilter = assignedUserId,
            CurrentPage = page,
            TotalPages = totalPages,
            TotalItems = totalItems,
            PageSize = pageSize
        };
    }

    public async Task<FollowUpCreateEditViewModel?> GetFollowUpForEditAsync(int followUpId, string? userId, string userRole)
    {
        var query = _context.FollowUps.Where(f => f.FollowUpId == followUpId && f.IsActive);
        query = ApplyScope(query, userId, userRole);
        var f = await query.FirstOrDefaultAsync();
        if (f == null) return null;

        return new FollowUpCreateEditViewModel
        {
            FollowUpId = f.FollowUpId,
            Subject = f.Subject,
            CustomerId = f.CustomerId,
            LeadId = f.LeadId,
            OpportunityId = f.OpportunityId,
            FollowUpDate = f.FollowUpDate,
            FollowUpType = f.FollowUpType,
            Status = f.Status,
            Remarks = f.Remarks,
            AssignedToUserId = f.AssignedToUserId
        };
    }

    public async Task<(bool Success, string Message, int FollowUpId)> CreateFollowUpAsync(FollowUpCreateEditViewModel model, string? currentUserId, string currentUserRole)
    {
        if (string.IsNullOrWhiteSpace(model.Subject))
            return (false, "Subject is required.", 0);

        // Validation rule: Follow-up date cannot be earlier than today for a new/planned follow-up
        if (model.Status == FollowUpStatus.Planned && model.FollowUpDate.Date < DateTime.UtcNow.Date)
            return (false, "Follow-up date cannot be earlier than today.", 0);

        var fu = new FollowUp
        {
            Subject = model.Subject.Trim(),
            CustomerId = model.CustomerId > 0 ? model.CustomerId : null,
            LeadId = model.LeadId > 0 ? model.LeadId : null,
            OpportunityId = model.OpportunityId > 0 ? model.OpportunityId : null,
            FollowUpDate = model.FollowUpDate,
            FollowUpType = model.FollowUpType,
            Status = model.Status,
            Remarks = model.Remarks?.Trim(),
            CreatedDate = DateTime.UtcNow,
            AssignedToUserId = currentUserRole == AppRoles.SalesExecutive ? currentUserId : model.AssignedToUserId,
            IsActive = true
        };

        _context.FollowUps.Add(fu);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync("Create", "FollowUp", fu.FollowUpId.ToString(), null, fu.Subject, "Success", $"Created follow-up '{fu.Subject}' scheduled for {fu.FollowUpDate:yyyy-MM-dd}");

        return (true, "Follow-up scheduled successfully.", fu.FollowUpId);
    }

    public async Task<(bool Success, string Message)> UpdateFollowUpAsync(FollowUpCreateEditViewModel model, string? currentUserId, string currentUserRole)
    {
        var query = _context.FollowUps.Where(f => f.FollowUpId == model.FollowUpId && f.IsActive);
        query = ApplyScope(query, currentUserId, currentUserRole);
        var fu = await query.FirstOrDefaultAsync();

        if (fu == null)
            return (false, "Follow-up not found or access denied.");

        // If status remains planned or changes to planned, check date
        if (model.Status == FollowUpStatus.Planned && model.FollowUpDate.Date < DateTime.UtcNow.Date)
            return (false, "Follow-up date cannot be earlier than today.");

        var oldStatus = fu.Status.ToString();
        var oldDate = fu.FollowUpDate;

        fu.Subject = model.Subject.Trim();
        fu.CustomerId = model.CustomerId > 0 ? model.CustomerId : null;
        fu.LeadId = model.LeadId > 0 ? model.LeadId : null;
        fu.OpportunityId = model.OpportunityId > 0 ? model.OpportunityId : null;
        fu.FollowUpDate = model.FollowUpDate;
        fu.FollowUpType = model.FollowUpType;
        fu.Status = model.Status;
        fu.Remarks = model.Remarks?.Trim();

        if (model.Status == FollowUpStatus.Completed && fu.CompletedDate == null)
        {
            fu.CompletedDate = DateTime.UtcNow;
        }

        if (currentUserRole != AppRoles.SalesExecutive)
        {
            fu.AssignedToUserId = model.AssignedToUserId;
        }

        await _context.SaveChangesAsync();

        await _auditService.LogAsync("Update", "FollowUp", fu.FollowUpId.ToString(), $"Date: {oldDate:yyyy-MM-dd}, Status: {oldStatus}", $"Date: {fu.FollowUpDate:yyyy-MM-dd}, Status: {fu.Status}", "Success", $"Updated follow-up '{fu.Subject}'");

        return (true, "Follow-up updated successfully.");
    }

    public async Task<(bool Success, string Message)> CompleteFollowUpAsync(int followUpId, string? remarks, string? currentUserId, string currentUserRole)
    {
        var query = _context.FollowUps.Where(f => f.FollowUpId == followUpId && f.IsActive);
        query = ApplyScope(query, currentUserId, currentUserRole);
        var fu = await query.FirstOrDefaultAsync();

        if (fu == null)
            return (false, "Follow-up not found or access denied.");

        fu.Status = FollowUpStatus.Completed;
        fu.CompletedDate = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(remarks))
        {
            fu.Remarks = string.IsNullOrEmpty(fu.Remarks) ? remarks : $"{fu.Remarks} | Completion remarks: {remarks}";
        }

        await _context.SaveChangesAsync();

        await _auditService.LogAsync("StatusChange", "FollowUp", fu.FollowUpId.ToString(), "Planned", "Completed", "Success", $"Completed follow-up '{fu.Subject}'");

        return (true, "Follow-up marked as completed.");
    }

    public async Task<(bool Success, string Message)> DeleteFollowUpAsync(int followUpId, string? currentUserId, string currentUserRole)
    {
        var query = _context.FollowUps.Where(f => f.FollowUpId == followUpId && f.IsActive);
        query = ApplyScope(query, currentUserId, currentUserRole);
        var fu = await query.FirstOrDefaultAsync();

        if (fu == null)
            return (false, "Follow-up not found or access denied.");

        fu.IsActive = false;
        fu.Status = FollowUpStatus.Cancelled;

        await _context.SaveChangesAsync();

        await _auditService.LogAsync("Delete/Cancel", "FollowUp", fu.FollowUpId.ToString(), fu.Subject, "Cancelled", "Success", $"Cancelled follow-up '{fu.Subject}'");

        return (true, "Follow-up cancelled.");
    }

    public async Task<bool> CanAccessFollowUpAsync(int followUpId, string? userId, string userRole)
    {
        if (userRole == AppRoles.Admin || userRole == AppRoles.Manager)
            return await _context.FollowUps.AnyAsync(f => f.FollowUpId == followUpId && f.IsActive);

        return await _context.FollowUps.AnyAsync(f => f.FollowUpId == followUpId && f.IsActive && f.AssignedToUserId == userId);
    }

    public async Task<List<FollowUpDto>> GetFollowUpDtosAsync(string? userId, string userRole)
    {
        var query = _context.FollowUps
            .Include(f => f.Customer)
            .Include(f => f.Lead)
            .Include(f => f.Opportunity)
            .Include(f => f.AssignedToUser)
            .Where(f => f.IsActive)
            .AsNoTracking();

        query = ApplyScope(query, userId, userRole);

        return await query.Select(f => new FollowUpDto
        {
            FollowUpId = f.FollowUpId,
            Subject = f.Subject,
            CustomerId = f.CustomerId,
            CustomerName = f.Customer != null ? f.Customer.CustomerName : null,
            LeadId = f.LeadId,
            LeadName = f.Lead != null ? f.Lead.LeadName : null,
            OpportunityId = f.OpportunityId,
            OpportunityName = f.Opportunity != null ? f.Opportunity.OpportunityName : null,
            FollowUpDate = f.FollowUpDate,
            FollowUpType = f.FollowUpType.ToString(),
            Status = f.Status.ToString(),
            Remarks = f.Remarks,
            AssignedToUserId = f.AssignedToUserId,
            AssignedToUserName = f.AssignedToUser != null ? f.AssignedToUser.FullName : null
        }).ToListAsync();
    }

    public async Task<(bool Success, string Message, FollowUpDto? FollowUp)> CreateFollowUpFromDtoAsync(CreateFollowUpDto dto, string? currentUserId, string currentUserRole)
    {
        if (!Enum.TryParse<FollowUpType>(dto.FollowUpType, true, out var fType))
            fType = FollowUpType.Call;

        if (!Enum.TryParse<FollowUpStatus>(dto.Status, true, out var fStatus))
            fStatus = FollowUpStatus.Planned;

        var vm = new FollowUpCreateEditViewModel
        {
            Subject = dto.Subject,
            CustomerId = dto.CustomerId,
            LeadId = dto.LeadId,
            OpportunityId = dto.OpportunityId,
            FollowUpDate = dto.FollowUpDate,
            FollowUpType = fType,
            Status = fStatus,
            Remarks = dto.Remarks,
            AssignedToUserId = dto.AssignedToUserId
        };

        var result = await CreateFollowUpAsync(vm, currentUserId, currentUserRole);
        if (!result.Success)
            return (false, result.Message, null);

        var created = await _context.FollowUps
            .Include(f => f.Customer)
            .Include(f => f.Lead)
            .Include(f => f.Opportunity)
            .Include(f => f.AssignedToUser)
            .Where(f => f.FollowUpId == result.FollowUpId)
            .Select(f => new FollowUpDto
            {
                FollowUpId = f.FollowUpId,
                Subject = f.Subject,
                CustomerId = f.CustomerId,
                CustomerName = f.Customer != null ? f.Customer.CustomerName : null,
                LeadId = f.LeadId,
                LeadName = f.Lead != null ? f.Lead.LeadName : null,
                OpportunityId = f.OpportunityId,
                OpportunityName = f.Opportunity != null ? f.Opportunity.OpportunityName : null,
                FollowUpDate = f.FollowUpDate,
                FollowUpType = f.FollowUpType.ToString(),
                Status = f.Status.ToString(),
                Remarks = f.Remarks,
                AssignedToUserId = f.AssignedToUserId,
                AssignedToUserName = f.AssignedToUser != null ? f.AssignedToUser.FullName : null
            }).FirstOrDefaultAsync();

        return (true, result.Message, created);
    }
}
