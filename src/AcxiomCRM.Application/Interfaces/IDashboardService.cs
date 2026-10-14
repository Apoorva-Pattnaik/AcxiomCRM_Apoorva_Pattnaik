using AcxiomCRM.Application.ViewModels;

namespace AcxiomCRM.Application.Interfaces;

public interface IDashboardService
{
    Task<DashboardViewModel> GetDashboardDataAsync(string? userId, string userRole, string dateFilter = "ThisMonth", DateTime? customStart = null, DateTime? customEnd = null);
}
