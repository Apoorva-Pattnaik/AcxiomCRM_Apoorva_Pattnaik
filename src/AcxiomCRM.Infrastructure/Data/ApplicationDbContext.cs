using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using AcxiomCRM.Application.Interfaces;
using AcxiomCRM.Domain.Entities;

namespace AcxiomCRM.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        return await Database.BeginTransactionAsync(cancellationToken);
    }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<Opportunity> Opportunities => Set<Opportunity>();
    public DbSet<FollowUp> FollowUps => Set<FollowUp>();
    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Customer Configurations
        builder.Entity<Customer>(entity =>
        {
            entity.HasKey(c => c.CustomerId);
            entity.HasIndex(c => c.CustomerCode).IsUnique();
            entity.HasIndex(c => c.Email).IsUnique();
            entity.HasIndex(c => c.Phone).IsUnique();

            entity.Property(c => c.CustomerName).IsRequired().HasMaxLength(150);
            entity.Property(c => c.Email).IsRequired().HasMaxLength(150);
            entity.Property(c => c.Phone).IsRequired().HasMaxLength(20);

            entity.HasOne(c => c.AssignedToUser)
                  .WithMany()
                  .HasForeignKey(c => c.AssignedToUserId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Lead Configurations
        builder.Entity<Lead>(entity =>
        {
            entity.HasKey(l => l.LeadId);
            entity.HasIndex(l => l.LeadCode).IsUnique();

            entity.Property(l => l.LeadName).IsRequired().HasMaxLength(150);
            entity.Property(l => l.Email).IsRequired().HasMaxLength(150);
            entity.Property(l => l.Phone).IsRequired().HasMaxLength(20);
            entity.Property(l => l.ExpectedValue).HasColumnType("decimal(18,2)");

            entity.HasOne(l => l.AssignedToUser)
                  .WithMany()
                  .HasForeignKey(l => l.AssignedToUserId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(l => l.ConvertedCustomer)
                  .WithMany(c => c.ConvertedLeads)
                  .HasForeignKey(l => l.ConvertedCustomerId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(l => l.ConvertedOpportunity)
                  .WithMany()
                  .HasForeignKey(l => l.ConvertedOpportunityId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Opportunity Configurations
        builder.Entity<Opportunity>(entity =>
        {
            entity.HasKey(o => o.OpportunityId);

            entity.Property(o => o.OpportunityName).IsRequired().HasMaxLength(150);
            entity.Property(o => o.Amount).HasColumnType("decimal(18,2)").IsRequired();

            entity.HasOne(o => o.Customer)
                  .WithMany(c => c.Opportunities)
                  .HasForeignKey(o => o.CustomerId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(o => o.Lead)
                  .WithMany(l => l.Opportunities)
                  .HasForeignKey(o => o.LeadId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(o => o.AssignedToUser)
                  .WithMany()
                  .HasForeignKey(o => o.AssignedToUserId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // FollowUp Configurations
        builder.Entity<FollowUp>(entity =>
        {
            entity.HasKey(f => f.FollowUpId);
            entity.Property(f => f.Subject).IsRequired().HasMaxLength(200);

            entity.HasOne(f => f.Customer)
                  .WithMany(c => c.FollowUps)
                  .HasForeignKey(f => f.CustomerId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(f => f.Lead)
                  .WithMany(l => l.FollowUps)
                  .HasForeignKey(f => f.LeadId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(f => f.Opportunity)
                  .WithMany(o => o.FollowUps)
                  .HasForeignKey(f => f.OpportunityId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(f => f.AssignedToUser)
                  .WithMany()
                  .HasForeignKey(f => f.AssignedToUserId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Activity Configurations
        builder.Entity<Activity>(entity =>
        {
            entity.HasKey(a => a.ActivityId);
            entity.Property(a => a.Subject).IsRequired().HasMaxLength(200);

            entity.HasOne(a => a.Customer)
                  .WithMany(c => c.Activities)
                  .HasForeignKey(a => a.CustomerId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(a => a.Lead)
                  .WithMany(l => l.Activities)
                  .HasForeignKey(a => a.LeadId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(a => a.AssignedToUser)
                  .WithMany()
                  .HasForeignKey(a => a.AssignedToUserId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // AuditLog Configurations
        builder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(a => a.AuditLogId);
            entity.Property(a => a.Action).IsRequired().HasMaxLength(100);
            entity.Property(a => a.EntityName).IsRequired().HasMaxLength(100);
            entity.HasIndex(a => a.CreatedDate);
            entity.HasIndex(a => a.UserId);
            entity.HasIndex(a => a.EntityName);
        });
    }
}
