using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Rackroom.Domain.Entities;
using Rackroom.Infrastructure.Data;
using Rackroom.Application.Abstractions;

namespace Rackroom.Tests;

public class Stage3SqlitePaymentTests
{
    private static readonly Guid GymId =
        Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public async Task Sqlite_assigns_sequential_invoice_numbers_and_preserves_decimal_total()
    {
        await using SqliteConnection connection =
            new("Data Source=:memory:");

        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        var gymContext = new TestGymContext(GymId);

        await using var db = new ApplicationDbContext(options, gymContext);
        await db.Database.EnsureCreatedAsync();

        Gym gym = new()
        {
            Id = GymId,
            Name = "Integration Gym",
            PanNumber = "999999999",
            Address = "Kathmandu"
        };

        Member member = new()
        {
            Id = Guid.NewGuid(),
            GymId = GymId,
            FirstName = "Test",
            LastName = "Member",
            Email = "test@example.com",
            IsActive = true
        };

        MembershipPlan plan = new()
        {
            Id = Guid.NewGuid(),
            GymId = GymId,
            Name = "Monthly",
            DurationMonths = 1,
            Price = 3000m,
            IsActive = true
        };

        Membership membership = new()
        {
            Id = Guid.NewGuid(),
            GymId = GymId,
            MemberId = member.Id,
            MembershipPlanId = plan.Id,
            StartDateUtc = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDateUtc = new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc),
            Status = MembershipStatus.Active
        };

        db.Gyms.Add(gym);
        db.Members.Add(member);
        db.MembershipPlans.Add(plan);
        db.Memberships.Add(membership);
        await db.SaveChangesAsync();

        var store = new RackroomDataStore(db);

        Payment payment1 = new()
        {
            Id = Guid.NewGuid(),
            GymId = GymId,
            MemberId = member.Id,
            MembershipId = membership.Id,
            Amount = 3000.10m,
            PaidAtUtc = new DateTime(2026, 11, 10, 0, 0, 0, DateTimeKind.Utc),
            Method = PaymentMethod.Cash,
            CreatedAtUtc = DateTime.UtcNow
        };

        Invoice invoice1 = new()
        {
            Id = Guid.NewGuid(),
            GymId = GymId,
            PaymentId = payment1.Id,
            IssuedAtUtc = DateTime.UtcNow
        };

        await store.AddPaymentAndInvoiceAsync(payment1, invoice1);

        Payment payment2 = new()
        {
            Id = Guid.NewGuid(),
            GymId = GymId,
            MemberId = member.Id,
            MembershipId = membership.Id,
            Amount = 250.25m,
            PaidAtUtc = new DateTime(2026, 11, 11, 0, 0, 0, DateTimeKind.Utc),
            Method = PaymentMethod.BankTransfer,
            CreatedAtUtc = DateTime.UtcNow
        };

        Invoice invoice2 = new()
        {
            Id = Guid.NewGuid(),
            GymId = GymId,
            PaymentId = payment2.Id,
            IssuedAtUtc = DateTime.UtcNow
        };

        await store.AddPaymentAndInvoiceAsync(payment2, invoice2);

        Assert.Equal(1, invoice1.InvoiceNumber);
        Assert.Equal(2, invoice2.InvoiceNumber);

        decimal total = await store.GetPaymentTotalAsync(
            GymId,
            new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc));

        Assert.Equal(3250.35m, total);
    }

    private sealed class TestGymContext(Guid gymId) : ICurrentGymContext
    {
        public Guid? GymId => gymId;
    }
}
