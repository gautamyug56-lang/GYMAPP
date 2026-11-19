# Stage 3 — Payments and Invoices

## What was added

- Staff/Admin can record a membership payment.
- Payment amount is stored as decimal(18,2).
- Payment amount is rounded to two decimal places using MidpointRounding.AwayFromZero.
- The payment is linked to the member and membership.
- Membership.LastPaidAtUtc is updated when a payment is recorded.
- A payment cannot be recorded for a cancelled membership.
- An inactive member cannot make a payment.
- Each payment creates exactly one invoice.
- Invoice numbers are sequential within each gym and start at 1.
- Invoice numbers are protected by the existing (GymId, InvoiceNumber) unique index.
- A PDF invoice is generated with QuestPDF.
- The PDF is stored outside wwwroot under App_Data/invoices/{gymId}/.
- Staff/Admin can download the PDF through a tenant-aware Razor Page endpoint.
- Revenue totals are calculated with decimal.

## Invoice numbering

SQLite does not support database sequences, so Rackroom uses the existing per-gym invoice table and an explicit database transaction:

1. Start a transaction.
2. Read the current maximum invoice number for the gym.
3. Assign max + 1.
4. Insert the payment and invoice.
5. Save the changes.
6. Commit.

SQLite permits only one transaction with pending database changes at a time and uses serializable isolation by default, so the write transaction prevents two committed invoices from sharing the same number. The unique (GymId, InvoiceNumber) index is an additional database-level guard.

## Payment flow

```
Staff/Admin
    |
    v
Record Payment page
    |
    v
PaymentManagementService
    |
    +--> validate amount/date/member/membership
    |
    +--> update Membership.LastPaidAtUtc
    |
    v
IRackroomDataStore
    |
    +--> create Payment
    +--> allocate InvoiceNumber
    +--> create Invoice
    |
    v
QuestPDF
    |
    v
App_Data/invoices/{GymId}/invoice-N.pdf
```

The business rules remain in PaymentManagementService. EF transaction mechanics and database queries remain in Infrastructure.

## Decimal totals

EF Core's SQLite provider supports decimal aggregate translation in EF Core 9+, including Sum(decimal). Rackroom uses EF Core 10.0.15 and decimal rather than double for payment money. The Stage 3 SQLite integration test verifies:

- 3000.10 + 250.25 = 3250.35
- invoice numbers 1 then 2

## PDF licensing

Rackroom uses QuestPDF 2026.9.2. The current QuestPDF Community License permits qualifying academic and learning use, and QuestPDF requires the selected license to be configured in application startup. Rackroom configures LicenseType.Community.

For the final commercial deployment of a real gym, the gym/operator should verify its eligibility under QuestPDF's current license terms before production use.

## Tests

Stage3PaymentTests.cs covers:

- positive amount validation
- two-decimal rounding
- membership LastPaidAtUtc update
- PDF generation
- PDF storage path
- decimal revenue result

Stage3SqlitePaymentTests.cs covers real SQLite persistence:

- sequential invoice numbering per gym
- decimal payment total

## Migration

Generate the EF migration locally because the current development environment does not have the .NET SDK available:

```powershell
dotnet ef migrations add Stage3PaymentsAndInvoices --project Rackroom.Infrastructure --startup-project Rackroom.Web --context ApplicationDbContext --output-dir Migrations
dotnet ef database update --project Rackroom.Infrastructure --startup-project Rackroom.Web --context ApplicationDbContext
```

Or:

```powershell
.\scripts\Generate-Stage3Migration.ps1
```

Commit the generated EF migration files to the repository.
