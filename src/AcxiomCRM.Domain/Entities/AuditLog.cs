using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.Domain.Entities;

public class AuditLog
{
    public int AuditLogId { get; set; }

    [MaxLength(100)]
    public string? UserId { get; set; }

    [MaxLength(150)]
    public string? UserName { get; set; }

    [Required]
    [MaxLength(100)]
    public string Action { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string EntityName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? RecordId { get; set; }

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [MaxLength(100)]
    public string? IpAddress { get; set; }

    [MaxLength(50)]
    public string? Result { get; set; } = "Success";

    [MaxLength(2000)]
    public string? Details { get; set; }
}
