using AcxiomCRM.Domain.Entities;

namespace AcxiomCRM.Application.Interfaces;

public interface IAuditService
{
    Task LogAsync(string action, string entityName, string? recordId = null, string? oldValue = null, string? newValue = null, string? result = "Success", string? details = null);
    Task LogSecurityEventAsync(string action, string? details = null, string? result = "Success", string? userId = null, string? userName = null);
    Task<List<AuditLog>> GetAuditLogsAsync(string? userId = null, string? entityName = null, string? action = null, DateTime? fromDate = null, DateTime? toDate = null, int page = 1, int pageSize = 50);
    Task<int> GetAuditLogsCountAsync(string? userId = null, string? entityName = null, string? action = null, DateTime? fromDate = null, DateTime? toDate = null);
}
