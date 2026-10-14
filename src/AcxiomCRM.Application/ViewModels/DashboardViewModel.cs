namespace AcxiomCRM.Application.ViewModels;

public class DashboardViewModel
{
    // Filter Info
    public string DateFilter { get; set; } = "ThisMonth"; // Today, ThisWeek, ThisMonth, Custom
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string UserRole { get; set; } = string.Empty;
    public string UserFullName { get; set; } = string.Empty;

    // 8 Required KPI Cards
    public int TotalCustomers { get; set; }
    public int TotalLeads { get; set; }
    public int OpenLeads { get; set; } // Leads not closed, lost, or converted
    public int TotalOpportunities { get; set; }
    public int OpenOpportunities { get; set; }
    public int WonOpportunities { get; set; }
    public int LostOpportunities { get; set; }
    public decimal TotalPipelineValue { get; set; } // Sum of eligible open opportunity amounts
    public decimal WeightedPipelineValue { get; set; } // Bonus metric: sum of (amount * prob / 100)

    // Additional Useful Dashboard Metrics
    public int PendingFollowUps { get; set; }
    public int OverdueFollowUps { get; set; }

    // Chart 1: Lead Status Breakdown
    public List<ChartDataPoint> LeadStatusChart { get; set; } = new();

    // Chart 2: Opportunity Pipeline Stage Breakdown
    public List<ChartDataPoint> OpportunityPipelineChart { get; set; } = new();

    // Chart 3: Monthly Sales (Historical outcome / closed won volume)
    public List<MonthlySalesDataPoint> MonthlySalesChart { get; set; } = new();

    // Upcoming and recent feeds
    public List<FollowUpItemViewModel> UrgentFollowUps { get; set; } = new();
    public List<ActivityItemViewModel> RecentActivities { get; set; } = new();
    public List<OpportunityItemViewModel> TopOpportunities { get; set; } = new();
}

public class ChartDataPoint
{
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Value { get; set; }
    public string Color { get; set; } = "#3B82F6";
}

public class MonthlySalesDataPoint
{
    public string Month { get; set; } = string.Empty;
    public decimal WonAmount { get; set; }
    public int DealsWonCount { get; set; }
}
