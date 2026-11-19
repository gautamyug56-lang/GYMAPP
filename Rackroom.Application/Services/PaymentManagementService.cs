using Rackroom.Application.Abstractions;
using Rackroom.Application.Exceptions;
using Rackroom.Domain.Entities;

namespace Rackroom.Application.Services;

public interface IPaymentManagementService
{
    Task<PaymentReceipt> RecordPaymentAsync(
        Guid membershipId,
        decimal amount,
        DateTime paidAtUtc,
        PaymentMethod method,
        string? reference,
        string? notes,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Payment>> GetPaymentsAsync(
        CancellationToken cancellationToken = default);

    Task<decimal> GetRevenueTotalAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default);
}

public sealed record PaymentReceipt(
    Payment Payment,
    Invoice Invoice);

public sealed class PaymentManagementService : IPaymentManagementService
{
    private readonly ICurrentGymContext _currentGymContext;
    private readonly IRackroomDataStore _dataStore;
    private readonly IInvoicePdfGenerator _pdfGenerator;
    private readonly IInvoicePdfStorage _pdfStorage;

    public PaymentManagementService(
        ICurrentGymContext currentGymContext,
        IRackroomDataStore dataStore,
        IInvoicePdfGenerator pdfGenerator,
        IInvoicePdfStorage pdfStorage)
    {
        _currentGymContext = currentGymContext;
        _dataStore = dataStore;
        _pdfGenerator = pdfGenerator;
        _pdfStorage = pdfStorage;
    }

    public async Task<PaymentReceipt> RecordPaymentAsync(
        Guid membershipId,
        decimal amount,
        DateTime paidAtUtc,
        PaymentMethod method,
        string? reference,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        Guid gymId = GetGymId();

        if (amount <= 0)
            throw new BusinessRuleException("Payment amount must be greater than zero.");

        if (paidAtUtc.Kind != DateTimeKind.Utc)
            throw new BusinessRuleException("Payment date must be UTC.");

        Membership membership = await _dataStore.GetMembershipForPaymentAsync(
                gymId, membershipId, cancellationToken)
            ?? throw new BusinessRuleException("Membership was not found in the current gym.");

        if (membership.Member is null)
            throw new BusinessRuleException("The membership has no member.");

        if (!membership.Member.IsActive)
            throw new BusinessRuleException("An inactive member cannot make a membership payment.");

        if (membership.Status == MembershipStatus.Cancelled)
            throw new BusinessRuleException("A cancelled membership cannot receive a payment.");

        decimal normalizedAmount = decimal.Round(
            amount, 2, MidpointRounding.AwayFromZero);

        Payment payment = new()
        {
            Id = Guid.NewGuid(),
            GymId = gymId,
            MembershipId = membership.Id,
            MemberId = membership.MemberId,
            Amount = normalizedAmount,
            PaidAtUtc = paidAtUtc,
            Method = method,
            Reference = NormalizeOptional(reference),
            Notes = NormalizeOptional(notes),
            CreatedAtUtc = DateTime.UtcNow
        };

        membership.LastPaidAtUtc = paidAtUtc;

        Invoice invoice = new()
        {
            Id = Guid.NewGuid(),
            GymId = gymId,
            PaymentId = payment.Id,
            IssuedAtUtc = DateTime.UtcNow,
            GymPanNumber = membership.Gym?.PanNumber,
            GymBusinessName = membership.Gym?.Name,
            GymAddress = membership.Gym?.Address
        };

        await _dataStore.AddPaymentAndInvoiceAsync(
            payment, invoice, cancellationToken);

        InvoicePdfModel pdfModel = new(
            invoice.InvoiceNumber,
            invoice.IssuedAtUtc,
            invoice.GymBusinessName ?? "Rackroom Gym",
            invoice.GymPanNumber ?? "Not provided",
            invoice.GymAddress ?? "Not provided",
            $"{membership.Member.FirstName} {membership.Member.LastName}",
            membership.Member.Email,
            membership.MembershipPlan?.Name ?? "Membership",
            payment.Amount,
            payment.Method.ToString(),
            payment.Reference);

        byte[] pdf = _pdfGenerator.Generate(pdfModel);

        invoice.PdfPath = await _pdfStorage.SaveAsync(
            gymId, invoice.InvoiceNumber, pdf, cancellationToken);

        await _dataStore.SaveChangesAsync(cancellationToken);

        return new PaymentReceipt(payment, invoice);
    }

    public Task<IReadOnlyList<Payment>> GetPaymentsAsync(
        CancellationToken cancellationToken = default)
    {
        return _dataStore.GetPaymentsAsync(GetGymId(), cancellationToken);
    }

    public Task<decimal> GetRevenueTotalAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default)
    {
        if (fromUtc.Kind != DateTimeKind.Utc || toUtc.Kind != DateTimeKind.Utc)
            throw new BusinessRuleException("Revenue date range must be UTC.");

        if (toUtc < fromUtc)
            throw new BusinessRuleException("Revenue end date cannot be before its start date.");

        return _dataStore.GetPaymentTotalAsync(
            GetGymId(), fromUtc, toUtc, cancellationToken);
    }

    private Guid GetGymId()
    {
        return _currentGymContext.GymId
            ?? throw new BusinessRuleException("No gym is associated with the current user.");
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
