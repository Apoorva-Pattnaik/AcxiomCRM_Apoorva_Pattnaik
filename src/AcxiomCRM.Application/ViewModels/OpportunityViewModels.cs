using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Domain.Enums;

namespace AcxiomCRM.Application.ViewModels;

public class OpportunityListViewModel
{
    public List<OpportunityItemViewModel> Opportunities { get; set; } = new();
    public string? SearchTerm { get; set; }
    public OpportunityStage? StageFilter { get; set; }
    public OpportunityStatus? StatusFilter { get; set; }
    public int? CustomerFilter { get; set; }
    public string? AssignedUserFilter { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int TotalItems { get; set; }
    public int PageSize { get; set; } = 10;

    public decimal TotalPipelineValue { get; set; }
    public decimal TotalWeightedPipelineValue { get; set; }
}

public class OpportunityItemViewModel
{
    public int OpportunityId { get; set; }
    public string OpportunityName { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int? LeadId { get; set; }
    public decimal Amount { get; set; }
    public OpportunityStage Stage { get; set; }
    public int Probability { get; set; }
    public decimal WeightedValue => (Amount * Probability) / 100m;
    public DateTime ExpectedCloseDate { get; set; }
    public OpportunityStatus Status { get; set; }
    public string? AssignedToUserName { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class OpportunityCreateEditViewModel
{
    public int OpportunityId { get; set; }

    [Required(ErrorMessage = "Opportunity Name is required.")]
    [StringLength(150, ErrorMessage = "Opportunity Name cannot exceed 150 characters.")]
    [Display(Name = "Opportunity Name")]
    public string OpportunityName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please select a Customer.")]
    [Display(Name = "Customer")]
    public int CustomerId { get; set; }

    [Display(Name = "Originating Lead (Optional)")]
    public int? LeadId { get; set; }

    [Required(ErrorMessage = "Opportunity Amount is required.")]
    [Range(0.01, 1000000000, ErrorMessage = "Opportunity Amount must be greater than 0.")]
    [Display(Name = "Amount (₹)")]
    public decimal Amount { get; set; }

    [Required(ErrorMessage = "Pipeline Stage is required.")]
    [Display(Name = "Stage")]
    public OpportunityStage Stage { get; set; } = OpportunityStage.Qualification;

    [Required(ErrorMessage = "Probability is required.")]
    [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")]
    [Display(Name = "Win Probability (%)")]
    public int Probability { get; set; } = 10;

    [Required(ErrorMessage = "Expected Close Date is required.")]
    [DataType(DataType.Date)]
    [Display(Name = "Expected Close Date")]
    public DateTime ExpectedCloseDate { get; set; } = DateTime.UtcNow.AddDays(30);

    [Required(ErrorMessage = "Status is required.")]
    [Display(Name = "Status")]
    public OpportunityStatus Status { get; set; } = OpportunityStatus.Open;

    [Display(Name = "Assigned Sales Executive")]
    public string? AssignedToUserId { get; set; }

    [Display(Name = "Notes / Strategy")]
    [DataType(DataType.MultilineText)]
    public string? Notes { get; set; }
}

public class OpportunityDetailsViewModel
{
    public int OpportunityId { get; set; }
    public string OpportunityName { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int? LeadId { get; set; }
    public string? LeadName { get; set; }
    public decimal Amount { get; set; }
    public OpportunityStage Stage { get; set; }
    public int Probability { get; set; }
    public decimal WeightedValue => (Amount * Probability) / 100m;
    public DateTime ExpectedCloseDate { get; set; }
    public OpportunityStatus Status { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public string? AssignedToUserName { get; set; }
    public string? Notes { get; set; }

    public List<FollowUpItemViewModel> FollowUps { get; set; } = new();
}
