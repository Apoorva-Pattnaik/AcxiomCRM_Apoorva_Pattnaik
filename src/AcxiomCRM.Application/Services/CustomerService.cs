using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Application.DTOs;
using AcxiomCRM.Application.Interfaces;
using AcxiomCRM.Application.ViewModels;
using AcxiomCRM.Domain.Entities;
using AcxiomCRM.Domain.Enums;

namespace AcxiomCRM.Application.Services;

public class CustomerService : ICustomerService
{
    private readonly IApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public CustomerService(IApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    private IQueryable<Customer> ApplyScope(IQueryable<Customer> query, string? userId, string userRole)
    {
        if (userRole == AppRoles.SalesExecutive && !string.IsNullOrEmpty(userId))
        {
            return query.Where(c => c.AssignedToUserId == userId);
        }
        return query;
    }

    public async Task<CustomerListViewModel> GetCustomersAsync(string? userId, string userRole, string? search, CustomerStatus? status, string? city, int page = 1, int pageSize = 10)
    {
        var query = _context.Customers
            .Include(c => c.AssignedToUser)
            .Where(c => c.IsActive)
            .AsNoTracking();

        query = ApplyScope(query, userId, userRole);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(c => c.CustomerName.ToLower().Contains(s) ||
                                     c.Email.ToLower().Contains(s) ||
                                     c.Phone.Contains(s) ||
                                     (c.CompanyName != null && c.CompanyName.ToLower().Contains(s)));
        }

        if (status.HasValue)
        {
            query = query.Where(c => c.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(city))
        {
            var ct = city.Trim().ToLower();
            query = query.Where(c => c.City != null && c.City.ToLower().Contains(ct));
        }

        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
        if (totalPages < 1) totalPages = 1;
        if (page < 1) page = 1;

        var items = await query.OrderByDescending(c => c.CreatedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CustomerItemViewModel
            {
                CustomerId = c.CustomerId,
                CustomerCode = c.CustomerCode,
                CustomerName = c.CustomerName,
                Email = c.Email,
                Phone = c.Phone,
                CompanyName = c.CompanyName,
                City = c.City,
                State = c.State,
                Status = c.Status,
                AssignedToUserName = c.AssignedToUser != null ? c.AssignedToUser.FullName : "Unassigned",
                CreatedDate = c.CreatedDate
            })
            .ToListAsync();

        return new CustomerListViewModel
        {
            Customers = items,
            SearchTerm = search,
            StatusFilter = status,
            CityFilter = city,
            CurrentPage = page,
            TotalPages = totalPages,
            TotalItems = totalItems,
            PageSize = pageSize
        };
    }

    public async Task<CustomerDetailsViewModel?> GetCustomerDetailsAsync(int customerId, string? userId, string userRole)
    {
        var query = _context.Customers
            .Include(c => c.AssignedToUser)
            .Include(c => c.Opportunities)
            .Include(c => c.FollowUps)
                .ThenInclude(f => f.AssignedToUser)
            .Include(c => c.Activities)
                .ThenInclude(a => a.AssignedToUser)
            .Where(c => c.CustomerId == customerId && c.IsActive)
            .AsNoTracking();

        query = ApplyScope(query, userId, userRole);
        var c = await query.FirstOrDefaultAsync();
        if (c == null) return null;

        return new CustomerDetailsViewModel
        {
            CustomerId = c.CustomerId,
            CustomerCode = c.CustomerCode,
            CustomerName = c.CustomerName,
            Email = c.Email,
            Phone = c.Phone,
            CompanyName = c.CompanyName,
            Address = c.Address,
            City = c.City,
            State = c.State,
            Status = c.Status,
            Notes = c.Notes,
            CreatedDate = c.CreatedDate,
            ModifiedDate = c.ModifiedDate,
            CreatedBy = c.CreatedBy,
            AssignedToUserName = c.AssignedToUser?.FullName ?? "Unassigned",
            Opportunities = c.Opportunities.Where(o => o.IsActive).Select(o => new OpportunityItemViewModel
            {
                OpportunityId = o.OpportunityId,
                OpportunityName = o.OpportunityName,
                CustomerId = o.CustomerId,
                CustomerName = c.CustomerName,
                Amount = o.Amount,
                Stage = o.Stage,
                Probability = o.Probability,
                ExpectedCloseDate = o.ExpectedCloseDate,
                Status = o.Status,
                CreatedDate = o.CreatedDate
            }).ToList(),
            FollowUps = c.FollowUps.Where(f => f.IsActive).OrderByDescending(f => f.FollowUpDate).Select(f => new FollowUpItemViewModel
            {
                FollowUpId = f.FollowUpId,
                Subject = f.Subject,
                CustomerId = f.CustomerId,
                CustomerName = c.CustomerName,
                FollowUpDate = f.FollowUpDate,
                FollowUpType = f.FollowUpType,
                Status = f.Status,
                Remarks = f.Remarks,
                AssignedToUserName = f.AssignedToUser?.FullName ?? "Unassigned",
                CreatedDate = f.CreatedDate
            }).ToList(),
            Activities = c.Activities.Where(a => a.IsActive).OrderByDescending(a => a.ActivityDate).Select(a => new ActivityItemViewModel
            {
                ActivityId = a.ActivityId,
                ActivityType = a.ActivityType,
                Subject = a.Subject,
                Description = a.Description,
                ActivityDate = a.ActivityDate,
                CustomerId = a.CustomerId,
                CustomerName = c.CustomerName,
                AssignedToUserName = a.AssignedToUser?.FullName ?? "Unassigned",
                Status = a.Status,
                CreatedDate = a.CreatedDate
            }).ToList()
        };
    }

    public async Task<CustomerCreateEditViewModel?> GetCustomerForEditAsync(int customerId, string? userId, string userRole)
    {
        var query = _context.Customers.Where(c => c.CustomerId == customerId && c.IsActive);
        query = ApplyScope(query, userId, userRole);
        var c = await query.FirstOrDefaultAsync();
        if (c == null) return null;

        return new CustomerCreateEditViewModel
        {
            CustomerId = c.CustomerId,
            CustomerCode = c.CustomerCode,
            CustomerName = c.CustomerName,
            Email = c.Email,
            Phone = c.Phone,
            CompanyName = c.CompanyName,
            Address = c.Address,
            City = c.City,
            State = c.State,
            Status = c.Status,
            AssignedToUserId = c.AssignedToUserId,
            Notes = c.Notes
        };
    }

    public async Task<(bool Success, string Message, int CustomerId)> CreateCustomerAsync(CustomerCreateEditViewModel model, string? currentUserId, string currentUserRole)
    {
        if (string.IsNullOrWhiteSpace(model.CustomerName))
            return (false, "Customer Name is required.", 0);

        if (string.IsNullOrWhiteSpace(model.Email))
            return (false, "Email is required.", 0);

        if (string.IsNullOrWhiteSpace(model.Phone))
            return (false, "Phone is required.", 0);

        // Check unique email
        if (!await IsEmailUniqueAsync(model.Email))
            return (false, $"Customer with email '{model.Email}' already exists.", 0);

        // Check unique phone
        if (!await IsPhoneUniqueAsync(model.Phone))
            return (false, $"Customer with phone number '{model.Phone}' already exists.", 0);

        // Generate CustomerCode
        var count = await _context.Customers.CountAsync() + 1;
        var code = $"CUST-{count:D4}";

        var customer = new Customer
        {
            CustomerCode = code,
            CustomerName = model.CustomerName.Trim(),
            Email = model.Email.Trim().ToLower(),
            Phone = model.Phone.Trim(),
            CompanyName = model.CompanyName?.Trim(),
            Address = model.Address?.Trim(),
            City = model.City?.Trim(),
            State = model.State?.Trim(),
            Status = model.Status,
            Notes = model.Notes?.Trim(),
            CreatedDate = DateTime.UtcNow,
            CreatedBy = currentUserRole == AppRoles.SalesExecutive ? currentUserId : "System",
            AssignedToUserId = currentUserRole == AppRoles.SalesExecutive ? currentUserId : model.AssignedToUserId,
            IsActive = true
        };

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync("Create", "Customer", customer.CustomerId.ToString(), null, customer.CustomerName, "Success", $"Created customer {customer.CustomerName} ({customer.CustomerCode})");

        return (true, "Customer created successfully.", customer.CustomerId);
    }

    public async Task<(bool Success, string Message)> UpdateCustomerAsync(CustomerCreateEditViewModel model, string? currentUserId, string currentUserRole)
    {
        var query = _context.Customers.Where(c => c.CustomerId == model.CustomerId && c.IsActive);
        query = ApplyScope(query, currentUserId, currentUserRole);
        var customer = await query.FirstOrDefaultAsync();

        if (customer == null)
            return (false, "Customer not found or you are not authorized to modify it.");

        // Check unique email
        if (!await IsEmailUniqueAsync(model.Email, model.CustomerId))
            return (false, $"Another customer with email '{model.Email}' already exists.");

        // Check unique phone
        if (!await IsPhoneUniqueAsync(model.Phone, model.CustomerId))
            return (false, $"Another customer with phone number '{model.Phone}' already exists.");

        var oldName = customer.CustomerName;
        var oldStatus = customer.Status.ToString();

        customer.CustomerName = model.CustomerName.Trim();
        customer.Email = model.Email.Trim().ToLower();
        customer.Phone = model.Phone.Trim();
        customer.CompanyName = model.CompanyName?.Trim();
        customer.Address = model.Address?.Trim();
        customer.City = model.City?.Trim();
        customer.State = model.State?.Trim();
        customer.Status = model.Status;
        customer.Notes = model.Notes?.Trim();
        customer.ModifiedDate = DateTime.UtcNow;

        if (currentUserRole != AppRoles.SalesExecutive)
        {
            customer.AssignedToUserId = model.AssignedToUserId;
        }

        await _context.SaveChangesAsync();

        await _auditService.LogAsync("Update", "Customer", customer.CustomerId.ToString(), oldName, customer.CustomerName, "Success", $"Updated customer {customer.CustomerName}. Status changed from {oldStatus} to {customer.Status}");

        return (true, "Customer updated successfully.");
    }

    public async Task<(bool Success, string Message)> DeactivateCustomerAsync(int customerId, string? currentUserId, string currentUserRole)
    {
        var query = _context.Customers.Where(c => c.CustomerId == customerId && c.IsActive);
        query = ApplyScope(query, currentUserId, currentUserRole);
        var customer = await query.FirstOrDefaultAsync();

        if (customer == null)
            return (false, "Customer not found or you are not authorized to deactivate it.");

        customer.IsActive = false;
        customer.Status = CustomerStatus.Inactive;
        customer.ModifiedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _auditService.LogAsync("Delete/Deactivate", "Customer", customer.CustomerId.ToString(), "Active", "Inactive", "Success", $"Deactivated customer {customer.CustomerName}");

        return (true, "Customer deactivated successfully.");
    }

    public async Task<bool> CanAccessCustomerAsync(int customerId, string? userId, string userRole)
    {
        if (userRole == AppRoles.Admin || userRole == AppRoles.Manager)
            return await _context.Customers.AnyAsync(c => c.CustomerId == customerId && c.IsActive);

        return await _context.Customers.AnyAsync(c => c.CustomerId == customerId && c.IsActive && c.AssignedToUserId == userId);
    }

    public async Task<bool> IsEmailUniqueAsync(string email, int? customerId = null)
    {
        var normalized = email.Trim().ToLower();
        return !await _context.Customers.AnyAsync(c => c.Email.ToLower() == normalized && c.IsActive && (customerId == null || c.CustomerId != customerId));
    }

    public async Task<bool> IsPhoneUniqueAsync(string phone, int? customerId = null)
    {
        var normalized = phone.Trim();
        return !await _context.Customers.AnyAsync(c => c.Phone == normalized && c.IsActive && (customerId == null || c.CustomerId != customerId));
    }

    public async Task<List<CustomerDto>> GetCustomerDtosAsync(string? userId, string userRole, string? search = null)
    {
        var query = _context.Customers
            .Include(c => c.AssignedToUser)
            .Where(c => c.IsActive)
            .AsNoTracking();

        query = ApplyScope(query, userId, userRole);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(c => c.CustomerName.ToLower().Contains(s) || c.Email.ToLower().Contains(s));
        }

        return await query.Select(c => new CustomerDto
        {
            CustomerId = c.CustomerId,
            CustomerCode = c.CustomerCode,
            CustomerName = c.CustomerName,
            Email = c.Email,
            Phone = c.Phone,
            CompanyName = c.CompanyName,
            Address = c.Address,
            City = c.City,
            State = c.State,
            Status = c.Status.ToString(),
            Notes = c.Notes,
            CreatedDate = c.CreatedDate,
            CreatedBy = c.CreatedBy,
            AssignedToUserId = c.AssignedToUserId,
            AssignedToUserName = c.AssignedToUser != null ? c.AssignedToUser.FullName : null
        }).ToListAsync();
    }

    public async Task<CustomerDto?> GetCustomerDtoByIdAsync(int customerId, string? userId, string userRole)
    {
        var query = _context.Customers
            .Include(c => c.AssignedToUser)
            .Where(c => c.CustomerId == customerId && c.IsActive)
            .AsNoTracking();

        query = ApplyScope(query, userId, userRole);

        return await query.Select(c => new CustomerDto
        {
            CustomerId = c.CustomerId,
            CustomerCode = c.CustomerCode,
            CustomerName = c.CustomerName,
            Email = c.Email,
            Phone = c.Phone,
            CompanyName = c.CompanyName,
            Address = c.Address,
            City = c.City,
            State = c.State,
            Status = c.Status.ToString(),
            Notes = c.Notes,
            CreatedDate = c.CreatedDate,
            CreatedBy = c.CreatedBy,
            AssignedToUserId = c.AssignedToUserId,
            AssignedToUserName = c.AssignedToUser != null ? c.AssignedToUser.FullName : null
        }).FirstOrDefaultAsync();
    }

    public async Task<(bool Success, string Message, CustomerDto? Customer)> CreateCustomerFromDtoAsync(CreateCustomerDto dto, string? currentUserId, string currentUserRole)
    {
        var vm = new CustomerCreateEditViewModel
        {
            CustomerName = dto.CustomerName,
            Email = dto.Email,
            Phone = dto.Phone,
            CompanyName = dto.CompanyName,
            Address = dto.Address,
            City = dto.City,
            State = dto.State,
            Notes = dto.Notes,
            AssignedToUserId = dto.AssignedToUserId
        };

        var result = await CreateCustomerAsync(vm, currentUserId, currentUserRole);
        if (!result.Success)
            return (false, result.Message, null);

        var created = await GetCustomerDtoByIdAsync(result.CustomerId, currentUserId, currentUserRole);
        return (true, result.Message, created);
    }
}
