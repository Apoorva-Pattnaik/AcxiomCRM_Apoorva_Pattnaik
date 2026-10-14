using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Domain.Enums;

namespace AcxiomCRM.Domain.Entities;

public class FollowUp
{
    public int FollowUpId { get; set; }

    [Required(ErrorMessage = "Subject is required.")]
    [MaxLength(200)]
    public string Subject { get; set; } = string.Empty;

    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public int? LeadId { get; set; }
    public Lead? Lead { get; set; }

    public int? OpportunityId { get; set; }
    public Opportunity? Opportunity { get; set; }

    [Required(ErrorMessage = "Follow-up date is required.")]
    public DateTime FollowUpDate { get; set; } = DateTime.UtcNow.Date.AddDays(1);

    [Required(ErrorMessage = "Follow-up type is required.")]
    public FollowUpType FollowUpType { get; set; } = FollowUpType.Call;

    public FollowUpStatus Status { get; set; } = FollowUpStatus.Planned;

    [MaxLength(1000)]
    public string? Remarks { get; set; }

    public string? AssignedToUserId { get; set; }
    public ApplicationUser? AssignedToUser { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedDate { get; set; }

    public bool IsActive { get; set; } = true;
}
