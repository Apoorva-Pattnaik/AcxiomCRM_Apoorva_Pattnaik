using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AcxiomCRM.Domain.Enums;

namespace AcxiomCRM.Domain.Entities;

public class Opportunity
{
    public int OpportunityId { get; set; }

    [Required(ErrorMessage = "Opportunity Name is required.")]
    [MaxLength(150)]
    public string OpportunityName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Customer is required.")]
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public int? LeadId { get; set; }
    public Lead? Lead { get; set; }

    [Required(ErrorMessage = "Opportunity Amount is required.")]
    [Range(0.01, 1000000000, ErrorMessage = "Opportunity Amount must be greater than 0.")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Required(ErrorMessage = "Stage is required.")]
    public OpportunityStage Stage { get; set; } = OpportunityStage.Qualification;

    [Required(ErrorMessage = "Probability is required.")]
    [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")]
    public int Probability { get; set; } = 10;

    [Required(ErrorMessage = "Expected Close Date is required.")]
    [DataType(DataType.Date)]
    public DateTime ExpectedCloseDate { get; set; } = DateTime.UtcNow.AddDays(30);

    public OpportunityStatus Status { get; set; } = OpportunityStatus.Open;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime? ModifiedDate { get; set; }

    public string? AssignedToUserId { get; set; }
    public ApplicationUser? AssignedToUser { get; set; }

    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;

    [NotMapped]
    public decimal WeightedPipelineValue => (Amount * Probability) / 100m;

    // Navigation properties
    public ICollection<FollowUp> FollowUps { get; set; } = new List<FollowUp>();
}
