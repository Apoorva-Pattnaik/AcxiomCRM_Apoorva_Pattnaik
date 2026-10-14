using AcxiomCRM.Application.DTOs;
using AcxiomCRM.Application.ViewModels;
using AcxiomCRM.Domain.Enums;

namespace AcxiomCRM.Application.Interfaces;

public interface IOpportunityService
{
    Task<OpportunityListViewModel> GetOpportunitiesAsync(string? userId, string userRole, string? search, OpportunityStage? stage, OpportunityStatus? status, int? customerId, string? assignedUserId, int page = 1, int pageSize = 10);
    Task<OpportunityDetailsViewModel?> GetOpportunityDetailsAsync(int opportunityId, string? userId, string userRole);
    Task<OpportunityCreateEditViewModel?> GetOpportunityForEditAsync(int opportunityId, string? userId, string userRole);
    Task<(bool Success, string Message, int OpportunityId)> CreateOpportunityAsync(OpportunityCreateEditViewModel model, string? currentUserId, string currentUserRole);
    Task<(bool Success, string Message)> UpdateOpportunityAsync(OpportunityCreateEditViewModel model, string? currentUserId, string currentUserRole);
    Task<(bool Success, string Message)> DeleteOpportunityAsync(int opportunityId, string? currentUserId, string currentUserRole);
    Task<bool> CanAccessOpportunityAsync(int opportunityId, string? userId, string userRole);

    // API methods
    Task<List<OpportunityDto>> GetOpportunityDtosAsync(string? userId, string userRole, string? search = null);
    Task<(bool Success, string Message, OpportunityDto? Opportunity)> CreateOpportunityFromDtoAsync(CreateOpportunityDto dto, string? currentUserId, string currentUserRole);
    Task<PipelineReportDto> GetPipelineSummaryAsync(string? userId, string userRole);
}
