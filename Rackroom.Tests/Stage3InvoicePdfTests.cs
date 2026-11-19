using Rackroom.Application.Abstractions;
using Rackroom.Infrastructure.Pdf;

namespace Rackroom.Tests;

public class Stage3InvoicePdfTests
{
    [Fact]
    public void QuestPdf_generator_returns_a_pdf_document()
    {
        var generator = new QuestPdfInvoiceGenerator();

        byte[] pdf = generator.Generate(new InvoicePdfModel(
            17,
            new DateTime(2026, 11, 19, 10, 0, 0, DateTimeKind.Utc),
            "Demo Gym",
            "123456789",
            "Kathmandu",
            "Jane Doe",
            "jane@example.com",
            "Monthly",
            3000.25m,
            "Cash",
            "cash-17"));

        Assert.NotEmpty(pdf);
        Assert.Equal("%PDF-", System.Text.Encoding.ASCII.GetString(pdf, 0, 5));
    }
}
