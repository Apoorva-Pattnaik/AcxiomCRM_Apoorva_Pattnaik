using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.Application.Interfaces;
using AcxiomCRM.Domain.Enums;

namespace AcxiomCRM.Web.Controllers;

[Authorize(Roles = AppRoles.Admin)]
public class AuditLogsController : Controller
{
    private readonly IAuditService _auditService;

    public AuditLogsController(IAuditService auditService)
    {
        _auditService = auditService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? userId, string? entityName, string? action, DateTime? fromDate, DateTime? toDate, int page = 1)
    {
        const int pageSize = 20;
        var totalCount = await _auditService.GetAuditLogsCountAsync(userId, entityName, action, fromDate, toDate);
        var logs = await _auditService.GetAuditLogsAsync(userId, entityName, action, fromDate, toDate, page, pageSize);

        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
        if (totalPages < 1) totalPages = 1;

        ViewBag.UserId = userId;
        ViewBag.EntityName = entityName;
        ViewBag.Action = action;
        ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
        ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalCount = totalCount;

        return View(logs);
    }
}
