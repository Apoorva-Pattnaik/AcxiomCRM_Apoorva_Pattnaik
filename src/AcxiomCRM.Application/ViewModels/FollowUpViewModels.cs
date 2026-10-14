using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Domain.Enums;

namespace AcxiomCRM.Application.ViewModels;

public class FollowUpListViewModel
{
    public List<FollowUpItemViewModel> FollowUps { get; set; } = new();
    public List<FollowUpItemViewModel> UpcomingFollowUps { get; set; } = new();
    public List<FollowUpItemViewModel> OverdueFollowUps { get; set; } = new();

    public FollowUpStatus? StatusFilter { get; set; }
    public FollowUpType? TypeFilter { get; set; }
    public string? AssignedUserFilter { get; set; }
    public DateTime? DateFilter { get; set; }
    public int? CustomerFilter { get; set; }
    public int? LeadFilter { get; set; }

    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int TotalItems { get; set; }
    public int PageSize { get; set; } = 10;
}

public class FollowUpItemViewModel
{
    public int FollowUpId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public int? LeadId { get; set; }
    public string? LeadName { get; set; }
    public int? OpportunityId { get; set; }
    public string? OpportunityName { get; set; }
    public DateTime FollowUpDate { get; set; }
    public FollowUpType FollowUpType { get; set; }
    public FollowUpStatus Status { get; set; }
    public string? Remarks { get; set; }
    public string? AssignedToUserName { get; set; }
    public DateTime CreatedDate { get; set; }
    public bool IsOverdue => Status == FollowUpStatus.Planned && FollowUpDate.Date < DateTime.UtcNow.Date;
}

public class FollowUpCreateEditViewModel
{
    public int FollowUpId { get; set; }

    [Required(ErrorMessage = "Subject is required.")]
    [StringLength(200, ErrorMessage = "Subject cannot exceed 200 characters.")]
    [Display(Name = "Subject / Objective")]
    public string Subject { get; set; } = string.Empty;

    [Display(Name = "Related Customer")]
    public int? CustomerId { get; set; }

    [Display(Name = "Related Lead")]
    public int? LeadId { get; set; }

    [Display(Name = "Related Opportunity")]
    public int? OpportunityId { get; set; }

    [Required(ErrorMessage = "Follow-up Date & Time is required.")]
    [DataType(DataType.DateTime)]
    [Display(Name = "Follow-Up Date & Time")]
    public DateTime FollowUpDate { get; set; } = DateTime.UtcNow.Date.AddDays(1).AddHours(10);

    [Required(ErrorMessage = "Follow-up Type is required.")]
    [Display(Name = "Activity Type")]
    public FollowUpType FollowUpType { get; set; } = FollowUpType.Call;

    [Required(ErrorMessage = "Status is required.")]
    [Display(Name = "Status")]
    public FollowUpStatus Status { get; set; } = FollowUpStatus.Planned;

    [StringLength(1000, ErrorMessage = "Remarks cannot exceed 1000 characters.")]
    [Display(Name = "Remarks / Meeting Notes")]
    [DataType(DataType.MultilineText)]
    public string? Remarks { get; set; }

    [Display(Name = "Assigned To")]
    public string? AssignedToUserId { get; set; }
}
