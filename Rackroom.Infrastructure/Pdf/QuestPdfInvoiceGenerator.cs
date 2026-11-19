using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Rackroom.Application.Abstractions;

namespace Rackroom.Infrastructure.Pdf;

public sealed class QuestPdfInvoiceGenerator : IInvoicePdfGenerator
{
    public byte[] Generate(InvoicePdfModel model)
    {
        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(container =>
                {
                    container.Column(column =>
                    {
                        column.Item()
                            .Text(model.GymBusinessName)
                            .FontSize(20)
                            .SemiBold();

                        column.Item().Text($"PAN: {model.GymPanNumber}");
                        column.Item().Text(model.GymAddress);

                        column.Item()
                            .PaddingTop(12)
                            .Text($"PAN Invoice #{model.InvoiceNumber}")
                            .FontSize(16)
                            .SemiBold();

                        column.Item().Text(
                            $"Issued: {model.IssuedAtUtc:yyyy-MM-dd HH:mm} UTC");
                    });
                });

                page.Content()
                    .PaddingVertical(25)
                    .Column(column =>
                    {
                        column.Spacing(15);

                        column.Item()
                            .Border(1)
                            .Padding(10)
                            .Column(customer =>
                            {
                                customer.Item().Text("Customer").Bold();
                                customer.Item().Text(model.MemberName);
                                customer.Item().Text(model.MemberEmail);
                            });

                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(4);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(HeaderCell).Text("Description");
                                header.Cell().Element(HeaderCell).AlignRight().Text("Payment method");
                                header.Cell().Element(HeaderCell).AlignRight().Text("Amount");

                                static IContainer HeaderCell(IContainer container)
                                {
                                    return container
                                        .Background(Colors.Grey.Lighten2)
                                        .BorderBottom(1)
                                        .Padding(8)
                                        .DefaultTextStyle(x => x.SemiBold());
                                }
                            });

                            table.Cell().Element(Cell).Text(model.MembershipPlanName);
                            table.Cell().Element(Cell).AlignRight().Text(model.PaymentMethod);
                            table.Cell().Element(Cell).AlignRight().Text($"NPR {model.Amount:N2}");

                            static IContainer Cell(IContainer container)
                            {
                                return container
                                    .BorderBottom(1)
                                    .Padding(8);
                            }
                        });

                        column.Item()
                            .AlignRight()
                            .Text($"Total paid: NPR {model.Amount:N2}")
                            .FontSize(14)
                            .SemiBold();

                        if (!string.IsNullOrWhiteSpace(model.Reference))
                        {
                            column.Item().Text(text =>
                            {
                                text.Span("Reference: ").SemiBold();
                                text.Span(model.Reference);
                            });
                        }

                        column.Item()
                            .PaddingTop(20)
                            .Text("Thank you for your payment.");
                    });

                page.Footer()
                    .AlignCenter()
                    .Text(text =>
                    {
                        text.Span("Rackroom • ");
                        text.Span("Page ");
                        text.CurrentPageNumber();
                        text.Span(" / ");
                        text.TotalPages();
                    });
            });
        }).GeneratePdf();
    }
}
