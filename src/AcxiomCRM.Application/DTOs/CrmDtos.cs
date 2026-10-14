using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.Application.DTOs;

public class LoginRequestDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class LoginResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public string? FullName { get; set; }
    public string? Role { get; set; }
}

public class LogoutResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class CustomerDto
{
    public int CustomerId { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string Status { get; set; } = "Active";
    public string? Notes { get; set; }
    public DateTime CreatedDate { get; set; }
    public string? CreatedBy { get; set; }
    public string? AssignedToUserId { get; set; }
    public string? AssignedToUserName { get; set; }
}

public class CreateCustomerDto
{
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

    public string? CompanyName { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Notes { get; set; }
    public string? AssignedToUserId { get; set; }
}

public class LeadDto
{
    public int LeadId { get; set; }
    public string LeadCode { get; set; } = string.Empty;
    public string LeadName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string Source { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public decimal ExpectedValue { get; set; }
    public DateTime CreatedDate { get; set; }
    public string? AssignedToUserId { get; set; }
    public string? AssignedToUserName { get; set; }
    public int? ConvertedCustomerId { get; set; }
    public int? ConvertedOpportunityId { get; set; }
    public string? Notes { get; set; }
}

public class CreateLeadDto
{
    [Required]
    [MaxLength(150)]
    public string LeadName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [Phone]
    public string Phone { get; set; } = string.Empty;

    public string? CompanyName { get; set; }
    public string Source { get; set; } = "Website";
    public string Status { get; set; } = "New";
    public string Priority { get; set; } = "Medium";

    [Range(0, 1000000000)]
    public decimal ExpectedValue { get; set; }

    public string? AssignedToUserId { get; set; }
    public string? Notes { get; set; }
}

public class OpportunityDto
{
    public int OpportunityId { get; set; }
    public string OpportunityName { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int? LeadId { get; set; }
    public decimal Amount { get; set; }
    public string Stage { get; set; } = string.Empty;
    public int Probability { get; set; }
    public decimal WeightedValue { get; set; }
    public DateTime ExpectedCloseDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public string? AssignedToUserId { get; set; }
    public string? AssignedToUserName { get; set; }
    public string? Notes { get; set; }
}

public class CreateOpportunityDto
{
    [Required]
    [MaxLength(150)]
    public string OpportunityName { get; set; } = string.Empty;

    [Required]
    public int CustomerId { get; set; }

    public int? LeadId { get; set; }

    [Required]
    [Range(0.01, 1000000000)]
    public decimal Amount { get; set; }

    public string Stage { get; set; } = "Qualification";

    [Range(0, 100)]
    public int Probability { get; set; } = 10;

    [Required]
    public DateTime ExpectedCloseDate { get; set; }

    public string? AssignedToUserId { get; set; }
    public string? Notes { get; set; }
}

public class FollowUpDto
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
    public string FollowUpType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Remarks { get; set; }
    public string? AssignedToUserId { get; set; }
    public string? AssignedToUserName { get; set; }
}

public class CreateFollowUpDto
{
    [Required]
    [MaxLength(200)]
    public string Subject { get; set; } = string.Empty;

    public int? CustomerId { get; set; }
    public int? LeadId { get; set; }
    public int? OpportunityId { get; set; }

    [Required]
    public DateTime FollowUpDate { get; set; }

    public string FollowUpType { get; set; } = "Call";
    public string Status { get; set; } = "Planned";
    public string? Remarks { get; set; }
    public string? AssignedToUserId { get; set; }
}

public class PipelineStageSummaryDto
{
    public string Stage { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal WeightedAmount { get; set; }
}

public class PipelineReportDto
{
    public decimal TotalPipelineValue { get; set; }
    public decimal TotalWeightedPipelineValue { get; set; }
    public int TotalOpenOpportunities { get; set; }
    public List<PipelineStageSummaryDto> Stages { get; set; } = new();
}
