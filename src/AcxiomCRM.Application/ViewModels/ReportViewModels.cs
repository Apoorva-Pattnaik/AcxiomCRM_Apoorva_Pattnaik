using AcxiomCRM.Domain.Entities;
using AcxiomCRM.Domain.Enums;

namespace AcxiomCRM.Application.ViewModels;

public class CustomerReportViewModel
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public CustomerStatus? StatusFilter { get; set; }
    public string? OwnerFilter { get; set; }
    public List<CustomerReportRow> Rows { get; set; } = new();
    public int TotalCustomers { get; set; }
    public int ActiveCustomers { get; set; }
}

public class CustomerReportRow
{
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public CustomerStatus Status { get; set; }
    public string? OwnerName { get; set; }
    public DateTime CreatedDate { get; set; }
    public int OpportunitiesCount { get; set; }
    public decimal TotalWonValue { get; set; }
}

public class LeadReportViewModel
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public LeadStatus? StatusFilter { get; set; }
    public string? SourceFilter { get; set; }
    public string? OwnerFilter { get; set; }
    public List<LeadReportRow> Rows { get; set; } = new();
    public int TotalLeads { get; set; }
    public int ConvertedCount { get; set; }
    public decimal TotalExpectedValue { get; set; }
    public double ConversionRate => TotalLeads > 0 ? Math.Round((double)ConvertedCount / TotalLeads * 100, 1) : 0;
}

public class LeadReportRow
{
    public string LeadCode { get; set; } = string.Empty;
    public string LeadName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string Source { get; set; } = string.Empty;
    public LeadStatus Status { get; set; }
    public LeadPriority Priority { get; set; }
    public decimal ExpectedValue { get; set; }
    public string? OwnerName { get; set; }
    public DateTime CreatedDate { get; set; }
    public bool IsConverted { get; set; }
    public string? ConvertedCustomerName { get; set; }
}

public class FollowUpReportViewModel
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public FollowUpStatus? StatusFilter { get; set; }
    public FollowUpType? TypeFilter { get; set; }
    public string? OwnerFilter { get; set; }
    public List<FollowUpReportRow> Rows { get; set; } = new();
    public int TotalFollowUps { get; set; }
    public int PlannedCount { get; set; }
    public int CompletedCount { get; set; }
    public int MissedCount { get; set; }
    public int OverdueCount { get; set; }
}

public class FollowUpReportRow
{
    public int FollowUpId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public FollowUpType FollowUpType { get; set; }
    public FollowUpStatus Status { get; set; }
    public DateTime FollowUpDate { get; set; }
    public string? RelatedTo { get; set; }
    public string? OwnerName { get; set; }
    public bool IsOverdue { get; set; }
}

public class OpportunityReportViewModel
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public OpportunityStage? StageFilter { get; set; }
    public OpportunityStatus? StatusFilter { get; set; }
    public string? OwnerFilter { get; set; }
    public List<OpportunityReportRow> Rows { get; set; } = new();
    public int TotalOpportunities { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal TotalWeightedValue { get; set; }
}

public class OpportunityReportRow
{
    public int OpportunityId { get; set; }
    public string OpportunityName { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public OpportunityStage Stage { get; set; }
    public int Probability { get; set; }
    public decimal WeightedValue => (Amount * Probability) / 100m;
    public DateTime ExpectedCloseDate { get; set; }
    public OpportunityStatus Status { get; set; }
    public string? OwnerName { get; set; }
}

public class PipelineReportViewModel
{
    public decimal TotalPipelineValue { get; set; }
    public decimal TotalWeightedValue { get; set; }
    public int TotalOpenDeals { get; set; }
    public List<StagePipelineSummary> StageSummaries { get; set; } = new();
    public List<OwnerPipelineSummary> OwnerSummaries { get; set; } = new();
    public List<OpportunityReportRow> Opportunities { get; set; } = new();
}

public class StagePipelineSummary
{
    public OpportunityStage Stage { get; set; }
    public int DealCount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal WeightedAmount { get; set; }
}

public class OwnerPipelineSummary
{
    public string OwnerName { get; set; } = string.Empty;
    public int DealCount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal WeightedAmount { get; set; }
}

public class SalesConversionReportViewModel
{
    public int TotalLeadsReceived { get; set; }
    public int LeadsConverted { get; set; }
    public double LeadConversionRate => TotalLeadsReceived > 0 ? Math.Round((double)LeadsConverted / TotalLeadsReceived * 100, 1) : 0;
    
    public int TotalOpportunitiesCreated { get; set; }
    public int OpportunitiesWon { get; set; }
    public int OpportunitiesLost { get; set; }
    public double WinRate => TotalOpportunitiesCreated > 0 ? Math.Round((double)OpportunitiesWon / TotalOpportunitiesCreated * 100, 1) : 0;

    public decimal TotalWonRevenue { get; set; }
    public decimal TotalLostRevenue { get; set; }
    public decimal AverageDealSize => OpportunitiesWon > 0 ? Math.Round(TotalWonRevenue / OpportunitiesWon, 2) : 0;
}

public class UserActivityReportViewModel
{
    public List<UserActivitySummary> UserSummaries { get; set; } = new();
    public List<ActivityItemViewModel> RecentActivities { get; set; } = new();
}

public class UserActivitySummary
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int CallsLogged { get; set; }
    public int MeetingsLogged { get; set; }
    public int EmailsSent { get; set; }
    public int TasksCompleted { get; set; }
    public int TotalActivities { get; set; }
}

public class AuditReportViewModel
{
    public string? UserId { get; set; }
    public string? EntityName { get; set; }
    public string? Action { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public List<AuditLog> Logs { get; set; } = new();
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int TotalCount { get; set; }
}
