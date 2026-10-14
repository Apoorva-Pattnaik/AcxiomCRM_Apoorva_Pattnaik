using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Domain.Enums;

namespace AcxiomCRM.Application.ViewModels;

public class ActivityListViewModel
{
    public List<ActivityItemViewModel> Activities { get; set; } = new();
    public ActivityType? TypeFilter { get; set; }
    public ActivityStatus? StatusFilter { get; set; }
    public string? AssignedUserFilter { get; set; }
    public DateTime? DateFilter { get; set; }
    public string? SearchTerm { get; set; }

    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int TotalItems { get; set; }
    public int PageSize { get; set; } = 10;
}

public class ActivityItemViewModel
{
    public int ActivityId { get; set; }
    public ActivityType ActivityType { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime ActivityDate { get; set; }
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public int? LeadId { get; set; }
    public string? LeadName { get; set; }
    public string? AssignedToUserName { get; set; }
    public ActivityStatus Status { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class ActivityCreateEditViewModel
{
    public int ActivityId { get; set; }

    [Required(ErrorMessage = "Activity Type is required.")]
    [Display(Name = "Activity Type")]
    public ActivityType ActivityType { get; set; } = ActivityType.Call;

    [Required(ErrorMessage = "Subject is required.")]
    [StringLength(200, ErrorMessage = "Subject cannot exceed 200 characters.")]
    [Display(Name = "Subject")]
    public string Subject { get; set; } = string.Empty;

    [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters.")]
    [Display(Name = "Description / Summary")]
    [DataType(DataType.MultilineText)]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Activity Date is required.")]
    [DataType(DataType.DateTime)]
    [Display(Name = "Activity Date")]
    public DateTime ActivityDate { get; set; } = DateTime.UtcNow;

    [Display(Name = "Related Customer (Optional)")]
    public int? CustomerId { get; set; }

    [Display(Name = "Related Lead (Optional)")]
    public int? LeadId { get; set; }

    [Required(ErrorMessage = "Status is required.")]
    [Display(Name = "Status")]
    public ActivityStatus Status { get; set; } = ActivityStatus.Completed;

    [Display(Name = "Assigned To")]
    public string? AssignedToUserId { get; set; }
}
