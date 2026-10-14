using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Application.DTOs;
using AcxiomCRM.Application.Interfaces;
using AcxiomCRM.Application.ViewModels;
using AcxiomCRM.Domain.Entities;
using AcxiomCRM.Domain.Enums;

namespace AcxiomCRM.Application.Services;

public class LeadService : ILeadService
{
    private readonly IApplicationDbContext _context;
    private readonly IAuditService _auditService;
    private readonly ICustomerService _customerService;

    public LeadService(IApplicationDbContext context, IAuditService auditService, ICustomerService customerService)
    {
        _context = context;
        _auditService = auditService;
        _customerService = customerService;
    }

    private IQueryable<Lead> ApplyScope(IQueryable<Lead> query, string? userId, string userRole)
    {
        if (userRole == AppRoles.SalesExecutive && !string.IsNullOrEmpty(userId))
        {
            return query.Where(l => l.AssignedToUserId == userId);
        }
        return query;
    }

    public async Task<LeadListViewModel> GetLeadsAsync(string? userId, string userRole, string? search, LeadStatus? status, string? source, string? assignedUserId, int page = 1, int pageSize = 10)
    {
        var query = _context.Leads
            .Include(l => l.AssignedToUser)
            .Where(l => l.IsActive)
            .AsNoTracking();

        query = ApplyScope(query, userId, userRole);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(l => l.LeadName.ToLower().Contains(s) ||
                                     l.Email.ToLower().Contains(s) ||
                                     l.Phone.Contains(s) ||
                                     (l.CompanyName != null && l.CompanyName.ToLower().Contains(s)));
        }

        if (status.HasValue)
        {
            query = query.Where(l => l.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(source))
        {
            query = query.Where(l => l.Source == source);
        }

        if (!string.IsNullOrWhiteSpace(assignedUserId) && userRole != AppRoles.SalesExecutive)
        {
            query = query.Where(l => l.AssignedToUserId == assignedUserId);
        }

        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
        if (totalPages < 1) totalPages = 1;
        if (page < 1) page = 1;

        var items = await query.OrderByDescending(l => l.CreatedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new LeadItemViewModel
            {
                LeadId = l.LeadId,
                LeadCode = l.LeadCode,
                LeadName = l.LeadName,
                Email = l.Email,
                Phone = l.Phone,
                CompanyName = l.CompanyName,
                Source = l.Source,
                Status = l.Status,
                Priority = l.Priority,
                ExpectedValue = l.ExpectedValue,
                AssignedToUserName = l.AssignedToUser != null ? l.AssignedToUser.FullName : "Unassigned",
                CreatedDate = l.CreatedDate,
                ConvertedCustomerId = l.ConvertedCustomerId,
                ConvertedOpportunityId = l.ConvertedOpportunityId
            })
            .ToListAsync();

        return new LeadListViewModel
        {
            Leads = items,
            SearchTerm = search,
            StatusFilter = status,
            SourceFilter = source,
            AssignedUserFilter = assignedUserId,
            CurrentPage = page,
            TotalPages = totalPages,
            TotalItems = totalItems,
            PageSize = pageSize
        };
    }

    public async Task<LeadDetailsViewModel?> GetLeadDetailsAsync(int leadId, string? userId, string userRole)
    {
        var query = _context.Leads
            .Include(l => l.AssignedToUser)
            .Include(l => l.ConvertedCustomer)
            .Include(l => l.ConvertedOpportunity)
            .Include(l => l.FollowUps)
                .ThenInclude(f => f.AssignedToUser)
            .Include(l => l.Activities)
                .ThenInclude(a => a.AssignedToUser)
            .Where(l => l.LeadId == leadId && l.IsActive)
            .AsNoTracking();

        query = ApplyScope(query, userId, userRole);
        var l = await query.FirstOrDefaultAsync();
        if (l == null) return null;

        return new LeadDetailsViewModel
        {
            LeadId = l.LeadId,
            LeadCode = l.LeadCode,
            LeadName = l.LeadName,
            Email = l.Email,
            Phone = l.Phone,
            CompanyName = l.CompanyName,
            Source = l.Source,
            Status = l.Status,
            Priority = l.Priority,
            ExpectedValue = l.ExpectedValue,
            CreatedDate = l.CreatedDate,
            AssignedToUserName = l.AssignedToUser?.FullName ?? "Unassigned",
            Notes = l.Notes,
            ConvertedCustomerId = l.ConvertedCustomerId,
            ConvertedCustomerName = l.ConvertedCustomer?.CustomerName,
            ConvertedOpportunityId = l.ConvertedOpportunityId,
            ConvertedOpportunityName = l.ConvertedOpportunity?.OpportunityName,
            ConvertedDate = l.ConvertedDate,
            FollowUps = l.FollowUps.Where(f => f.IsActive).OrderByDescending(f => f.FollowUpDate).Select(f => new FollowUpItemViewModel
            {
                FollowUpId = f.FollowUpId,
                Subject = f.Subject,
                LeadId = f.LeadId,
                LeadName = l.LeadName,
                FollowUpDate = f.FollowUpDate,
                FollowUpType = f.FollowUpType,
                Status = f.Status,
                Remarks = f.Remarks,
                AssignedToUserName = f.AssignedToUser?.FullName ?? "Unassigned",
                CreatedDate = f.CreatedDate
            }).ToList(),
            Activities = l.Activities.Where(a => a.IsActive).OrderByDescending(a => a.ActivityDate).Select(a => new ActivityItemViewModel
            {
                ActivityId = a.ActivityId,
                ActivityType = a.ActivityType,
                Subject = a.Subject,
                Description = a.Description,
                ActivityDate = a.ActivityDate,
                LeadId = a.LeadId,
                LeadName = l.LeadName,
                AssignedToUserName = a.AssignedToUser?.FullName ?? "Unassigned",
                Status = a.Status,
                CreatedDate = a.CreatedDate
            }).ToList()
        };
    }

    public async Task<LeadCreateEditViewModel?> GetLeadForEditAsync(int leadId, string? userId, string userRole)
    {
        var query = _context.Leads.Where(l => l.LeadId == leadId && l.IsActive);
        query = ApplyScope(query, userId, userRole);
        var l = await query.FirstOrDefaultAsync();
        if (l == null) return null;

        return new LeadCreateEditViewModel
        {
            LeadId = l.LeadId,
            LeadCode = l.LeadCode,
            LeadName = l.LeadName,
            Email = l.Email,
            Phone = l.Phone,
            CompanyName = l.CompanyName,
            Source = l.Source,
            Status = l.Status,
            Priority = l.Priority,
            ExpectedValue = l.ExpectedValue,
            AssignedToUserId = l.AssignedToUserId,
            Notes = l.Notes
        };
    }

    public async Task<(bool Success, string Message, int LeadId)> CreateLeadAsync(LeadCreateEditViewModel model, string? currentUserId, string currentUserRole)
    {
        if (string.IsNullOrWhiteSpace(model.LeadName))
            return (false, "Lead Name is required.", 0);

        if (string.IsNullOrWhiteSpace(model.Email))
            return (false, "Email is required.", 0);

        if (string.IsNullOrWhiteSpace(model.Phone))
            return (false, "Phone is required.", 0);

        if (model.ExpectedValue < 0 || model.ExpectedValue > 1000000000m)
            return (false, "Expected Value must be between 0 and 1,000,000,000.", 0);

        var count = await _context.Leads.CountAsync() + 1;
        var code = $"LEAD-{count:D4}";

        var lead = new Lead
        {
            LeadCode = code,
            LeadName = model.LeadName.Trim(),
            Email = model.Email.Trim().ToLower(),
            Phone = model.Phone.Trim(),
            CompanyName = model.CompanyName?.Trim(),
            Source = model.Source.Trim(),
            Status = model.Status,
            Priority = model.Priority,
            ExpectedValue = model.ExpectedValue,
            Notes = model.Notes?.Trim(),
            CreatedDate = DateTime.UtcNow,
            AssignedToUserId = currentUserRole == AppRoles.SalesExecutive ? currentUserId : model.AssignedToUserId,
            IsActive = true
        };

        _context.Leads.Add(lead);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync("Create", "Lead", lead.LeadId.ToString(), null, lead.LeadName, "Success", $"Created lead {lead.LeadName} ({lead.LeadCode}) with expected value ₹{lead.ExpectedValue}");

        return (true, "Lead created successfully.", lead.LeadId);
    }

    public async Task<(bool Success, string Message)> UpdateLeadAsync(LeadCreateEditViewModel model, string? currentUserId, string currentUserRole)
    {
        var query = _context.Leads.Where(l => l.LeadId == model.LeadId && l.IsActive);
        query = ApplyScope(query, currentUserId, currentUserRole);
        var lead = await query.FirstOrDefaultAsync();

        if (lead == null)
            return (false, "Lead not found or you are not authorized to modify it.");

        if (lead.Status == LeadStatus.Converted && model.Status != LeadStatus.Converted)
            return (false, "Converted leads cannot be reverted to an open status.");

        if (model.ExpectedValue < 0 || model.ExpectedValue > 1000000000m)
            return (false, "Expected Value must be between 0 and 1,000,000,000.");

        var oldStatus = lead.Status.ToString();
        var oldName = lead.LeadName;

        lead.LeadName = model.LeadName.Trim();
        lead.Email = model.Email.Trim().ToLower();
        lead.Phone = model.Phone.Trim();
        lead.CompanyName = model.CompanyName?.Trim();
        lead.Source = model.Source.Trim();
        lead.Status = model.Status;
        lead.Priority = model.Priority;
        lead.ExpectedValue = model.ExpectedValue;
        lead.Notes = model.Notes?.Trim();
        lead.ModifiedDate = DateTime.UtcNow;

        if (currentUserRole != AppRoles.SalesExecutive)
        {
            lead.AssignedToUserId = model.AssignedToUserId;
        }

        await _context.SaveChangesAsync();

        await _auditService.LogAsync("Update", "Lead", lead.LeadId.ToString(), oldName, lead.LeadName, "Success", $"Updated lead {lead.LeadName}. Status changed from {oldStatus} to {lead.Status}");

        return (true, "Lead updated successfully.");
    }

    public async Task<(bool Success, string Message)> UpdateLeadStatusAsync(int leadId, LeadStatus newStatus, string? currentUserId, string currentUserRole)
    {
        var query = _context.Leads.Where(l => l.LeadId == leadId && l.IsActive);
        query = ApplyScope(query, currentUserId, currentUserRole);
        var lead = await query.FirstOrDefaultAsync();

        if (lead == null)
            return (false, "Lead not found or access denied.");

        if (lead.Status == LeadStatus.Converted)
            return (false, "Converted leads cannot be altered directly. They are permanently archived as converted.");

        var oldStatus = lead.Status.ToString();
        lead.Status = newStatus;
        lead.ModifiedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _auditService.LogAsync("StatusChange", "Lead", lead.LeadId.ToString(), oldStatus, newStatus.ToString(), "Success", $"Updated lead {lead.LeadName} status to {newStatus}");

        return (true, $"Lead status updated to {newStatus}.");
    }

    public async Task<LeadConversionViewModel?> GetLeadForConversionAsync(int leadId, string? userId, string userRole)
    {
        var query = _context.Leads.Where(l => l.LeadId == leadId && l.IsActive);
        query = ApplyScope(query, userId, userRole);
        var l = await query.FirstOrDefaultAsync();

        if (l == null) return null;
        if (l.Status == LeadStatus.Converted) return null;

        return new LeadConversionViewModel
        {
            LeadId = l.LeadId,
            LeadCode = l.LeadCode,
            LeadName = l.LeadName,
            Email = l.Email,
            Phone = l.Phone,
            CompanyName = l.CompanyName,
            ExpectedValue = l.ExpectedValue,
            CustomerName = !string.IsNullOrWhiteSpace(l.CompanyName) ? l.CompanyName : l.LeadName,
            OpportunityName = $"{l.LeadName} - New Opportunity",
            OpportunityAmount = l.ExpectedValue > 0 ? l.ExpectedValue : 50000m,
            OpportunityStage = OpportunityStage.Qualification,
            Probability = 20,
            ExpectedCloseDate = DateTime.UtcNow.AddDays(30),
            AssignedToUserId = l.AssignedToUserId
        };
    }

    public async Task<(bool Success, string Message, int? CustomerId, int? OpportunityId)> ConvertLeadAsync(LeadConversionViewModel model, string? currentUserId, string currentUserRole)
    {
        var query = _context.Leads.Where(l => l.LeadId == model.LeadId && l.IsActive);
        query = ApplyScope(query, currentUserId, currentUserRole);
        var lead = await query.FirstOrDefaultAsync();

        if (lead == null)
            return (false, "Lead not found or access denied.", null, null);

        if (lead.Status == LeadStatus.Converted)
            return (false, "This lead has already been converted.", lead.ConvertedCustomerId, lead.ConvertedOpportunityId);

        // Validation for conversion
        if (model.CreateCustomer && string.IsNullOrWhiteSpace(model.CustomerName))
            return (false, "Customer Name is required when creating a customer.", null, null);

        if (model.CreateOpportunity && model.OpportunityAmount <= 0)
            return (false, "Opportunity Amount must be greater than 0.", null, null);

        if (model.CreateOpportunity && (model.Probability < 0 || model.Probability > 100))
            return (false, "Probability must be between 0 and 100.", null, null);

        if (model.CreateOpportunity && model.ExpectedCloseDate.Date < DateTime.UtcNow.Date)
            return (false, "Expected Close Date cannot be in the past.", null, null);

        using var transaction = await _context.BeginTransactionAsync();
        try
        {
            int? createdCustomerId = null;
            int? createdOppId = null;

            // 1. Create Customer if checked
            if (model.CreateCustomer)
            {
                // Check if customer email already exists
                var existingCustomer = await _context.Customers.FirstOrDefaultAsync(c => c.Email.ToLower() == lead.Email.ToLower() && c.IsActive);
                if (existingCustomer != null)
                {
                    createdCustomerId = existingCustomer.CustomerId;
                }
                else
                {
                    var custCount = await _context.Customers.CountAsync() + 1;
                    var newCust = new Customer
                    {
                        CustomerCode = $"CUST-{custCount:D4}",
                        CustomerName = model.CustomerName.Trim(),
                        Email = lead.Email.Trim().ToLower(),
                        Phone = lead.Phone.Trim(),
                        CompanyName = !string.IsNullOrWhiteSpace(model.CompanyName) ? model.CompanyName.Trim() : lead.CompanyName,
                        Address = null,
                        City = null,
                        State = null,
                        Status = CustomerStatus.Active,
                        Notes = $"Converted from lead {lead.LeadCode} ({lead.LeadName})",
                        CreatedDate = DateTime.UtcNow,
                        CreatedBy = currentUserRole == AppRoles.SalesExecutive ? currentUserId : "System",
                        AssignedToUserId = lead.AssignedToUserId ?? currentUserId,
                        IsActive = true
                    };
                    _context.Customers.Add(newCust);
                    await _context.SaveChangesAsync();
                    createdCustomerId = newCust.CustomerId;

                    await _auditService.LogAsync("Create", "Customer", newCust.CustomerId.ToString(), null, newCust.CustomerName, "Success", $"Created customer via lead conversion ({lead.LeadCode})");
                }
            }

            // 2. Create Opportunity if checked
            if (model.CreateOpportunity && createdCustomerId.HasValue)
            {
                var opp = new Opportunity
                {
                    OpportunityName = !string.IsNullOrWhiteSpace(model.OpportunityName) ? model.OpportunityName.Trim() : $"{lead.LeadName} Deal",
                    CustomerId = createdCustomerId.Value,
                    LeadId = lead.LeadId,
                    Amount = model.OpportunityAmount,
                    Stage = model.OpportunityStage,
                    Probability = model.Probability,
                    ExpectedCloseDate = model.ExpectedCloseDate,
                    Status = OpportunityStatus.Open,
                    CreatedDate = DateTime.UtcNow,
                    AssignedToUserId = lead.AssignedToUserId ?? currentUserId,
                    Notes = $"Originated from lead conversion {lead.LeadCode}",
                    IsActive = true
                };
                _context.Opportunities.Add(opp);
                await _context.SaveChangesAsync();
                createdOppId = opp.OpportunityId;

                await _auditService.LogAsync("Create", "Opportunity", opp.OpportunityId.ToString(), null, opp.OpportunityName, "Success", $"Created opportunity via lead conversion for customer ID {createdCustomerId}");
            }

            // 3. Update Lead Status and Links
            lead.Status = LeadStatus.Converted;
            lead.ConvertedCustomerId = createdCustomerId;
            lead.ConvertedOpportunityId = createdOppId;
            lead.ConvertedDate = DateTime.UtcNow;
            lead.ModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _auditService.LogAsync("LeadConversion", "Lead", lead.LeadId.ToString(), "Qualified", "Converted", "Success", $"Converted lead {lead.LeadName} to Customer ID {createdCustomerId} and Opportunity ID {createdOppId}");

            await transaction.CommitAsync();

            return (true, "Lead converted successfully.", createdCustomerId, createdOppId);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return (false, $"Conversion failed: {ex.Message}", null, null);
        }
    }

    public async Task<(bool Success, string Message)> DeleteLeadAsync(int leadId, string? currentUserId, string currentUserRole)
    {
        var query = _context.Leads.Where(l => l.LeadId == leadId && l.IsActive);
        query = ApplyScope(query, currentUserId, currentUserRole);
        var lead = await query.FirstOrDefaultAsync();

        if (lead == null)
            return (false, "Lead not found or access denied.");

        lead.IsActive = false;
        lead.Status = LeadStatus.Lost;
        lead.ModifiedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _auditService.LogAsync("Delete/Deactivate", "Lead", lead.LeadId.ToString(), lead.LeadName, "Deactivated", "Success", $"Deactivated lead {lead.LeadName}");

        return (true, "Lead deactivated successfully.");
    }

    public async Task<bool> CanAccessLeadAsync(int leadId, string? userId, string userRole)
    {
        if (userRole == AppRoles.Admin || userRole == AppRoles.Manager)
            return await _context.Leads.AnyAsync(l => l.LeadId == leadId && l.IsActive);

        return await _context.Leads.AnyAsync(l => l.LeadId == leadId && l.IsActive && l.AssignedToUserId == userId);
    }

    public async Task<List<LeadDto>> GetLeadDtosAsync(string? userId, string userRole, string? search = null)
    {
        var query = _context.Leads
            .Include(l => l.AssignedToUser)
            .Where(l => l.IsActive)
            .AsNoTracking();

        query = ApplyScope(query, userId, userRole);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(l => l.LeadName.ToLower().Contains(s) || l.Email.ToLower().Contains(s));
        }

        return await query.Select(l => new LeadDto
        {
            LeadId = l.LeadId,
            LeadCode = l.LeadCode,
            LeadName = l.LeadName,
            Email = l.Email,
            Phone = l.Phone,
            CompanyName = l.CompanyName,
            Source = l.Source,
            Status = l.Status.ToString(),
            Priority = l.Priority.ToString(),
            ExpectedValue = l.ExpectedValue,
            CreatedDate = l.CreatedDate,
            AssignedToUserId = l.AssignedToUserId,
            AssignedToUserName = l.AssignedToUser != null ? l.AssignedToUser.FullName : null,
            ConvertedCustomerId = l.ConvertedCustomerId,
            ConvertedOpportunityId = l.ConvertedOpportunityId,
            Notes = l.Notes
        }).ToListAsync();
    }

    public async Task<(bool Success, string Message, LeadDto? Lead)> CreateLeadFromDtoAsync(CreateLeadDto dto, string? currentUserId, string currentUserRole)
    {
        if (!Enum.TryParse<LeadStatus>(dto.Status, true, out var leadStatus))
            leadStatus = LeadStatus.New;

        if (!Enum.TryParse<LeadPriority>(dto.Priority, true, out var leadPriority))
            leadPriority = LeadPriority.Medium;

        var vm = new LeadCreateEditViewModel
        {
            LeadName = dto.LeadName,
            Email = dto.Email,
            Phone = dto.Phone,
            CompanyName = dto.CompanyName,
            Source = dto.Source,
            Status = leadStatus,
            Priority = leadPriority,
            ExpectedValue = dto.ExpectedValue,
            AssignedToUserId = dto.AssignedToUserId,
            Notes = dto.Notes
        };

        var result = await CreateLeadAsync(vm, currentUserId, currentUserRole);
        if (!result.Success)
            return (false, result.Message, null);

        var created = await _context.Leads
            .Include(l => l.AssignedToUser)
            .Where(l => l.LeadId == result.LeadId)
            .Select(l => new LeadDto
            {
                LeadId = l.LeadId,
                LeadCode = l.LeadCode,
                LeadName = l.LeadName,
                Email = l.Email,
                Phone = l.Phone,
                CompanyName = l.CompanyName,
                Source = l.Source,
                Status = l.Status.ToString(),
                Priority = l.Priority.ToString(),
                ExpectedValue = l.ExpectedValue,
                CreatedDate = l.CreatedDate,
                AssignedToUserId = l.AssignedToUserId,
                AssignedToUserName = l.AssignedToUser != null ? l.AssignedToUser.FullName : null
            }).FirstOrDefaultAsync();

        return (true, result.Message, created);
    }
}
