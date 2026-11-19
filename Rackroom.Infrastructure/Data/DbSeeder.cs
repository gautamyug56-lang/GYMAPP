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

        await db.Database.EnsureCreatedAsync();

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
                Id = Guid.NewGuid(),
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

        if (!await db.Members.IgnoreQueryFilters().AnyAsync())
        {
            db.Members.Add(new Member
            {
                Id = Guid.NewGuid(),
                GymId = gym.Id,
                FirstName = "Demo",
                LastName = "Member",
                Email = "member@rackroom.local",
                CreatedAtUtc = DateTime.UtcNow
            });

            await db.SaveChangesAsync();
        }
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
