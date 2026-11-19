namespace Rackroom.Domain.Entities;

public class Invoice
{
    public Guid Id { get; set; }
    public Guid GymId { get; set; }
    public Guid PaymentId { get; set; }
    public int InvoiceNumber { get; set; }
    public DateTime IssuedAtUtc { get; set; }
    public string? GymPanNumber { get; set; }
    public string? GymBusinessName { get; set; }
    public string? GymAddress { get; set; }
    public string? PdfPath { get; set; }

    public Gym? Gym { get; set; }
    public Payment? Payment { get; set; }
}
