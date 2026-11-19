using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Rackroom.Domain.Entities;
using Rackroom.Infrastructure.Identity;

namespace Rackroom.Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using IServiceScope scope = services.CreateScope();

        ApplicationDbContext db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        RoleManager<IdentityRole> roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        UserManager<ApplicationUser> userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        await db.Database.MigrateAsync();

        foreach (string role in new[] { "Member", "Staff", "Admin" })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        Gym? gym = await db.Gyms.IgnoreQueryFilters().FirstOrDefaultAsync();

        if (gym is null)
        {
            gym = new Gym
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Name = "Rackroom Demo Gym",
                PanNumber = "DEMO-PAN-001",
                Address = "Kathmandu, Nepal",
                CreatedAtUtc = DateTime.UtcNow
            };

            db.Gyms.Add(gym);
            await db.SaveChangesAsync();
        }

        await CreateUserAsync(userManager, "admin@rackroom.local", "Admin123!", "Admin", gym.Id);
        await CreateUserAsync(userManager, "staff@rackroom.local", "Staff123!", "Staff", gym.Id);
        await CreateUserAsync(userManager, "member@rackroom.local", "Member123!", "Member", gym.Id);

        Member member = await GetOrCreateMemberAsync(db, gym.Id);
        MembershipPlan monthly = await GetOrCreatePlanAsync(db, gym.Id, "Monthly", 1, 3000m);
        MembershipPlan quarterly = await GetOrCreatePlanAsync(db, gym.Id, "3 Months", 3, 8000m);
        MembershipPlan annual = await GetOrCreatePlanAsync(db, gym.Id, "Annual", 12, 28000m);

        Membership? membership = await db.Memberships
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.GymId == gym.Id && x.MemberId == member.Id);

        if (membership is null)
        {
            DateTime start = DateTime.UtcNow.Date.AddDays(-10);

            membership = new Membership
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                GymId = gym.Id,
                MemberId = member.Id,
                MembershipPlanId = monthly.Id,
                StartDateUtc = start,
                EndDateUtc = start.AddMonths(1),
                Status = MembershipStatus.Active,
                CreatedAtUtc = DateTime.UtcNow
            };

            db.Memberships.Add(membership);
            await db.SaveChangesAsync();
        }

        if (!await db.Payments.IgnoreQueryFilters().AnyAsync(x => x.GymId == gym.Id && x.MembershipId == membership.Id))
        {
            Payment payment = new()
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                GymId = gym.Id,
                MembershipId = membership.Id,
                MemberId = member.Id,
                Amount = monthly.Price,
                PaidAtUtc = membership.StartDateUtc,
                Method = PaymentMethod.Cash,
                Reference = "DEMO-CASH-001",
                Notes = "Seed payment",
                CreatedAtUtc = membership.StartDateUtc
            };

            db.Payments.Add(payment);
            await db.SaveChangesAsync();

            db.Invoices.Add(new Invoice
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                GymId = gym.Id,
                PaymentId = payment.Id,
                InvoiceNumber = 1,
                IssuedAtUtc = payment.PaidAtUtc,
                GymPanNumber = gym.PanNumber,
                GymBusinessName = gym.Name,
                GymAddress = gym.Address
            });

            await db.SaveChangesAsync();
        }

        if (!await db.Attendances.IgnoreQueryFilters().AnyAsync(x => x.GymId == gym.Id && x.MemberId == member.Id))
        {
            db.Attendances.AddRange(
                new Attendance
                {
                    Id = Guid.Parse("55555555-5555-5555-5555-555555555551"),
                    GymId = gym.Id,
                    MemberId = member.Id,
                    CheckedInAtUtc = DateTime.UtcNow.Date.AddDays(-3).AddHours(7),
                    Source = "QrCode"
                },
                new Attendance
                {
                    Id = Guid.Parse("55555555-5555-5555-5555-555555555552"),
                    GymId = gym.Id,
                    MemberId = member.Id,
                    CheckedInAtUtc = DateTime.UtcNow.Date.AddDays(-1).AddHours(7),
                    Source = "QrCode"
                });

            await db.SaveChangesAsync();
        }

        WorkoutPlanTemplate? template = await db.WorkoutPlanTemplates
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.GymId == gym.Id && x.Name == "Demo Push Pull Legs");

        if (template is null)
        {
            template = new WorkoutPlanTemplate
            {
                Id = Guid.Parse("66666666-6666-6666-6666-666666666666"),
                GymId = gym.Id,
                Name = "Demo Push Pull Legs",
                Description = "Starter PPL template for the demo member.",
                PlanJson = """{"days":[{"name":"Push","exercises":["Bench Press","Shoulder Press"]},{"name":"Pull","exercises":["Lat Pulldown","Seated Row"]},{"name":"Legs","exercises":["Squat","Leg Press"]}]}""",
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            };

            db.WorkoutPlanTemplates.Add(template);
            await db.SaveChangesAsync();
        }

        if (!await db.WorkoutPlanAssignments.IgnoreQueryFilters().AnyAsync(x => x.GymId == gym.Id && x.MemberId == member.Id && x.IsActive))
        {
            db.WorkoutPlanAssignments.Add(new WorkoutPlanAssignment
            {
                Id = Guid.Parse("77777777-7777-7777-7777-777777777777"),
                GymId = gym.Id,
                MemberId = member.Id,
                WorkoutPlanTemplateId = template.Id,
                AssignedAtUtc = DateTime.UtcNow,
                IsActive = true
            });

            await db.SaveChangesAsync();
        }

        if (!await db.BodyMeasurements.IgnoreQueryFilters().AnyAsync(x => x.GymId == gym.Id && x.MemberId == member.Id))
        {
            db.BodyMeasurements.Add(new BodyMeasurement
            {
                Id = Guid.Parse("88888888-8888-8888-8888-888888888888"),
                GymId = gym.Id,
                MemberId = member.Id,
                RecordedAtUtc = DateTime.UtcNow.Date.AddDays(-5),
                WeightKg = 72.5m,
                HeightCm = 175m
            });

            await db.SaveChangesAsync();
        }

        if (!await db.ChurnPredictions.IgnoreQueryFilters().AnyAsync(x => x.GymId == gym.Id && x.MemberId == member.Id))
        {
            db.ChurnPredictions.Add(new ChurnPrediction
            {
                Id = Guid.Parse("99999999-9999-9999-9999-999999999999"),
                GymId = gym.Id,
                MemberId = member.Id,
                ScoredAtUtc = DateTime.UtcNow,
                Probability = 0.18,
                RiskLevel = "Low",
                ModelVersion = "Seed",
                FeatureImportanceJson = """{"visitsPerWeek":0.4,"daysSinceLastVisit":0.2,"paymentHistory":0.2}"""
            });

            await db.SaveChangesAsync();
        }

        _ = quarterly;
        _ = annual;
    }

    private static async Task<Member> GetOrCreateMemberAsync(
        ApplicationDbContext db,
        Guid gymId)
    {
        Member? member = await db.Members
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.GymId == gymId && x.Email == "member@rackroom.local");

        if (member is not null)
            return member;

        member = new Member
        {
            Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            GymId = gymId,
            FirstName = "Demo",
            LastName = "Member",
            Email = "member@rackroom.local",
            CreatedAtUtc = DateTime.UtcNow
        };

        db.Members.Add(member);
        await db.SaveChangesAsync();
        return member;
    }

    private static async Task<MembershipPlan> GetOrCreatePlanAsync(
        ApplicationDbContext db,
        Guid gymId,
        string name,
        int durationMonths,
        decimal price)
    {
        MembershipPlan? plan = await db.MembershipPlans
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.GymId == gymId && x.Name == name);

        if (plan is not null)
            return plan;

        plan = new MembershipPlan
        {
            Id = Guid.NewGuid(),
            GymId = gymId,
            Name = name,
            DurationMonths = durationMonths,
            Price = price,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        db.MembershipPlans.Add(plan);
        await db.SaveChangesAsync();
        return plan;
    }

    private static async Task CreateUserAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string password,
        string role,
        Guid gymId)
    {
        ApplicationUser? existing = await userManager.FindByEmailAsync(email);

        if (existing is not null)
            return;

        ApplicationUser user = new()
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            GymId = gymId
        };

        IdentityResult result = await userManager.CreateAsync(user, password);

        if (!result.Succeeded)
        {
            string errors = string.Join("; ", result.Errors.Select(x => x.Description));
            throw new InvalidOperationException($"Could not create {role} user: {errors}");
        }

        await userManager.AddToRoleAsync(user, role);
    }
}
