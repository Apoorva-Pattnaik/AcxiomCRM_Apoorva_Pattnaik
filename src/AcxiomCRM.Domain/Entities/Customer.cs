using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Domain.Enums;

namespace AcxiomCRM.Domain.Entities;

public class Customer
{
    public int CustomerId { get; set; }

    [Required]
    [MaxLength(50)]
    public string CustomerCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string CustomerName { get; set; } = string.Empty;

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

    [MaxLength(300)]
    public string? Address { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(100)]
    public string? State { get; set; }

    public CustomerStatus Status { get; set; } = CustomerStatus.Active;

    public string? Notes { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime? ModifiedDate { get; set; }

    [MaxLength(100)]
    public string? CreatedBy { get; set; }

    public string? AssignedToUserId { get; set; }
    public ApplicationUser? AssignedToUser { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ICollection<Opportunity> Opportunities { get; set; } = new List<Opportunity>();
    public ICollection<FollowUp> FollowUps { get; set; } = new List<FollowUp>();
    public ICollection<Activity> Activities { get; set; } = new List<Activity>();
    public ICollection<Lead> ConvertedLeads { get; set; } = new List<Lead>();
}
