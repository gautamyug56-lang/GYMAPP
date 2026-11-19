using Moq;
using Rackroom.Application.Abstractions;
using Rackroom.Application.Exceptions;
using Rackroom.Application.Services;
using Rackroom.Domain.Entities;

namespace Rackroom.Tests;

public class Stage3PaymentTests
{
    private static readonly Guid GymId =
        Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    private static readonly Guid MemberId =
        Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    private static readonly Guid MembershipId =
        Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    [Fact]
    public async Task Record_payment_rounds_to_two_decimal_places_and_updates_membership()
    {
        Membership membership = CreateMembership();

        var store = new Mock<IRackroomDataStore>();
        store.Setup(x => x.GetMembershipForPaymentAsync(
                GymId,
                MembershipId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(membership);

        store.Setup(x => x.AddPaymentAndInvoiceAsync(
                It.IsAny<Payment>(),
                It.IsAny<Invoice>(),
                It.IsAny<CancellationToken>()))
            .Callback<Payment, Invoice, CancellationToken>((payment, invoice, _) =>
            {
                invoice.InvoiceNumber = 4;
            })
            .Returns(Task.CompletedTask);

        var pdfGenerator = new Mock<IInvoicePdfGenerator>();
        pdfGenerator.Setup(x => x.Generate(It.IsAny<InvoicePdfModel>()))
            .Returns([1, 2, 3]);

        var storage = new Mock<IInvoicePdfStorage>();
        storage.Setup(x => x.SaveAsync(
                GymId,
                4,
                It.IsAny<byte[]>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("App_Data/invoices/4/invoice-4.pdf");

        var service = new PaymentManagementService(
            CreateGymContext().Object,
            store.Object,
            pdfGenerator.Object,
            storage.Object);

        DateTime paidAt = new(
            2026, 11, 19, 10, 0, 0, DateTimeKind.Utc);

        PaymentReceipt result = await service.RecordPaymentAsync(
            MembershipId,
            3000.126m,
            paidAt,
            PaymentMethod.Cash,
            "cash-001",
            "Monthly payment");

        Assert.Equal(3000.13m, result.Payment.Amount);
        Assert.Equal(paidAt, membership.LastPaidAtUtc);
        Assert.Equal(4, result.Invoice.InvoiceNumber);
        Assert.Equal(
            "App_Data/invoices/4/invoice-4.pdf",
            result.Invoice.PdfPath);

        pdfGenerator.Verify(
            x => x.Generate(It.Is<InvoicePdfModel>(model =>
                model.Amount == 3000.13m &&
                model.InvoiceNumber == 4)),
            Times.Once);

        store.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Record_payment_rejects_zero_or_negative_amount()
    {
        var store = new Mock<IRackroomDataStore>();
        var service = new PaymentManagementService(
            CreateGymContext().Object,
            store.Object,
            Mock.Of<IInvoicePdfGenerator>(),
            Mock.Of<IInvoicePdfStorage>());

        BusinessRuleException exception = await Assert.ThrowsAsync<BusinessRuleException>(
            () => service.RecordPaymentAsync(
                MembershipId,
                0m,
                DateTime.UtcNow,
                PaymentMethod.Cash,
                null,
                null));

        Assert.Equal(
            "Payment amount must be greater than zero.",
            exception.Message);
    }

    [Fact]
    public async Task Revenue_total_preserves_decimal_precision()
    {
        var store = new Mock<IRackroomDataStore>();
        store.Setup(x => x.GetPaymentTotalAsync(
                GymId,
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(3250.35m);

        var service = new PaymentManagementService(
            CreateGymContext().Object,
            store.Object,
            Mock.Of<IInvoicePdfGenerator>(),
            Mock.Of<IInvoicePdfStorage>());

        decimal total = await service.GetRevenueTotalAsync(
            new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc));

        Assert.Equal(3250.35m, total);
    }

    private static Membership CreateMembership()
    {
        Gym gym = new()
        {
            Id = GymId,
            Name = "Demo Gym",
            PanNumber = "123456789",
            Address = "Kathmandu"
        };

        Member member = new()
        {
            Id = MemberId,
            GymId = GymId,
            FirstName = "Jane",
            LastName = "Doe",
            Email = "jane@example.com",
            IsActive = true,
            Gym = gym
        };

        return new Membership
        {
            Id = MembershipId,
            GymId = GymId,
            MemberId = MemberId,
            MembershipPlanId = Guid.NewGuid(),
            StartDateUtc = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDateUtc = new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc),
            Status = MembershipStatus.Active,
            Member = member,
            MembershipPlan = new MembershipPlan
            {
                Id = Guid.NewGuid(),
                GymId = GymId,
                Name = "Monthly",
                DurationMonths = 1,
                Price = 3000m
            },
            Gym = gym
        };
    }

    private static Mock<ICurrentGymContext> CreateGymContext()
    {
        var context = new Mock<ICurrentGymContext>();
        context.Setup(x => x.GymId).Returns(GymId);
        return context;
    }
}
