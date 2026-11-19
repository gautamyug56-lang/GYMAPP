using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Rackroom.Domain.Entities;
using Rackroom.Infrastructure.Data;
using Rackroom.Infrastructure.Tenant;

namespace Rackroom.Tests;

public class MultiTenancyTests
{
    [Fact]
    public async Task Gym_A_user_cannot_see_Gym_B_members()
    {
        Guid gymA = Guid.NewGuid();
        Guid gymB = Guid.NewGuid();

        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim("GymId", gymA.ToString()),
                new Claim(ClaimTypes.Role, "Admin")
            ], "Test"))
        };

        var accessor = new HttpContextAccessor { HttpContext = httpContext };
        var gymContext = new CurrentGymContext(accessor);

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        await using var db = new ApplicationDbContext(options, gymContext);
        await db.Database.OpenConnectionAsync();
        await db.Database.EnsureCreatedAsync();

        db.Gyms.AddRange(
            new Gym { Id = gymA, Name = "Gym A" },
            new Gym { Id = gymB, Name = "Gym B" });

        db.Members.AddRange(
            new Member { Id = Guid.NewGuid(), GymId = gymA, FirstName = "Alice", LastName = "A", Email = "alice@gym-a.test" },
            new Member { Id = Guid.NewGuid(), GymId = gymB, FirstName = "Bob", LastName = "B", Email = "bob@gym-b.test" });

        await db.SaveChangesAsync();

        List<Member> visible = await db.Members.ToListAsync();

        Assert.Single(visible);
        Assert.Equal("Alice", visible[0].FirstName);
        Assert.Equal(gymA, visible[0].GymId);
    }
}
