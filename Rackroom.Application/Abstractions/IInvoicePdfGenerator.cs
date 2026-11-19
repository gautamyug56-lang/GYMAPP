namespace Rackroom.Application.Abstractions;

public sealed record InvoicePdfModel(
    int InvoiceNumber,
    DateTime IssuedAtUtc,
    string GymBusinessName,
    string GymPanNumber,
    string GymAddress,
    string MemberName,
    string MemberEmail,
    string MembershipPlanName,
    decimal Amount,
    string PaymentMethod,
    string? Reference);

public interface IInvoicePdfGenerator
{
    byte[] Generate(InvoicePdfModel model);
}
