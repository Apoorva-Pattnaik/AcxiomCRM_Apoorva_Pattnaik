using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Application.Interfaces;
using AcxiomCRM.Application.ViewModels;
using AcxiomCRM.Domain.Entities;
using AcxiomCRM.Domain.Enums;

namespace AcxiomCRM.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly IApplicationDbContext _context;

    public DashboardService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardViewModel> GetDashboardDataAsync(string? userId, string userRole, string dateFilter = "ThisMonth", DateTime? customStart = null, DateTime? customEnd = null)
    {
        var now = DateTime.UtcNow;
        DateTime? startDate = null;
        DateTime? endDate = now;

        switch (dateFilter)
        {
            case "Today":
                startDate = now.Date;
                break;
            case "ThisWeek":
                var diff = (7 + (now.DayOfWeek - DayOfWeek.Monday)) % 7;
                startDate = now.Date.AddDays(-1 * diff);
                break;
            case "ThisMonth":
                startDate = new DateTime(now.Year, now.Month, 1);
                break;
            case "Custom":
                startDate = customStart;
                endDate = customEnd.HasValue ? customEnd.Value.Date.AddDays(1).AddTicks(-1) : now;
                break;
            default:
                dateFilter = "ThisMonth";
                startDate = new DateTime(now.Year, now.Month, 1);
                break;
        }

        // Base scoped queries
        var customerQuery = _context.Customers.Where(c => c.IsActive).AsNoTracking();
        var leadQuery = _context.Leads.Where(l => l.IsActive).AsNoTracking();
        var oppQuery = _context.Opportunities.Where(o => o.IsActive).AsNoTracking();
        var followUpQuery = _context.FollowUps.Where(f => f.IsActive).AsNoTracking();
        var actQuery = _context.Activities.Where(a => a.IsActive).AsNoTracking();

        if (userRole == AppRoles.SalesExecutive && !string.IsNullOrEmpty(userId))
        {
            customerQuery = customerQuery.Where(c => c.AssignedToUserId == userId);
            leadQuery = leadQuery.Where(l => l.AssignedToUserId == userId);
            oppQuery = oppQuery.Where(o => o.AssignedToUserId == userId);
            followUpQuery = followUpQuery.Where(f => f.AssignedToUserId == userId);
            actQuery = actQuery.Where(a => a.AssignedToUserId == userId);
        }

        // Date-filtered queries where applicable
        var filteredCustQuery = customerQuery;
        var filteredLeadQuery = leadQuery;
        var filteredOppQuery = oppQuery;

        if (startDate.HasValue)
        {
            filteredCustQuery = filteredCustQuery.Where(c => c.CreatedDate >= startDate.Value && c.CreatedDate <= endDate);
            filteredLeadQuery = filteredLeadQuery.Where(l => l.CreatedDate >= startDate.Value && l.CreatedDate <= endDate);
            filteredOppQuery = filteredOppQuery.Where(o => o.CreatedDate >= startDate.Value && o.CreatedDate <= endDate);
        }

        // 8 Required KPI Cards
        // 1. Total Customers
        var totalCustomers = await customerQuery.CountAsync();

        // 2. Total Leads
        var totalLeads = await leadQuery.CountAsync();

        // 3. Open Leads (Leads that are not closed/lost/converted)
        var openLeads = await leadQuery.CountAsync(l => l.Status == LeadStatus.New || l.Status == LeadStatus.Contacted || l.Status == LeadStatus.Qualified);

        // 4. Total Opportunities
        var totalOpportunities = await oppQuery.CountAsync();

        // 5. Open Opportunities
        var openOpportunities = await oppQuery.CountAsync(o => o.Status == OpportunityStatus.Open);

        // 6. Won Opportunities
        var wonOpportunities = await oppQuery.CountAsync(o => o.Status == OpportunityStatus.Won);

        // 7. Lost Opportunities
        var lostOpportunities = await oppQuery.CountAsync(o => o.Status == OpportunityStatus.Lost);

        // 8. Total Pipeline Value (Sum of eligible open opportunity amounts)
        var openDealsList = await oppQuery.Where(o => o.Status == OpportunityStatus.Open).ToListAsync();
        var totalPipelineValue = openDealsList.Sum(o => o.Amount);
        var weightedPipelineValue = openDealsList.Sum(o => o.WeightedPipelineValue);

        // Pending & Overdue FollowUps
        var today = DateTime.UtcNow.Date;
        var pendingFollowUps = await followUpQuery.CountAsync(f => f.Status == FollowUpStatus.Planned);
        var overdueFollowUps = await followUpQuery.CountAsync(f => f.Status == FollowUpStatus.Planned && f.FollowUpDate.Date < today);

        // Chart 1: Lead Status Chart
        var leadStatuses = new[]
        {
            (LeadStatus.New, "New", "#3B82F6"),
            (LeadStatus.Contacted, "Contacted", "#F59E0B"),
            (LeadStatus.Qualified, "Qualified", "#10B981"),
            (LeadStatus.Lost, "Lost", "#EF4444"),
            (LeadStatus.Converted, "Converted", "#8B5CF6")
        };

        var leadChartData = new List<ChartDataPoint>();
        foreach (var (status, label, color) in leadStatuses)
        {
            var count = await leadQuery.CountAsync(l => l.Status == status);
            leadChartData.Add(new ChartDataPoint
            {
                Label = label,
                Count = count,
                Color = color
            });
        }

        // Chart 2: Opportunity Pipeline Stage Chart
        var oppStages = new[]
        {
            (OpportunityStage.Qualification, "Qualification", "#60A5FA"),
            (OpportunityStage.Proposal, "Proposal", "#FBBF24"),
            (OpportunityStage.Negotiation, "Negotiation", "#F97316"),
            (OpportunityStage.Won, "Won", "#10B981"),
            (OpportunityStage.Lost, "Lost", "#EF4444")
        };

        var oppChartData = new List<ChartDataPoint>();
        foreach (var (stage, label, color) in oppStages)
        {
            var stageDeals = await oppQuery.Where(o => o.Stage == stage).ToListAsync();
            oppChartData.Add(new ChartDataPoint
            {
                Label = label,
                Count = stageDeals.Count,
                Value = stageDeals.Sum(x => x.Amount),
                Color = color
            });
        }

        // Chart 3: Monthly Sales (Last 6 Months Won Deals)
        var monthlySalesData = new List<MonthlySalesDataPoint>();
        for (int i = 5; i >= 0; i--)
        {
            var mDate = now.AddMonths(-i);
            var mStart = new DateTime(mDate.Year, mDate.Month, 1);
            var mEnd = mStart.AddMonths(1).AddTicks(-1);

            var mWonDeals = await oppQuery
                .Where(o => o.Status == OpportunityStatus.Won && o.CreatedDate >= mStart && o.CreatedDate <= mEnd)
                .ToListAsync();

            monthlySalesData.Add(new MonthlySalesDataPoint
            {
                Month = mDate.ToString("MMM yyyy"),
                WonAmount = mWonDeals.Sum(d => d.Amount),
                DealsWonCount = mWonDeals.Count
            });
        }

        // Urgent followups
        var urgentFollowUps = await followUpQuery
            .Include(f => f.Customer)
            .Include(f => f.Lead)
            .Include(f => f.Opportunity)
            .Include(f => f.AssignedToUser)
            .Where(f => f.Status == FollowUpStatus.Planned)
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
                AssignedToUserName = f.AssignedToUser != null ? f.AssignedToUser.FullName : "Unassigned"
            })
            .ToListAsync();

        // Recent activities
        var recentActs = await actQuery
            .Include(a => a.Customer)
            .Include(a => a.Lead)
            .Include(a => a.AssignedToUser)
            .OrderByDescending(a => a.ActivityDate)
            .Take(5)
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
            })
            .ToListAsync();

        return new DashboardViewModel
        {
            DateFilter = dateFilter,
            StartDate = startDate,
            EndDate = endDate,
            UserRole = userRole,
            TotalCustomers = totalCustomers,
            TotalLeads = totalLeads,
            OpenLeads = openLeads,
            TotalOpportunities = totalOpportunities,
            OpenOpportunities = openOpportunities,
            WonOpportunities = wonOpportunities,
            LostOpportunities = lostOpportunities,
            TotalPipelineValue = totalPipelineValue,
            WeightedPipelineValue = weightedPipelineValue,
            PendingFollowUps = pendingFollowUps,
            OverdueFollowUps = overdueFollowUps,
            LeadStatusChart = leadChartData,
            OpportunityPipelineChart = oppChartData,
            MonthlySalesChart = monthlySalesData,
            UrgentFollowUps = urgentFollowUps,
            RecentActivities = recentActs
        };
    }
}
