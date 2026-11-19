using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Rackroom.Infrastructure.Data;
using Rackroom.Infrastructure.Tenant;

namespace Rackroom.Tests;

public class Stage1ModelTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        return new ApplicationDbContext(
            options,
            new CurrentGymContext(new HttpContextAccessor()));
    }

    [Fact]
    public void Required_tenant_entities_have_GymId_and_query_filters()
    {
        using ApplicationDbContext db = CreateContext();

        string[] expected =
        [
            "Gym",
            "Member",
            "MembershipPlan",
            "Membership",
            "MembershipPause",
            "Payment",
            "Invoice",
            "Attendance",
            "WorkoutPlanTemplate",
            "WorkoutPlanAssignment",
            "BodyMeasurement",
            "CancellationRecord",
            "ChurnPrediction"
        ];

        foreach (string name in expected)
        {
            var entity = db.Model.FindEntityType($"Rackroom.Domain.Entities.{name}");

            Assert.NotNull(entity);
            Assert.NotNull(entity!.FindProperty("GymId"));
            Assert.NotNull(entity.GetQueryFilter());
        }
    }

    [Fact]
    public void Per_gym_unique_indexes_are_configured()
    {
        using ApplicationDbContext db = CreateContext();

        AssertUniqueIndex<Domain.Entities.Member>(db, "GymId", "Email");
        AssertUniqueIndex<Domain.Entities.MembershipPlan>(db, "GymId", "Name");
        AssertUniqueIndex<Domain.Entities.Invoice>(db, "GymId", "InvoiceNumber");
        AssertUniqueIndex<Domain.Entities.WorkoutPlanTemplate>(db, "GymId", "Name");
        AssertUniqueIndex<Domain.Entities.CancellationRecord>(db, "GymId", "MembershipId");
    }

    private static void AssertUniqueIndex<TEntity>(
        ApplicationDbContext db,
        params string[] propertyNames)
    {
        var entity = db.Model.FindEntityType(typeof(TEntity));
        Assert.NotNull(entity);

        var index = entity!.GetIndexes()
            .SingleOrDefault(x =>
                x.IsUnique &&
                x.Properties.Select(p => p.Name).SequenceEqual(propertyNames));

        Assert.NotNull(index);
    }
}
