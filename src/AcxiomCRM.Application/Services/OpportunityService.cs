using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Application.DTOs;
using AcxiomCRM.Application.Interfaces;
using AcxiomCRM.Application.ViewModels;
using AcxiomCRM.Domain.Entities;
using AcxiomCRM.Domain.Enums;
using AcxiomCRM.Infrastructure.Data;

namespace AcxiomCRM.Application.Services;

public class OpportunityService : IOpportunityService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public OpportunityService(ApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    private IQueryable<Opportunity> ApplyScope(IQueryable<Opportunity> query, string? userId, string userRole)
    {
        if (userRole == AppRoles.SalesExecutive && !string.IsNullOrEmpty(userId))
        {
            return query.Where(o => o.AssignedToUserId == userId);
        }
        return query;
    }

    public async Task<OpportunityListViewModel> GetOpportunitiesAsync(string? userId, string userRole, string? search, OpportunityStage? stage, OpportunityStatus? status, int? customerId, string? assignedUserId, int page = 1, int pageSize = 10)
    {
        var query = _context.Opportunities
            .Include(o => o.Customer)
            .Include(o => o.AssignedToUser)
            .Where(o => o.IsActive)
            .AsNoTracking();

        query = ApplyScope(query, userId, userRole);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(o => o.OpportunityName.ToLower().Contains(s) ||
                                     o.Customer.CustomerName.ToLower().Contains(s));
        }

        if (stage.HasValue)
        {
            query = query.Where(o => o.Stage == stage.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(o => o.Status == status.Value);
        }

        if (customerId.HasValue)
        {
            query = query.Where(o => o.CustomerId == customerId.Value);
        }

        if (!string.IsNullOrWhiteSpace(assignedUserId) && userRole != AppRoles.SalesExecutive)
        {
            query = query.Where(o => o.AssignedToUserId == assignedUserId);
        }

        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
        if (totalPages < 1) totalPages = 1;
        if (page < 1) page = 1;

        // Total pipeline value for open deals in scope
        var openDeals = await query.Where(o => o.Status == OpportunityStatus.Open).ToListAsync();
        var totalPipelineVal = openDeals.Sum(o => o.Amount);
        var totalWeightedVal = openDeals.Sum(o => o.WeightedPipelineValue);

        var items = await query.OrderByDescending(o => o.CreatedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new OpportunityItemViewModel
            {
                OpportunityId = o.OpportunityId,
                OpportunityName = o.OpportunityName,
                CustomerId = o.CustomerId,
                CustomerName = o.Customer.CustomerName,
                LeadId = o.LeadId,
                Amount = o.Amount,
                Stage = o.Stage,
                Probability = o.Probability,
                ExpectedCloseDate = o.ExpectedCloseDate,
                Status = o.Status,
                AssignedToUserName = o.AssignedToUser != null ? o.AssignedToUser.FullName : "Unassigned",
                CreatedDate = o.CreatedDate
            })
            .ToListAsync();

        return new OpportunityListViewModel
        {
            Opportunities = items,
            SearchTerm = search,
            StageFilter = stage,
            StatusFilter = status,
            CustomerFilter = customerId,
            AssignedUserFilter = assignedUserId,
            CurrentPage = page,
            TotalPages = totalPages,
            TotalItems = totalItems,
            PageSize = pageSize,
            TotalPipelineValue = totalPipelineVal,
            TotalWeightedPipelineValue = totalWeightedVal
        };
    }

    public async Task<OpportunityDetailsViewModel?> GetOpportunityDetailsAsync(int opportunityId, string? userId, string userRole)
    {
        var query = _context.Opportunities
            .Include(o => o.Customer)
            .Include(o => o.Lead)
            .Include(o => o.AssignedToUser)
            .Include(o => o.FollowUps)
                .ThenInclude(f => f.AssignedToUser)
            .Where(o => o.OpportunityId == opportunityId && o.IsActive)
            .AsNoTracking();

        query = ApplyScope(query, userId, userRole);
        var o = await query.FirstOrDefaultAsync();
        if (o == null) return null;

        return new OpportunityDetailsViewModel
        {
            OpportunityId = o.OpportunityId,
            OpportunityName = o.OpportunityName,
            CustomerId = o.CustomerId,
            CustomerName = o.Customer.CustomerName,
            LeadId = o.LeadId,
            LeadName = o.Lead?.LeadName,
            Amount = o.Amount,
            Stage = o.Stage,
            Probability = o.Probability,
            ExpectedCloseDate = o.ExpectedCloseDate,
            Status = o.Status,
            CreatedDate = o.CreatedDate,
            ModifiedDate = o.ModifiedDate,
            AssignedToUserName = o.AssignedToUser?.FullName ?? "Unassigned",
            Notes = o.Notes,
            FollowUps = o.FollowUps.Where(f => f.IsActive).OrderByDescending(f => f.FollowUpDate).Select(f => new FollowUpItemViewModel
            {
                FollowUpId = f.FollowUpId,
                Subject = f.Subject,
                OpportunityId = o.OpportunityId,
                OpportunityName = o.OpportunityName,
                FollowUpDate = f.FollowUpDate,
                FollowUpType = f.FollowUpType,
                Status = f.Status,
                Remarks = f.Remarks,
                AssignedToUserName = f.AssignedToUser?.FullName ?? "Unassigned",
                CreatedDate = f.CreatedDate
            }).ToList()
        };
    }

    public async Task<OpportunityCreateEditViewModel?> GetOpportunityForEditAsync(int opportunityId, string? userId, string userRole)
    {
        var query = _context.Opportunities.Where(o => o.OpportunityId == opportunityId && o.IsActive);
        query = ApplyScope(query, userId, userRole);
        var o = await query.FirstOrDefaultAsync();
        if (o == null) return null;

        return new OpportunityCreateEditViewModel
        {
            OpportunityId = o.OpportunityId,
            OpportunityName = o.OpportunityName,
            CustomerId = o.CustomerId,
            LeadId = o.LeadId,
            Amount = o.Amount,
            Stage = o.Stage,
            Probability = o.Probability,
            ExpectedCloseDate = o.ExpectedCloseDate,
            Status = o.Status,
            AssignedToUserId = o.AssignedToUserId,
            Notes = o.Notes
        };
    }

    public async Task<(bool Success, string Message, int OpportunityId)> CreateOpportunityAsync(OpportunityCreateEditViewModel model, string? currentUserId, string currentUserRole)
    {
        if (string.IsNullOrWhiteSpace(model.OpportunityName))
            return (false, "Opportunity Name is required.", 0);

        if (model.CustomerId <= 0)
            return (false, "Please select a valid Customer.", 0);

        if (model.Amount <= 0)
            return (false, "Opportunity Amount must be greater than 0.", 0);

        if (model.Probability < 0 || model.Probability > 100)
            return (false, "Probability must be between 0 and 100.", 0);

        if (model.ExpectedCloseDate.Date < DateTime.UtcNow.Date && model.Status == OpportunityStatus.Open)
            return (false, "Expected Close Date cannot be in the past for an active opportunity.", 0);

        // Verify customer exists
        var customerExists = await _context.Customers.AnyAsync(c => c.CustomerId == model.CustomerId && c.IsActive);
        if (!customerExists)
            return (false, "Selected Customer does not exist.", 0);

        // Auto-sync status with stage
        var status = model.Status;
        if (model.Stage == OpportunityStage.Won)
        {
            status = OpportunityStatus.Won;
            model.Probability = 100;
        }
        else if (model.Stage == OpportunityStage.Lost)
        {
            status = OpportunityStatus.Lost;
            model.Probability = 0;
        }

        var opp = new Opportunity
        {
            OpportunityName = model.OpportunityName.Trim(),
            CustomerId = model.CustomerId,
            LeadId = model.LeadId,
            Amount = model.Amount,
            Stage = model.Stage,
            Probability = model.Probability,
            ExpectedCloseDate = model.ExpectedCloseDate,
            Status = status,
            Notes = model.Notes?.Trim(),
            CreatedDate = DateTime.UtcNow,
            AssignedToUserId = currentUserRole == AppRoles.SalesExecutive ? currentUserId : model.AssignedToUserId,
            IsActive = true
        };

        _context.Opportunities.Add(opp);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync("Create", "Opportunity", opp.OpportunityId.ToString(), null, opp.OpportunityName, "Success", $"Created opportunity '{opp.OpportunityName}' with amount ₹{opp.Amount}");

        return (true, "Opportunity created successfully.", opp.OpportunityId);
    }

    public async Task<(bool Success, string Message)> UpdateOpportunityAsync(OpportunityCreateEditViewModel model, string? currentUserId, string currentUserRole)
    {
        var query = _context.Opportunities.Where(o => o.OpportunityId == model.OpportunityId && o.IsActive);
        query = ApplyScope(query, currentUserId, currentUserRole);
        var opp = await query.FirstOrDefaultAsync();

        if (opp == null)
            return (false, "Opportunity not found or access denied.");

        if (model.Amount <= 0)
            return (false, "Opportunity Amount must be greater than 0.");

        if (model.Probability < 0 || model.Probability > 100)
            return (false, "Probability must be between 0 and 100.");

        // If keeping or setting to Open/Active, close date cannot be in the past
        if ((model.Status == OpportunityStatus.Open || (model.Stage != OpportunityStage.Won && model.Stage != OpportunityStage.Lost)) && 
            model.ExpectedCloseDate.Date < DateTime.UtcNow.Date)
        {
            return (false, "Expected Close Date cannot be in the past for an active opportunity.");
        }

        var oldStage = opp.Stage.ToString();
        var oldAmount = opp.Amount;

        // Auto-sync status with stage
        var status = model.Status;
        if (model.Stage == OpportunityStage.Won)
        {
            status = OpportunityStatus.Won;
            model.Probability = 100;
        }
        else if (model.Stage == OpportunityStage.Lost)
        {
            status = OpportunityStatus.Lost;
            model.Probability = 0;
        }

        opp.OpportunityName = model.OpportunityName.Trim();
        opp.CustomerId = model.CustomerId;
        opp.LeadId = model.LeadId;
        opp.Amount = model.Amount;
        opp.Stage = model.Stage;
        opp.Probability = model.Probability;
        opp.ExpectedCloseDate = model.ExpectedCloseDate;
        opp.Status = status;
        opp.Notes = model.Notes?.Trim();
        opp.ModifiedDate = DateTime.UtcNow;

        if (currentUserRole != AppRoles.SalesExecutive)
        {
            opp.AssignedToUserId = model.AssignedToUserId;
        }

        await _context.SaveChangesAsync();

        await _auditService.LogAsync("Update", "Opportunity", opp.OpportunityId.ToString(), $"Stage: {oldStage}, Amt: {oldAmount}", $"Stage: {opp.Stage}, Amt: {opp.Amount}", "Success", $"Updated opportunity '{opp.OpportunityName}'");

        return (true, "Opportunity updated successfully.");
    }

    public async Task<(bool Success, string Message)> DeleteOpportunityAsync(int opportunityId, string? currentUserId, string currentUserRole)
    {
        var query = _context.Opportunities.Where(o => o.OpportunityId == opportunityId && o.IsActive);
        query = ApplyScope(query, currentUserId, currentUserRole);
        var opp = await query.FirstOrDefaultAsync();

        if (opp == null)
            return (false, "Opportunity not found or access denied.");

        opp.IsActive = false;
        opp.Status = OpportunityStatus.Lost;
        opp.ModifiedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _auditService.LogAsync("Delete/Deactivate", "Opportunity", opp.OpportunityId.ToString(), opp.OpportunityName, "Deactivated", "Success", $"Deactivated opportunity '{opp.OpportunityName}'");

        return (true, "Opportunity deactivated successfully.");
    }

    public async Task<bool> CanAccessOpportunityAsync(int opportunityId, string? userId, string userRole)
    {
        if (userRole == AppRoles.Admin || userRole == AppRoles.Manager)
            return await _context.Opportunities.AnyAsync(o => o.OpportunityId == opportunityId && o.IsActive);

        return await _context.Opportunities.AnyAsync(o => o.OpportunityId == opportunityId && o.IsActive && o.AssignedToUserId == userId);
    }

    public async Task<List<OpportunityDto>> GetOpportunityDtosAsync(string? userId, string userRole, string? search = null)
    {
        var query = _context.Opportunities
            .Include(o => o.Customer)
            .Include(o => o.AssignedToUser)
            .Where(o => o.IsActive)
            .AsNoTracking();

        query = ApplyScope(query, userId, userRole);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(o => o.OpportunityName.ToLower().Contains(s) || o.Customer.CustomerName.ToLower().Contains(s));
        }

        return await query.Select(o => new OpportunityDto
        {
            OpportunityId = o.OpportunityId,
            OpportunityName = o.OpportunityName,
            CustomerId = o.CustomerId,
            CustomerName = o.Customer.CustomerName,
            LeadId = o.LeadId,
            Amount = o.Amount,
            Stage = o.Stage.ToString(),
            Probability = o.Probability,
            WeightedValue = (o.Amount * o.Probability) / 100m,
            ExpectedCloseDate = o.ExpectedCloseDate,
            Status = o.Status.ToString(),
            CreatedDate = o.CreatedDate,
            AssignedToUserId = o.AssignedToUserId,
            AssignedToUserName = o.AssignedToUser != null ? o.AssignedToUser.FullName : null,
            Notes = o.Notes
        }).ToListAsync();
    }

    public async Task<(bool Success, string Message, OpportunityDto? Opportunity)> CreateOpportunityFromDtoAsync(CreateOpportunityDto dto, string? currentUserId, string currentUserRole)
    {
        if (!Enum.TryParse<OpportunityStage>(dto.Stage, true, out var stage))
            stage = OpportunityStage.Qualification;

        var vm = new OpportunityCreateEditViewModel
        {
            OpportunityName = dto.OpportunityName,
            CustomerId = dto.CustomerId,
            LeadId = dto.LeadId,
            Amount = dto.Amount,
            Stage = stage,
            Probability = dto.Probability,
            ExpectedCloseDate = dto.ExpectedCloseDate,
            AssignedToUserId = dto.AssignedToUserId,
            Notes = dto.Notes
        };

        var result = await CreateOpportunityAsync(vm, currentUserId, currentUserRole);
        if (!result.Success)
            return (false, result.Message, null);

        var created = await _context.Opportunities
            .Include(o => o.Customer)
            .Include(o => o.AssignedToUser)
            .Where(o => o.OpportunityId == result.OpportunityId)
            .Select(o => new OpportunityDto
            {
                OpportunityId = o.OpportunityId,
                OpportunityName = o.OpportunityName,
                CustomerId = o.CustomerId,
                CustomerName = o.Customer.CustomerName,
                LeadId = o.LeadId,
                Amount = o.Amount,
                Stage = o.Stage.ToString(),
                Probability = o.Probability,
                WeightedValue = (o.Amount * o.Probability) / 100m,
                ExpectedCloseDate = o.ExpectedCloseDate,
                Status = o.Status.ToString(),
                CreatedDate = o.CreatedDate,
                AssignedToUserId = o.AssignedToUserId,
                AssignedToUserName = o.AssignedToUser != null ? o.AssignedToUser.FullName : null,
                Notes = o.Notes
            }).FirstOrDefaultAsync();

        return (true, result.Message, created);
    }

    public async Task<PipelineReportDto> GetPipelineSummaryAsync(string? userId, string userRole)
    {
        var query = _context.Opportunities.Where(o => o.IsActive && o.Status == OpportunityStatus.Open).AsNoTracking();
        query = ApplyScope(query, userId, userRole);

        var list = await query.ToListAsync();
        var totalVal = list.Sum(o => o.Amount);
        var totalWeighted = list.Sum(o => o.WeightedPipelineValue);

        var stages = list.GroupBy(o => o.Stage)
            .Select(g => new PipelineStageSummaryDto
            {
                Stage = g.Key.ToString(),
                Count = g.Count(),
                TotalAmount = g.Sum(x => x.Amount),
                WeightedAmount = g.Sum(x => x.WeightedPipelineValue)
            }).ToList();

        return new PipelineReportDto
        {
            TotalPipelineValue = totalVal,
            TotalWeightedPipelineValue = totalWeighted,
            TotalOpenOpportunities = list.Count,
            Stages = stages
        };
    }
}
