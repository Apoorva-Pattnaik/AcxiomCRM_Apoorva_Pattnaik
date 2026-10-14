using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Domain.Enums;

namespace AcxiomCRM.Domain.Entities;

public class Activity
{
    public int ActivityId { get; set; }

    [Required(ErrorMessage = "Activity type is required.")]
    public ActivityType ActivityType { get; set; } = ActivityType.Call;

    [Required(ErrorMessage = "Subject is required.")]
    [MaxLength(200)]
    public string Subject { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Activity date is required.")]
    public DateTime ActivityDate { get; set; } = DateTime.UtcNow;

    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public int? LeadId { get; set; }
    public Lead? Lead { get; set; }

    public string? AssignedToUserId { get; set; }
    public ApplicationUser? AssignedToUser { get; set; }

    public ActivityStatus Status { get; set; } = ActivityStatus.Pending;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;
}
