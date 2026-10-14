using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AcxiomCRM.Domain.Enums;

namespace AcxiomCRM.Domain.Entities;

public class Lead
{
    public int LeadId { get; set; }

    [Required]
    [MaxLength(50)]
    public string LeadCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string LeadName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [Phone]
    [MaxLength(20)]
    public string Phone { get; set; } = string.Empty;

    [MaxLength(150)]
    public string? CompanyName { get; set; }

    [MaxLength(100)]
    public string Source { get; set; } = "Website";

    public LeadStatus Status { get; set; } = LeadStatus.New;

    public LeadPriority Priority { get; set; } = LeadPriority.Medium;

    [Range(0, 1000000000, ErrorMessage = "Expected value must be within the valid range (0 - 1,000,000,000).")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal ExpectedValue { get; set; } = 0;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime? ModifiedDate { get; set; }

    public string? AssignedToUserId { get; set; }
    public ApplicationUser? AssignedToUser { get; set; }

    public int? ConvertedCustomerId { get; set; }
    public Customer? ConvertedCustomer { get; set; }

    public int? ConvertedOpportunityId { get; set; }
    public Opportunity? ConvertedOpportunity { get; set; }

    public DateTime? ConvertedDate { get; set; }

    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ICollection<Opportunity> Opportunities { get; set; } = new List<Opportunity>();
    public ICollection<FollowUp> FollowUps { get; set; } = new List<FollowUp>();
    public ICollection<Activity> Activities { get; set; } = new List<Activity>();
}
