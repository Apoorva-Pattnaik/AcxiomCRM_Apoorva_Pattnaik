using AcxiomCRM.Application.DTOs;
using AcxiomCRM.Application.ViewModels;
using AcxiomCRM.Domain.Entities;
using AcxiomCRM.Domain.Enums;

namespace AcxiomCRM.Application.Interfaces;

public interface ICustomerService
{
    Task<CustomerListViewModel> GetCustomersAsync(string? userId, string userRole, string? search, CustomerStatus? status, string? city, int page = 1, int pageSize = 10);
    Task<CustomerDetailsViewModel?> GetCustomerDetailsAsync(int customerId, string? userId, string userRole);
    Task<CustomerCreateEditViewModel?> GetCustomerForEditAsync(int customerId, string? userId, string userRole);
    Task<(bool Success, string Message, int CustomerId)> CreateCustomerAsync(CustomerCreateEditViewModel model, string? currentUserId, string currentUserRole);
    Task<(bool Success, string Message)> UpdateCustomerAsync(CustomerCreateEditViewModel model, string? currentUserId, string currentUserRole);
    Task<(bool Success, string Message)> DeactivateCustomerAsync(int customerId, string? currentUserId, string currentUserRole);
    Task<bool> CanAccessCustomerAsync(int customerId, string? userId, string userRole);
    Task<bool> IsEmailUniqueAsync(string email, int? customerId = null);
    Task<bool> IsPhoneUniqueAsync(string phone, int? customerId = null);

    // API methods
    Task<List<CustomerDto>> GetCustomerDtosAsync(string? userId, string userRole, string? search = null);
    Task<CustomerDto?> GetCustomerDtoByIdAsync(int customerId, string? userId, string userRole);
    Task<(bool Success, string Message, CustomerDto? Customer)> CreateCustomerFromDtoAsync(CreateCustomerDto dto, string? currentUserId, string currentUserRole);
}
