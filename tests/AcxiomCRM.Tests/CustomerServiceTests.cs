using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Application.Interfaces;
using AcxiomCRM.Application.Services;
using AcxiomCRM.Application.ViewModels;
using AcxiomCRM.Domain.Entities;
using AcxiomCRM.Domain.Enums;
using AcxiomCRM.Infrastructure.Data;
using Xunit;

namespace AcxiomCRM.Tests;

public class FakeAuditService : IAuditService
{
    public readonly List<AuditLog> Logs = new();

    public Task LogAsync(string action, string entityName, string? recordId = null, string? oldValue = null, string? newValue = null, string? result = "Success", string? details = null)
    {
        Logs.Add(new AuditLog
        {
            Action = action,
            EntityName = entityName,
            RecordId = recordId,
            OldValue = oldValue,
            NewValue = newValue,
            Result = result,
            Details = details,
            CreatedDate = DateTime.UtcNow
        });
        return Task.CompletedTask;
    }

    public Task LogSecurityEventAsync(string action, string? details = null, string? result = "Success", string? userId = null, string? userName = null)
    {
        Logs.Add(new AuditLog
        {
            Action = action,
            EntityName = "Security",
            RecordId = userId,
            Result = result,
            Details = details,
            CreatedDate = DateTime.UtcNow
        });
        return Task.CompletedTask;
    }

    public Task<List<AuditLog>> GetAuditLogsAsync(string? userId = null, string? entityName = null, string? action = null, DateTime? fromDate = null, DateTime? toDate = null, int page = 1, int pageSize = 50)
    {
        return Task.FromResult(Logs.ToList());
    }

    public Task<int> GetAuditLogsCountAsync(string? userId = null, string? entityName = null, string? action = null, DateTime? fromDate = null, DateTime? toDate = null)
    {
        return Task.FromResult(Logs.Count);
    }
}

public class CustomerServiceTests
{
    private ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task CreateCustomerAsync_ValidModel_CreatesCustomerAndLogsAudit()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var fakeAudit = new FakeAuditService();
        var service = new CustomerService(context, fakeAudit);

        var model = new CustomerCreateEditViewModel
        {
            CustomerName = "Acme Corp",
            Email = "contact@acme.com",
            Phone = "1234567890",
            CompanyName = "Acme Global",
            Status = CustomerStatus.Active
        };

        // Act
        var (success, message, customerId) = await service.CreateCustomerAsync(model, "user-1", AppRoles.Admin);

        // Assert
        Assert.True(success);
        Assert.True(customerId > 0);

        var customer = await context.Customers.FindAsync(customerId);
        Assert.NotNull(customer);
        Assert.Equal("Acme Corp", customer.CustomerName);
        Assert.Equal("contact@acme.com", customer.Email);
        Assert.Single(fakeAudit.Logs);
        Assert.Equal("Create", fakeAudit.Logs[0].Action);
        Assert.Equal("Customer", fakeAudit.Logs[0].EntityName);
    }

    [Fact]
    public async Task CreateCustomerAsync_DuplicateEmail_ReturnsError()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var fakeAudit = new FakeAuditService();
        var service = new CustomerService(context, fakeAudit);

        context.Customers.Add(new Customer
        {
            CustomerCode = "CUST-0001",
            CustomerName = "Existing Customer",
            Email = "duplicate@acme.com",
            Phone = "1111111111",
            Status = CustomerStatus.Active,
            CreatedDate = DateTime.UtcNow,
            IsActive = true
        });
        await context.SaveChangesAsync();

        var model = new CustomerCreateEditViewModel
        {
            CustomerName = "New Customer",
            Email = "duplicate@acme.com",
            Phone = "2222222222",
            Status = CustomerStatus.Active
        };

        // Act
        var (success, message, customerId) = await service.CreateCustomerAsync(model, "user-1", AppRoles.Admin);

        // Assert
        Assert.False(success);
        Assert.Contains("email", message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, customerId);
    }

    [Fact]
    public async Task DeactivateCustomerAsync_SetsIsActiveFalse()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var fakeAudit = new FakeAuditService();
        var service = new CustomerService(context, fakeAudit);

        var customer = new Customer
        {
            CustomerCode = "CUST-0002",
            CustomerName = "To Deactivate",
            Email = "deactivate@acme.com",
            Phone = "9999999999",
            Status = CustomerStatus.Active,
            CreatedDate = DateTime.UtcNow,
            IsActive = true
        };
        context.Customers.Add(customer);
        await context.SaveChangesAsync();

        // Act
        var (success, message) = await service.DeactivateCustomerAsync(customer.CustomerId, "user-1", AppRoles.Admin);

        // Assert
        Assert.True(success);
        var refreshed = await context.Customers.FindAsync(customer.CustomerId);
        Assert.NotNull(refreshed);
        Assert.False(refreshed.IsActive);
    }
}
