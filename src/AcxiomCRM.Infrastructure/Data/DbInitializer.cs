using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Domain.Entities;
using AcxiomCRM.Domain.Enums;

namespace AcxiomCRM.Infrastructure.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        string? adminSeedPassword = null)
    {
        await context.Database.EnsureCreatedAsync();

        // 1. Seed Roles
        foreach (var role in AppRoles.AllRoles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // 2. Development Admin Account - Securely provisioned only via environment/configuration
        // Fails safely if missing (does not seed any default or publicly known password)
        var resolvedPassword = !string.IsNullOrWhiteSpace(adminSeedPassword)
            ? adminSeedPassword
            : Environment.GetEnvironmentVariable("ACXIOM_ADMIN_SEED_PASSWORD");

        ApplicationUser? adminUser = await userManager.FindByEmailAsync("admin@acxiomcrm.local");

        if (adminUser == null && !string.IsNullOrWhiteSpace(resolvedPassword) && resolvedPassword.Length >= 8)
        {
            adminUser = new ApplicationUser
            {
                UserName = "admin@acxiomcrm.local",
                Email = "admin@acxiomcrm.local",
                FullName = "System Administrator",
                RoleName = AppRoles.Admin,
                Department = "IT & Operations",
                EmailConfirmed = true,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };
            var result = await userManager.CreateAsync(adminUser, resolvedPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, AppRoles.Admin);
            }
        }

        // Staff accounts are not seeded with public/hardcoded passwords.
        // Users are provisioned by Administrator via the UI or registration flow.
        var sales1User = await userManager.FindByEmailAsync("sales1@acxiomcrm.local");
        var sales2User = await userManager.FindByEmailAsync("sales2@acxiomcrm.local");

        // 3. Seed Customers if empty
        if (!await context.Customers.AnyAsync())
        {
            var cust1 = new Customer
            {
                CustomerCode = "CUST-0001",
                CustomerName = "Apex Global Technologies",
                Email = "contact@apextechnologies.com",
                Phone = "+91 9876543210",
                CompanyName = "Apex Global Corp",
                Address = "Plot 42, Cyber City, Phase 2",
                City = "Gurugram",
                State = "Haryana",
                Status = CustomerStatus.Active,
                CreatedDate = DateTime.UtcNow.AddDays(-60),
                CreatedBy = "System Administrator",
                AssignedToUserId = sales1User?.Id,
                Notes = "Strategic cloud infrastructure account. Annual review in Q4."
            };

            var cust2 = new Customer
            {
                CustomerCode = "CUST-0002",
                CustomerName = "BlueHorizon Logistics Ltd",
                Email = "procurement@bluehorizonlogistics.com",
                Phone = "+91 9876543211",
                CompanyName = "BlueHorizon Group",
                Address = "Unit 108, Maritime Tower",
                City = "Mumbai",
                State = "Maharashtra",
                Status = CustomerStatus.Active,
                CreatedDate = DateTime.UtcNow.AddDays(-45),
                CreatedBy = "System Administrator",
                AssignedToUserId = sales2User?.Id,
                Notes = "Supply chain tracking system requirements."
            };

            var cust3 = new Customer
            {
                CustomerCode = "CUST-0003",
                CustomerName = "Crestview Financial Services",
                Email = "info@crestviewfin.com",
                Phone = "+91 9876543212",
                CompanyName = "Crestview Financial",
                Address = "Floor 12, Financial District",
                City = "Hyderabad",
                State = "Telangana",
                Status = CustomerStatus.Active,
                CreatedDate = DateTime.UtcNow.AddDays(-30),
                CreatedBy = "System Administrator",
                AssignedToUserId = sales1User?.Id,
                Notes = "High priority compliance and analytics expansion."
            };

            context.Customers.AddRange(cust1, cust2, cust3);
            await context.SaveChangesAsync();

            // 4. Seed Leads
            var lead1 = new Lead
            {
                LeadCode = "LEAD-0001",
                LeadName = "Pinnacle Retailers",
                Email = "director@pinnacleretail.com",
                Phone = "+91 9876543213",
                CompanyName = "Pinnacle Stores India",
                Source = "Website",
                Status = LeadStatus.New,
                Priority = LeadPriority.High,
                ExpectedValue = 150000m,
                CreatedDate = DateTime.UtcNow.AddDays(-10),
                AssignedToUserId = sales1User?.Id,
                Notes = "Inquired about POS CRM integration via website contact form."
            };

            var lead2 = new Lead
            {
                LeadCode = "LEAD-0002",
                LeadName = "Zenith Healthcare Solutions",
                Email = "dr.sharma@zenithhealth.org",
                Phone = "+91 9876543214",
                CompanyName = "Zenith Hospitals Network",
                Source = "Referral",
                Status = LeadStatus.Contacted,
                Priority = LeadPriority.Urgent,
                ExpectedValue = 350000m,
                CreatedDate = DateTime.UtcNow.AddDays(-8),
                AssignedToUserId = sales2User?.Id,
                Notes = "Hospital management portal consultation initiated."
            };

            var lead3 = new Lead
            {
                LeadCode = "LEAD-0003",
                LeadName = "Quantum AI Innovations",
                Email = "partnerships@quantumai.io",
                Phone = "+91 9876543215",
                CompanyName = "Quantum Labs",
                Source = "Event",
                Status = LeadStatus.Qualified,
                Priority = LeadPriority.Medium,
                ExpectedValue = 500000m,
                CreatedDate = DateTime.UtcNow.AddDays(-15),
                AssignedToUserId = sales1User?.Id,
                Notes = "Met at Tech Expo. Ready for proposal demonstration."
            };

            var lead4 = new Lead
            {
                LeadCode = "LEAD-0004",
                LeadName = "Nova Digital Media",
                Email = "hello@novamedia.co",
                Phone = "+91 9876543216",
                CompanyName = "Nova Agency",
                Source = "ColdCall",
                Status = LeadStatus.Lost,
                Priority = LeadPriority.Low,
                ExpectedValue = 75000m,
                CreatedDate = DateTime.UtcNow.AddDays(-20),
                AssignedToUserId = sales2User?.Id,
                Notes = "Decided to retain existing internal spreadsheets for now."
            };

            context.Leads.AddRange(lead1, lead2, lead3, lead4);
            await context.SaveChangesAsync();

            // 5. Seed Opportunities
            var opp1 = new Opportunity
            {
                OpportunityName = "Apex Cloud Migration Enterprise",
                CustomerId = cust1.CustomerId,
                Amount = 240000m,
                Stage = OpportunityStage.Proposal,
                Probability = 60,
                ExpectedCloseDate = DateTime.UtcNow.Date.AddDays(25),
                Status = OpportunityStatus.Open,
                CreatedDate = DateTime.UtcNow.AddDays(-20),
                AssignedToUserId = sales1User?.Id,
                Notes = "Submitted commercial RFP proposal. Awaiting executive signoff."
            };

            var opp2 = new Opportunity
            {
                OpportunityName = "BlueHorizon Fleet Tracking Expansion",
                CustomerId = cust2.CustomerId,
                Amount = 180000m,
                Stage = OpportunityStage.Negotiation,
                Probability = 80,
                ExpectedCloseDate = DateTime.UtcNow.Date.AddDays(14),
                Status = OpportunityStatus.Open,
                CreatedDate = DateTime.UtcNow.AddDays(-25),
                AssignedToUserId = sales2User?.Id,
                Notes = "Final contract terms under legal review."
            };

            var opp3 = new Opportunity
            {
                OpportunityName = "Crestview Security Module Add-on",
                CustomerId = cust3.CustomerId,
                Amount = 95000m,
                Stage = OpportunityStage.Won,
                Probability = 100,
                ExpectedCloseDate = DateTime.UtcNow.Date.AddDays(-5),
                Status = OpportunityStatus.Won,
                CreatedDate = DateTime.UtcNow.AddDays(-40),
                AssignedToUserId = sales1User?.Id,
                Notes = "Deal successfully closed and invoice generated."
            };

            var opp4 = new Opportunity
            {
                OpportunityName = "Apex Mobile Sales App Upgrade",
                CustomerId = cust1.CustomerId,
                Amount = 120000m,
                Stage = OpportunityStage.Qualification,
                Probability = 30,
                ExpectedCloseDate = DateTime.UtcNow.Date.AddDays(45),
                Status = OpportunityStatus.Open,
                CreatedDate = DateTime.UtcNow.AddDays(-5),
                AssignedToUserId = sales1User?.Id,
                Notes = "Initial scoping requirements discussion."
            };

            context.Opportunities.AddRange(opp1, opp2, opp3, opp4);
            await context.SaveChangesAsync();

            // 6. Seed FollowUps
            var fu1 = new FollowUp
            {
                Subject = "Quarterly Architecture Review Call",
                CustomerId = cust1.CustomerId,
                OpportunityId = opp1.OpportunityId,
                FollowUpDate = DateTime.UtcNow.Date.AddDays(2),
                FollowUpType = FollowUpType.Call,
                Status = FollowUpStatus.Planned,
                Remarks = "Review technical requirements with VP of Engineering.",
                AssignedToUserId = sales1User?.Id,
                CreatedDate = DateTime.UtcNow.AddDays(-3)
            };

            var fu2 = new FollowUp
            {
                Subject = "Contract Negotiations In-Person Meeting",
                CustomerId = cust2.CustomerId,
                OpportunityId = opp2.OpportunityId,
                FollowUpDate = DateTime.UtcNow.Date.AddDays(4),
                FollowUpType = FollowUpType.Meeting,
                Status = FollowUpStatus.Planned,
                Remarks = "Present final pricing table to procurement head.",
                AssignedToUserId = sales2User?.Id,
                CreatedDate = DateTime.UtcNow.AddDays(-2)
            };

            var fu3 = new FollowUp
            {
                Subject = "Product Demo for Pinnacle Retail Lead",
                LeadId = lead1.LeadId,
                FollowUpDate = DateTime.UtcNow.Date.AddDays(1),
                FollowUpType = FollowUpType.Call,
                Status = FollowUpStatus.Planned,
                Remarks = "Conduct 30-min online walkthrough of features.",
                AssignedToUserId = sales1User?.Id,
                CreatedDate = DateTime.UtcNow.AddDays(-1)
            };

            context.FollowUps.AddRange(fu1, fu2, fu3);
            await context.SaveChangesAsync();

            // 7. Seed Activities
            var act1 = new Activity
            {
                ActivityType = ActivityType.Call,
                Subject = "Initial Discovery Discussion",
                Description = "Discussed infrastructure pain points and current cloud cost baseline.",
                ActivityDate = DateTime.UtcNow.AddDays(-15),
                CustomerId = cust1.CustomerId,
                AssignedToUserId = sales1User?.Id,
                Status = ActivityStatus.Completed,
                CreatedDate = DateTime.UtcNow.AddDays(-15)
            };

            var act2 = new Activity
            {
                ActivityType = ActivityType.Meeting,
                Subject = "Stakeholder Alignment Presentation",
                Description = "Presented executive slide deck to board of directors.",
                ActivityDate = DateTime.UtcNow.AddDays(-10),
                CustomerId = cust2.CustomerId,
                AssignedToUserId = sales2User?.Id,
                Status = ActivityStatus.Completed,
                CreatedDate = DateTime.UtcNow.AddDays(-10)
            };

            var act3 = new Activity
            {
                ActivityType = ActivityType.Email,
                Subject = "Sent Proposal Document v1.2",
                Description = "Emailed formal proposal with SLA terms and payment schedule.",
                ActivityDate = DateTime.UtcNow.AddDays(-5),
                CustomerId = cust1.CustomerId,
                AssignedToUserId = sales1User?.Id,
                Status = ActivityStatus.Completed,
                CreatedDate = DateTime.UtcNow.AddDays(-5)
            };

            context.Activities.AddRange(act1, act2, act3);
            await context.SaveChangesAsync();

            // 8. Seed Initial Audit Logs
            context.AuditLogs.AddRange(
                new AuditLog
                {
                    UserId = adminUser?.Id,
                    UserName = adminUser?.UserName,
                    Action = "SystemSetup",
                    EntityName = "Database",
                    RecordId = "Init",
                    IpAddress = "127.0.0.1",
                    Result = "Success",
                    Details = "Initial CRM database setup and baseline master data seeded.",
                    CreatedDate = DateTime.UtcNow.AddDays(-60)
                },
                new AuditLog
                {
                    UserId = adminUser?.Id,
                    UserName = adminUser?.UserName,
                    Action = "Create",
                    EntityName = "Customer",
                    RecordId = cust1.CustomerId.ToString(),
                    NewValue = cust1.CustomerName,
                    IpAddress = "127.0.0.1",
                    Result = "Success",
                    Details = "Created customer Apex Global Technologies.",
                    CreatedDate = DateTime.UtcNow.AddDays(-60)
                },
                new AuditLog
                {
                    UserId = sales1User?.Id,
                    UserName = sales1User?.UserName,
                    Action = "Create",
                    EntityName = "Opportunity",
                    RecordId = opp1.OpportunityId.ToString(),
                    NewValue = opp1.OpportunityName,
                    IpAddress = "127.0.0.1",
                    Result = "Success",
                    Details = "Created opportunity with amount ₹240,000.",
                    CreatedDate = DateTime.UtcNow.AddDays(-20)
                }
            );
            await context.SaveChangesAsync();
        }
    }
}
