namespace Rackroom.Application.Abstractions;

public interface IInvoicePdfStorage
{
    Task<string> SaveAsync(
        Guid gymId,
        int invoiceNumber,
        byte[] pdf,
        CancellationToken cancellationToken = default);

    Task<byte[]?> ReadAsync(
        Guid gymId,
        int invoiceNumber,
        CancellationToken cancellationToken = default);
}
