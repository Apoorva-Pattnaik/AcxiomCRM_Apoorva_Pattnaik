using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Application.Interfaces;
using AcxiomCRM.Application.ViewModels;
using AcxiomCRM.Domain.Entities;
using AcxiomCRM.Domain.Enums;

namespace AcxiomCRM.Application.Services;

public class ReportService : IReportService
{
    private readonly IApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public ReportService(IApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<CustomerReportViewModel> GetCustomerReportAsync(string? userId, string userRole, DateTime? fromDate, DateTime? toDate, CustomerStatus? status, string? ownerId)
    {
        var query = _context.Customers
            .Include(c => c.AssignedToUser)
            .Include(c => c.Opportunities)
            .Where(c => c.IsActive)
            .AsNoTracking();

        if (userRole == AppRoles.SalesExecutive && !string.IsNullOrEmpty(userId))
            query = query.Where(c => c.AssignedToUserId == userId);
        else if (!string.IsNullOrWhiteSpace(ownerId))
            query = query.Where(c => c.AssignedToUserId == ownerId);

        if (fromDate.HasValue)
            query = query.Where(c => c.CreatedDate >= fromDate.Value.Date);

        if (toDate.HasValue)
            query = query.Where(c => c.CreatedDate <= toDate.Value.Date.AddDays(1).AddTicks(-1));

        if (status.HasValue)
            query = query.Where(c => c.Status == status.Value);

        var list = await query.OrderByDescending(c => c.CreatedDate).ToListAsync();

        var rows = list.Select(c => new CustomerReportRow
        {
            CustomerCode = c.CustomerCode,
            CustomerName = c.CustomerName,
            Email = c.Email,
            Phone = c.Phone,
            CompanyName = c.CompanyName,
            Status = c.Status,
            OwnerName = c.AssignedToUser?.FullName ?? "Unassigned",
            CreatedDate = c.CreatedDate,
            OpportunitiesCount = c.Opportunities.Count(o => o.IsActive),
            TotalWonValue = c.Opportunities.Where(o => o.IsActive && o.Status == OpportunityStatus.Won).Sum(o => o.Amount)
        }).ToList();

        return new CustomerReportViewModel
        {
            FromDate = fromDate,
            ToDate = toDate,
            StatusFilter = status,
            OwnerFilter = ownerId,
            Rows = rows,
            TotalCustomers = rows.Count,
            ActiveCustomers = rows.Count(r => r.Status == CustomerStatus.Active)
        };
    }

    public async Task<LeadReportViewModel> GetLeadReportAsync(string? userId, string userRole, DateTime? fromDate, DateTime? toDate, LeadStatus? status, string? source, string? ownerId)
    {
        var query = _context.Leads
            .Include(l => l.AssignedToUser)
            .Include(l => l.ConvertedCustomer)
            .Where(l => l.IsActive)
            .AsNoTracking();

        if (userRole == AppRoles.SalesExecutive && !string.IsNullOrEmpty(userId))
            query = query.Where(l => l.AssignedToUserId == userId);
        else if (!string.IsNullOrWhiteSpace(ownerId))
            query = query.Where(l => l.AssignedToUserId == ownerId);

        if (fromDate.HasValue)
            query = query.Where(l => l.CreatedDate >= fromDate.Value.Date);

        if (toDate.HasValue)
            query = query.Where(l => l.CreatedDate <= toDate.Value.Date.AddDays(1).AddTicks(-1));

        if (status.HasValue)
            query = query.Where(l => l.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(source))
            query = query.Where(l => l.Source == source);

        var list = await query.OrderByDescending(l => l.CreatedDate).ToListAsync();

        var rows = list.Select(l => new LeadReportRow
        {
            LeadCode = l.LeadCode,
            LeadName = l.LeadName,
            Email = l.Email,
            CompanyName = l.CompanyName,
            Source = l.Source,
            Status = l.Status,
            Priority = l.Priority,
            ExpectedValue = l.ExpectedValue,
            OwnerName = l.AssignedToUser?.FullName ?? "Unassigned",
            CreatedDate = l.CreatedDate,
            IsConverted = l.Status == LeadStatus.Converted,
            ConvertedCustomerName = l.ConvertedCustomer?.CustomerName
        }).ToList();

        return new LeadReportViewModel
        {
            FromDate = fromDate,
            ToDate = toDate,
            StatusFilter = status,
            SourceFilter = source,
            OwnerFilter = ownerId,
            Rows = rows,
            TotalLeads = rows.Count,
            ConvertedCount = rows.Count(r => r.IsConverted),
            TotalExpectedValue = rows.Sum(r => r.ExpectedValue)
        };
    }

    public async Task<FollowUpReportViewModel> GetFollowUpReportAsync(string? userId, string userRole, DateTime? fromDate, DateTime? toDate, FollowUpStatus? status, FollowUpType? type, string? ownerId)
    {
        var query = _context.FollowUps
            .Include(f => f.Customer)
            .Include(f => f.Lead)
            .Include(f => f.AssignedToUser)
            .Where(f => f.IsActive)
            .AsNoTracking();

        if (userRole == AppRoles.SalesExecutive && !string.IsNullOrEmpty(userId))
            query = query.Where(f => f.AssignedToUserId == userId);
        else if (!string.IsNullOrWhiteSpace(ownerId))
            query = query.Where(f => f.AssignedToUserId == ownerId);

        if (fromDate.HasValue)
            query = query.Where(f => f.FollowUpDate >= fromDate.Value.Date);

        if (toDate.HasValue)
            query = query.Where(f => f.FollowUpDate <= toDate.Value.Date.AddDays(1).AddTicks(-1));

        if (status.HasValue)
            query = query.Where(f => f.Status == status.Value);

        if (type.HasValue)
            query = query.Where(f => f.FollowUpType == type.Value);

        var list = await query.OrderByDescending(f => f.FollowUpDate).ToListAsync();
        var today = DateTime.UtcNow.Date;

        var rows = list.Select(f => new FollowUpReportRow
        {
            FollowUpId = f.FollowUpId,
            Subject = f.Subject,
            FollowUpType = f.FollowUpType,
            Status = f.Status,
            FollowUpDate = f.FollowUpDate,
            RelatedTo = f.Customer != null ? $"Customer: {f.Customer.CustomerName}" : (f.Lead != null ? $"Lead: {f.Lead.LeadName}" : "N/A"),
            OwnerName = f.AssignedToUser?.FullName ?? "Unassigned",
            IsOverdue = f.Status == FollowUpStatus.Planned && f.FollowUpDate.Date < today
        }).ToList();

        return new FollowUpReportViewModel
        {
            FromDate = fromDate,
            ToDate = toDate,
            StatusFilter = status,
            TypeFilter = type,
            OwnerFilter = ownerId,
            Rows = rows,
            TotalFollowUps = rows.Count,
            PlannedCount = rows.Count(r => r.Status == FollowUpStatus.Planned),
            CompletedCount = rows.Count(r => r.Status == FollowUpStatus.Completed),
            MissedCount = rows.Count(r => r.Status == FollowUpStatus.Missed),
            OverdueCount = rows.Count(r => r.IsOverdue)
        };
    }

    public async Task<OpportunityReportViewModel> GetOpportunityReportAsync(string? userId, string userRole, DateTime? fromDate, DateTime? toDate, OpportunityStage? stage, OpportunityStatus? status, string? ownerId)
    {
        var query = _context.Opportunities
            .Include(o => o.Customer)
            .Include(o => o.AssignedToUser)
            .Where(o => o.IsActive)
            .AsNoTracking();

        if (userRole == AppRoles.SalesExecutive && !string.IsNullOrEmpty(userId))
            query = query.Where(o => o.AssignedToUserId == userId);
        else if (!string.IsNullOrWhiteSpace(ownerId))
            query = query.Where(o => o.AssignedToUserId == ownerId);

        if (fromDate.HasValue)
            query = query.Where(o => o.CreatedDate >= fromDate.Value.Date);

        if (toDate.HasValue)
            query = query.Where(o => o.CreatedDate <= toDate.Value.Date.AddDays(1).AddTicks(-1));

        if (stage.HasValue)
            query = query.Where(o => o.Stage == stage.Value);

        if (status.HasValue)
            query = query.Where(o => o.Status == status.Value);

        var list = await query.OrderByDescending(o => o.CreatedDate).ToListAsync();

        var rows = list.Select(o => new OpportunityReportRow
        {
            OpportunityId = o.OpportunityId,
            OpportunityName = o.OpportunityName,
            CustomerName = o.Customer.CustomerName,
            Amount = o.Amount,
            Stage = o.Stage,
            Probability = o.Probability,
            ExpectedCloseDate = o.ExpectedCloseDate,
            Status = o.Status,
            OwnerName = o.AssignedToUser?.FullName ?? "Unassigned"
        }).ToList();

        return new OpportunityReportViewModel
        {
            FromDate = fromDate,
            ToDate = toDate,
            StageFilter = stage,
            StatusFilter = status,
            OwnerFilter = ownerId,
            Rows = rows,
            TotalOpportunities = rows.Count,
            TotalAmount = rows.Sum(r => r.Amount),
            TotalWeightedValue = rows.Sum(r => r.WeightedValue)
        };
    }

    public async Task<PipelineReportViewModel> GetPipelineReportAsync(string? userId, string userRole)
    {
        var query = _context.Opportunities
            .Include(o => o.Customer)
            .Include(o => o.AssignedToUser)
            .Where(o => o.IsActive && o.Status == OpportunityStatus.Open)
            .AsNoTracking();

        if (userRole == AppRoles.SalesExecutive && !string.IsNullOrEmpty(userId))
            query = query.Where(o => o.AssignedToUserId == userId);

        var list = await query.ToListAsync();

        var rows = list.Select(o => new OpportunityReportRow
        {
            OpportunityId = o.OpportunityId,
            OpportunityName = o.OpportunityName,
            CustomerName = o.Customer.CustomerName,
            Amount = o.Amount,
            Stage = o.Stage,
            Probability = o.Probability,
            ExpectedCloseDate = o.ExpectedCloseDate,
            Status = o.Status,
            OwnerName = o.AssignedToUser?.FullName ?? "Unassigned"
        }).ToList();

        var stageSummaries = list.GroupBy(o => o.Stage)
            .Select(g => new StagePipelineSummary
            {
                Stage = g.Key,
                DealCount = g.Count(),
                TotalAmount = g.Sum(x => x.Amount),
                WeightedAmount = g.Sum(x => x.WeightedPipelineValue)
            }).OrderBy(s => (int)s.Stage).ToList();

        var ownerSummaries = list.GroupBy(o => o.AssignedToUser?.FullName ?? "Unassigned")
            .Select(g => new OwnerPipelineSummary
            {
                OwnerName = g.Key,
                DealCount = g.Count(),
                TotalAmount = g.Sum(x => x.Amount),
                WeightedAmount = g.Sum(x => x.WeightedPipelineValue)
            }).OrderByDescending(s => s.TotalAmount).ToList();

        return new PipelineReportViewModel
        {
            TotalPipelineValue = list.Sum(o => o.Amount),
            TotalWeightedValue = list.Sum(o => o.WeightedPipelineValue),
            TotalOpenDeals = list.Count,
            StageSummaries = stageSummaries,
            OwnerSummaries = ownerSummaries,
            Opportunities = rows
        };
    }

    public async Task<SalesConversionReportViewModel> GetSalesConversionReportAsync(string? userId, string userRole, DateTime? fromDate, DateTime? toDate)
    {
        var leadQuery = _context.Leads.Where(l => l.IsActive).AsNoTracking();
        var oppQuery = _context.Opportunities.Where(o => o.IsActive).AsNoTracking();

        if (userRole == AppRoles.SalesExecutive && !string.IsNullOrEmpty(userId))
        {
            leadQuery = leadQuery.Where(l => l.AssignedToUserId == userId);
            oppQuery = oppQuery.Where(o => o.AssignedToUserId == userId);
        }

        if (fromDate.HasValue)
        {
            leadQuery = leadQuery.Where(l => l.CreatedDate >= fromDate.Value.Date);
            oppQuery = oppQuery.Where(o => o.CreatedDate >= fromDate.Value.Date);
        }

        if (toDate.HasValue)
        {
            leadQuery = leadQuery.Where(l => l.CreatedDate <= toDate.Value.Date.AddDays(1).AddTicks(-1));
            oppQuery = oppQuery.Where(o => o.CreatedDate <= toDate.Value.Date.AddDays(1).AddTicks(-1));
        }

        var totalLeads = await leadQuery.CountAsync();
        var convertedLeads = await leadQuery.CountAsync(l => l.Status == LeadStatus.Converted);

        var totalOpps = await oppQuery.CountAsync();
        var wonOpps = await oppQuery.Where(o => o.Status == OpportunityStatus.Won).ToListAsync();
        var lostOpps = await oppQuery.Where(o => o.Status == OpportunityStatus.Lost).ToListAsync();

        return new SalesConversionReportViewModel
        {
            TotalLeadsReceived = totalLeads,
            LeadsConverted = convertedLeads,
            TotalOpportunitiesCreated = totalOpps,
            OpportunitiesWon = wonOpps.Count,
            OpportunitiesLost = lostOpps.Count,
            TotalWonRevenue = wonOpps.Sum(o => o.Amount),
            TotalLostRevenue = lostOpps.Sum(o => o.Amount)
        };
    }

    public async Task<UserActivityReportViewModel> GetUserActivityReportAsync(string? userId, string userRole, DateTime? fromDate, DateTime? toDate)
    {
        var users = await _userManager.Users.Where(u => u.IsActive).ToListAsync();
        var actQuery = _context.Activities.Where(a => a.IsActive).AsNoTracking();

        if (fromDate.HasValue)
            actQuery = actQuery.Where(a => a.ActivityDate >= fromDate.Value.Date);

        if (toDate.HasValue)
            actQuery = actQuery.Where(a => a.ActivityDate <= toDate.Value.Date.AddDays(1).AddTicks(-1));

        var activities = await actQuery.ToListAsync();

        var summaries = new List<UserActivitySummary>();
        foreach (var u in users)
        {
            var uActs = activities.Where(a => a.AssignedToUserId == u.Id).ToList();
            summaries.Add(new UserActivitySummary
            {
                UserId = u.Id,
                UserName = u.UserName ?? string.Empty,
                FullName = u.FullName,
                Role = u.RoleName,
                CallsLogged = uActs.Count(a => a.ActivityType == ActivityType.Call),
                MeetingsLogged = uActs.Count(a => a.ActivityType == ActivityType.Meeting),
                EmailsSent = uActs.Count(a => a.ActivityType == ActivityType.Email),
                TasksCompleted = uActs.Count(a => a.ActivityType == ActivityType.Task),
                TotalActivities = uActs.Count
            });
        }

        var recent = await _context.Activities
            .Include(a => a.Customer)
            .Include(a => a.Lead)
            .Include(a => a.AssignedToUser)
            .Where(a => a.IsActive)
            .OrderByDescending(a => a.ActivityDate)
            .Take(15)
            .Select(a => new ActivityItemViewModel
            {
                ActivityId = a.ActivityId,
                ActivityType = a.ActivityType,
                Subject = a.Subject,
                Description = a.Description,
                ActivityDate = a.ActivityDate,
                CustomerName = a.Customer != null ? a.Customer.CustomerName : null,
                LeadName = a.Lead != null ? a.Lead.LeadName : null,
                AssignedToUserName = a.AssignedToUser != null ? a.AssignedToUser.FullName : "Unassigned",
                Status = a.Status
            }).ToListAsync();

        return new UserActivityReportViewModel
        {
            UserSummaries = summaries.OrderByDescending(s => s.TotalActivities).ToList(),
            RecentActivities = recent
        };
    }

    public async Task<AuditReportViewModel> GetAuditReportAsync(string? userId, string userRole, string? filterUserId, string? entityName, string? action, DateTime? fromDate, DateTime? toDate, int page = 1, int pageSize = 50)
    {
        var query = _context.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(filterUserId))
            query = query.Where(a => a.UserId == filterUserId || (a.UserName != null && a.UserName.Contains(filterUserId)));

        if (!string.IsNullOrWhiteSpace(entityName))
            query = query.Where(a => a.EntityName == entityName);

        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(a => a.Action == action);

        if (fromDate.HasValue)
            query = query.Where(a => a.CreatedDate >= fromDate.Value.Date);

        if (toDate.HasValue)
            query = query.Where(a => a.CreatedDate <= toDate.Value.Date.AddDays(1).AddTicks(-1));

        var total = await query.CountAsync();
        var totalPages = (int)Math.Ceiling((double)total / pageSize);
        if (totalPages < 1) totalPages = 1;

        var logs = await query.OrderByDescending(a => a.CreatedDate)
                              .Skip((page - 1) * pageSize)
                              .Take(pageSize)
                              .ToListAsync();

        return new AuditReportViewModel
        {
            UserId = filterUserId,
            EntityName = entityName,
            Action = action,
            FromDate = fromDate,
            ToDate = toDate,
            Logs = logs,
            CurrentPage = page,
            TotalPages = totalPages,
            TotalCount = total
        };
    }
}
