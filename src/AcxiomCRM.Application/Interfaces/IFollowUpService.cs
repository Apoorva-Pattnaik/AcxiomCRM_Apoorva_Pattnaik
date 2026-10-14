using AcxiomCRM.Application.DTOs;
using AcxiomCRM.Application.ViewModels;
using AcxiomCRM.Domain.Enums;

namespace AcxiomCRM.Application.Interfaces;

public interface IFollowUpService
{
    Task<FollowUpListViewModel> GetFollowUpsAsync(string? userId, string userRole, FollowUpStatus? status, FollowUpType? type, DateTime? date, int? customerId, int? leadId, string? assignedUserId, int page = 1, int pageSize = 10);
    Task<FollowUpCreateEditViewModel?> GetFollowUpForEditAsync(int followUpId, string? userId, string userRole);
    Task<(bool Success, string Message, int FollowUpId)> CreateFollowUpAsync(FollowUpCreateEditViewModel model, string? currentUserId, string currentUserRole);
    Task<(bool Success, string Message)> UpdateFollowUpAsync(FollowUpCreateEditViewModel model, string? currentUserId, string currentUserRole);
    Task<(bool Success, string Message)> CompleteFollowUpAsync(int followUpId, string? remarks, string? currentUserId, string currentUserRole);
    Task<(bool Success, string Message)> DeleteFollowUpAsync(int followUpId, string? currentUserId, string currentUserRole);
    Task<bool> CanAccessFollowUpAsync(int followUpId, string? userId, string userRole);

    // API methods
    Task<List<FollowUpDto>> GetFollowUpDtosAsync(string? userId, string userRole);
    Task<(bool Success, string Message, FollowUpDto? FollowUp)> CreateFollowUpFromDtoAsync(CreateFollowUpDto dto, string? currentUserId, string currentUserRole);
}
