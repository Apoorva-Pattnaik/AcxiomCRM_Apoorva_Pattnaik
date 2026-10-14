using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using AcxiomCRM.Domain.Entities;

namespace AcxiomCRM.Application.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Customer> Customers { get; }
    DbSet<Lead> Leads { get; }
    DbSet<Opportunity> Opportunities { get; }
    DbSet<FollowUp> FollowUps { get; }
    DbSet<Activity> Activities { get; }
    DbSet<AuditLog> AuditLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
