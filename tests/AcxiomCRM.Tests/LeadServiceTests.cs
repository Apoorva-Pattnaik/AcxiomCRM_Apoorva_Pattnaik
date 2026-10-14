using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Application.DTOs;
using AcxiomCRM.Application.Interfaces;
using AcxiomCRM.Application.Services;
using AcxiomCRM.Application.ViewModels;
using AcxiomCRM.Domain.Entities;
using AcxiomCRM.Domain.Enums;
using AcxiomCRM.Infrastructure.Data;
using Xunit;

namespace AcxiomCRM.Tests;

public class LeadServiceTests
{
    private ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task CreateLeadAsync_ValidModel_CreatesLeadWithCode()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var fakeAudit = new FakeAuditService();
        var custService = new CustomerService(context, fakeAudit);
        var service = new LeadService(context, fakeAudit, custService);

        var model = new LeadCreateEditViewModel
        {
            LeadName = "Potential Lead",
            Email = "lead@example.com",
            Phone = "1234567890",
            CompanyName = "Prospect Co",
            Status = LeadStatus.New,
            Priority = LeadPriority.High,
            ExpectedValue = 50000m
        };

        // Act
        var (success, message, leadId) = await service.CreateLeadAsync(model, "user-1", AppRoles.SalesExecutive);

        // Assert
        Assert.True(success);
        Assert.True(leadId > 0);

        var lead = await context.Leads.FindAsync(leadId);
        Assert.NotNull(lead);
        Assert.StartsWith("LEAD-", lead.LeadCode);
        Assert.Equal(50000m, lead.ExpectedValue);
    }

    [Fact]
    public async Task UpdateLeadStatusAsync_UpdatesStatusAndLogsAudit()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var fakeAudit = new FakeAuditService();
        var custService = new CustomerService(context, fakeAudit);
        var service = new LeadService(context, fakeAudit, custService);

        var lead = new Lead
        {
            LeadCode = "LEAD-0001",
            LeadName = "Stage Test",
            Email = "stage@example.com",
            Phone = "5555555555",
            Status = LeadStatus.New,
            Priority = LeadPriority.Medium,
            CreatedDate = DateTime.UtcNow,
            IsActive = true
        };
        context.Leads.Add(lead);
        await context.SaveChangesAsync();

        // Act
        var (success, message) = await service.UpdateLeadStatusAsync(lead.LeadId, LeadStatus.Contacted, "user-1", AppRoles.Admin);

        // Assert
        Assert.True(success);
        var updated = await context.Leads.FindAsync(lead.LeadId);
        Assert.NotNull(updated);
        Assert.Equal(LeadStatus.Contacted, updated.Status);
        Assert.Contains(fakeAudit.Logs, l => l.Action == "StatusChange" && l.EntityName == "Lead");
    }
}
