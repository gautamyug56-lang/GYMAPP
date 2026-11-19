using Microsoft.Extensions.Hosting;
using Rackroom.Application.Abstractions;

namespace Rackroom.Infrastructure.Pdf;

public sealed class FileInvoicePdfStorage : IInvoicePdfStorage
{
    private readonly string _rootDirectory;

    public FileInvoicePdfStorage(IHostEnvironment environment)
    {
        _rootDirectory = Path.Combine(
            environment.ContentRootPath,
            "App_Data",
            "invoices");
    }

    public async Task<string> SaveAsync(
        Guid gymId,
        int invoiceNumber,
        byte[] pdf,
        CancellationToken cancellationToken = default)
    {
        string gymDirectory = Path.Combine(
            _rootDirectory,
            gymId.ToString("N"));

        Directory.CreateDirectory(gymDirectory);

        string fileName = $"invoice-{invoiceNumber}.pdf";
        string fullPath = Path.Combine(gymDirectory, fileName);

        await File.WriteAllBytesAsync(fullPath, pdf, cancellationToken);

        return Path.Combine(
            "App_Data",
            "invoices",
            gymId.ToString("N"),
            fileName);
    }

    public async Task<byte[]?> ReadAsync(
        Guid gymId,
        int invoiceNumber,
        CancellationToken cancellationToken = default)
    {
        string fullPath = Path.Combine(
            _rootDirectory,
            gymId.ToString("N"),
            $"invoice-{invoiceNumber}.pdf");

        if (!File.Exists(fullPath))
            return null;

        return await File.ReadAllBytesAsync(fullPath, cancellationToken);
    }
}
