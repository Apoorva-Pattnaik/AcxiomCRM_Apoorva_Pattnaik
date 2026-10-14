using AcxiomCRM.Application.DTOs;
using AcxiomCRM.Application.ViewModels;
using AcxiomCRM.Domain.Enums;

namespace AcxiomCRM.Application.Interfaces;

public interface ILeadService
{
    Task<LeadListViewModel> GetLeadsAsync(string? userId, string userRole, string? search, LeadStatus? status, string? source, string? assignedUserId, int page = 1, int pageSize = 10);
    Task<LeadDetailsViewModel?> GetLeadDetailsAsync(int leadId, string? userId, string userRole);
    Task<LeadCreateEditViewModel?> GetLeadForEditAsync(int leadId, string? userId, string userRole);
    Task<(bool Success, string Message, int LeadId)> CreateLeadAsync(LeadCreateEditViewModel model, string? currentUserId, string currentUserRole);
    Task<(bool Success, string Message)> UpdateLeadAsync(LeadCreateEditViewModel model, string? currentUserId, string currentUserRole);
    Task<(bool Success, string Message)> UpdateLeadStatusAsync(int leadId, LeadStatus newStatus, string? currentUserId, string currentUserRole);
    Task<LeadConversionViewModel?> GetLeadForConversionAsync(int leadId, string? userId, string userRole);
    Task<(bool Success, string Message, int? CustomerId, int? OpportunityId)> ConvertLeadAsync(LeadConversionViewModel model, string? currentUserId, string currentUserRole);
    Task<(bool Success, string Message)> DeleteLeadAsync(int leadId, string? currentUserId, string currentUserRole);
    Task<bool> CanAccessLeadAsync(int leadId, string? userId, string userRole);

    // API methods
    Task<List<LeadDto>> GetLeadDtosAsync(string? userId, string userRole, string? search = null);
    Task<(bool Success, string Message, LeadDto? Lead)> CreateLeadFromDtoAsync(CreateLeadDto dto, string? currentUserId, string currentUserRole);
}
