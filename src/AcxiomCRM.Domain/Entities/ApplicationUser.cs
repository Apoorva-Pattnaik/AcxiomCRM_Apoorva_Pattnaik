using Microsoft.AspNetCore.Identity;

namespace AcxiomCRM.Domain.Entities;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string? Department { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}
