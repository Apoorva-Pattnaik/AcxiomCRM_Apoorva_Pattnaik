using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Application.Interfaces;
using AcxiomCRM.Domain.Entities;
using AcxiomCRM.Infrastructure.Data;

namespace AcxiomCRM.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LogAsync(string action, string entityName, string? recordId = null, string? oldValue = null, string? newValue = null, string? result = "Success", string? details = null)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var userId = httpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        var userName = httpContext?.User?.Identity?.Name ?? "System";
        var ipAddress = httpContext?.Connection?.RemoteIpAddress?.ToString() ?? "127.0.0.1";

        var log = new AuditLog
        {
            UserId = userId,
            UserName = userName,
            Action = action,
            EntityName = entityName,
            RecordId = recordId,
            OldValue = oldValue,
            NewValue = newValue,
            IpAddress = ipAddress,
            Result = result,
            Details = details,
            CreatedDate = DateTime.UtcNow
        };

        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    public async Task LogSecurityEventAsync(string action, string? details = null, string? result = "Success", string? userId = null, string? userName = null)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var resolvedUserId = userId ?? httpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        var resolvedUserName = userName ?? httpContext?.User?.Identity?.Name ?? "Anonymous";
        var ipAddress = httpContext?.Connection?.RemoteIpAddress?.ToString() ?? "127.0.0.1";

        var log = new AuditLog
        {
            UserId = resolvedUserId,
            UserName = resolvedUserName,
            Action = action,
            EntityName = "Security",
            RecordId = resolvedUserId,
            IpAddress = ipAddress,
            Result = result,
            Details = details,
            CreatedDate = DateTime.UtcNow
        };

        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    public async Task<List<AuditLog>> GetAuditLogsAsync(string? userId = null, string? entityName = null, string? action = null, DateTime? fromDate = null, DateTime? toDate = null, int page = 1, int pageSize = 50)
    {
        var query = _context.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(userId))
            query = query.Where(a => a.UserId == userId || (a.UserName != null && a.UserName.Contains(userId)));

        if (!string.IsNullOrWhiteSpace(entityName))
            query = query.Where(a => a.EntityName == entityName);

        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(a => a.Action == action);

        if (fromDate.HasValue)
            query = query.Where(a => a.CreatedDate >= fromDate.Value.Date);

        if (toDate.HasValue)
            query = query.Where(a => a.CreatedDate <= toDate.Value.Date.AddDays(1).AddTicks(-1));

        return await query.OrderByDescending(a => a.CreatedDate)
                          .Skip((page - 1) * pageSize)
                          .Take(pageSize)
                          .ToListAsync();
    }

    public async Task<int> GetAuditLogsCountAsync(string? userId = null, string? entityName = null, string? action = null, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = _context.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(userId))
            query = query.Where(a => a.UserId == userId || (a.UserName != null && a.UserName.Contains(userId)));

        if (!string.IsNullOrWhiteSpace(entityName))
            query = query.Where(a => a.EntityName == entityName);

        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(a => a.Action == action);

        if (fromDate.HasValue)
            query = query.Where(a => a.CreatedDate >= fromDate.Value.Date);

        if (toDate.HasValue)
            query = query.Where(a => a.CreatedDate <= toDate.Value.Date.AddDays(1).AddTicks(-1));

        return await query.CountAsync();
    }
}
