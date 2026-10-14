using AcxiomCRM.Application.ViewModels;
using AcxiomCRM.Domain.Enums;

namespace AcxiomCRM.Application.Interfaces;

public interface IReportService
{
    Task<CustomerReportViewModel> GetCustomerReportAsync(string? userId, string userRole, DateTime? fromDate, DateTime? toDate, CustomerStatus? status, string? ownerId);
    Task<LeadReportViewModel> GetLeadReportAsync(string? userId, string userRole, DateTime? fromDate, DateTime? toDate, LeadStatus? status, string? source, string? ownerId);
    Task<FollowUpReportViewModel> GetFollowUpReportAsync(string? userId, string userRole, DateTime? fromDate, DateTime? toDate, FollowUpStatus? status, FollowUpType? type, string? ownerId);
    Task<OpportunityReportViewModel> GetOpportunityReportAsync(string? userId, string userRole, DateTime? fromDate, DateTime? toDate, OpportunityStage? stage, OpportunityStatus? status, string? ownerId);
    Task<PipelineReportViewModel> GetPipelineReportAsync(string? userId, string userRole);
    Task<SalesConversionReportViewModel> GetSalesConversionReportAsync(string? userId, string userRole, DateTime? fromDate, DateTime? toDate);
    Task<UserActivityReportViewModel> GetUserActivityReportAsync(string? userId, string userRole, DateTime? fromDate, DateTime? toDate);
    Task<AuditReportViewModel> GetAuditReportAsync(string? userId, string userRole, string? filterUserId, string? entityName, string? action, DateTime? fromDate, DateTime? toDate, int page = 1, int pageSize = 50);
}
