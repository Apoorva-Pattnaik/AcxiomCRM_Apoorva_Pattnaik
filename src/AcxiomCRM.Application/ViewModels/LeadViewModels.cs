using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Domain.Enums;

namespace AcxiomCRM.Application.ViewModels;

public class LeadListViewModel
{
    public List<LeadItemViewModel> Leads { get; set; } = new();
    public string? SearchTerm { get; set; }
    public LeadStatus? StatusFilter { get; set; }
    public string? SourceFilter { get; set; }
    public string? AssignedUserFilter { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int TotalItems { get; set; }
    public int PageSize { get; set; } = 10;
}

public class LeadItemViewModel
{
    public int LeadId { get; set; }
    public string LeadCode { get; set; } = string.Empty;
    public string LeadName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string Source { get; set; } = string.Empty;
    public LeadStatus Status { get; set; }
    public LeadPriority Priority { get; set; }
    public decimal ExpectedValue { get; set; }
    public string? AssignedToUserName { get; set; }
    public DateTime CreatedDate { get; set; }
    public int? ConvertedCustomerId { get; set; }
    public int? ConvertedOpportunityId { get; set; }
}

public class LeadCreateEditViewModel
{
    public int LeadId { get; set; }

    [Display(Name = "Lead Code")]
    public string? LeadCode { get; set; }

    [Required(ErrorMessage = "Lead Name is required.")]
    [StringLength(150, ErrorMessage = "Lead Name cannot exceed 150 characters.")]
    [Display(Name = "Lead / Contact Name")]
    public string LeadName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [StringLength(150, ErrorMessage = "Email cannot exceed 150 characters.")]
    [Display(Name = "Email Address")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone number is required.")]
    [RegularExpression(@"^(\+?[0-9\s\-]{7,20})$", ErrorMessage = "Enter a valid phone number (digits, optional +, spaces, dashes, 7-20 chars).")]
    [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters.")]
    [Display(Name = "Phone Number")]
    public string Phone { get; set; } = string.Empty;

    [StringLength(150, ErrorMessage = "Company Name cannot exceed 150 characters.")]
    [Display(Name = "Company Name")]
    public string? CompanyName { get; set; }

    [Required(ErrorMessage = "Lead Source is required.")]
    [StringLength(100)]
    [Display(Name = "Lead Source")]
    public string Source { get; set; } = "Website";

    [Required(ErrorMessage = "Status is required.")]
    [Display(Name = "Status")]
    public LeadStatus Status { get; set; } = LeadStatus.New;

    [Required(ErrorMessage = "Priority is required.")]
    [Display(Name = "Priority")]
    public LeadPriority Priority { get; set; } = LeadPriority.Medium;

    [Required(ErrorMessage = "Expected Value is required.")]
    [Range(0, 1000000000, ErrorMessage = "Expected Value must be between 0 and 1,000,000,000.")]
    [Display(Name = "Expected Value (₹)")]
    public decimal ExpectedValue { get; set; }

    [Display(Name = "Assigned Sales Executive")]
    public string? AssignedToUserId { get; set; }

    [Display(Name = "Notes / Description")]
    [DataType(DataType.MultilineText)]
    public string? Notes { get; set; }
}

public class LeadConversionViewModel
{
    public int LeadId { get; set; }
    public string LeadCode { get; set; } = string.Empty;
    public string LeadName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public decimal ExpectedValue { get; set; }

    [Display(Name = "Create Customer from this Lead")]
    public bool CreateCustomer { get; set; } = true;

    [Required(ErrorMessage = "Customer Name is required.")]
    [Display(Name = "Customer Name")]
    public string CustomerName { get; set; } = string.Empty;

    [Display(Name = "Create Opportunity from this Lead")]
    public bool CreateOpportunity { get; set; } = true;

    [Display(Name = "Opportunity Name")]
    public string? OpportunityName { get; set; }

    [Range(0.01, 1000000000, ErrorMessage = "Opportunity Amount must be greater than 0.")]
    [Display(Name = "Opportunity Amount (₹)")]
    public decimal OpportunityAmount { get; set; }

    [Display(Name = "Opportunity Stage")]
    public OpportunityStage OpportunityStage { get; set; } = OpportunityStage.Qualification;

    [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")]
    [Display(Name = "Probability (%)")]
    public int Probability { get; set; } = 25;

    [DataType(DataType.Date)]
    [Display(Name = "Expected Close Date")]
    public DateTime ExpectedCloseDate { get; set; } = DateTime.UtcNow.AddDays(30);

    [Display(Name = "Assigned Sales Executive")]
    public string? AssignedToUserId { get; set; }
}

public class LeadDetailsViewModel
{
    public int LeadId { get; set; }
    public string LeadCode { get; set; } = string.Empty;
    public string LeadName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string Source { get; set; } = string.Empty;
    public LeadStatus Status { get; set; }
    public LeadPriority Priority { get; set; }
    public decimal ExpectedValue { get; set; }
    public DateTime CreatedDate { get; set; }
    public string? AssignedToUserName { get; set; }
    public string? Notes { get; set; }
    public int? ConvertedCustomerId { get; set; }
    public string? ConvertedCustomerName { get; set; }
    public int? ConvertedOpportunityId { get; set; }
    public string? ConvertedOpportunityName { get; set; }
    public DateTime? ConvertedDate { get; set; }

    public List<FollowUpItemViewModel> FollowUps { get; set; } = new();
    public List<ActivityItemViewModel> Activities { get; set; } = new();
}
