# Stage 5 — Admin Dashboard and Expiry Reminders

## Dashboard

The Admin dashboard is available at `/Admin/Dashboard` and is restricted to the Admin role.

It provides:

- current-month revenue
- new members this month
- memberships expiring within 30 days
- payment deadlines within 7 days
- today's attendance
- six months of revenue chart data
- six months of new-member chart data
- paginated expiring memberships
- paginated payment deadlines
- paginated today's attendance

Revenue is kept as `decimal` from the database through the application layer. Chart.js receives the serialized decimal values for display.

## Payment deadline definition

Rackroom currently treats the membership end date as the upcoming payment/renewal deadline. This is intentionally simple because the current domain model does not contain a separate recurring billing schedule.

## Chart.js

The dashboard uses Chart.js 4.6.1 from jsDelivr.

The server supplies chart data; the browser only renders the charts. Missing months are explicitly filled with zero so the six-month chart always has six points.

## Pagination

The dashboard tables use a page size of 10.

Pagination is server-side:

1. the datastore counts matching records
2. only the requested page is fetched with `Skip` and `Take`
3. the Razor Page renders page links

This avoids loading an entire attendance or membership history into the browser.

## Expiry reminder background job

`ExpiryReminderBackgroundService` runs when the application starts and then every six hours.

For every gym it:

1. finds active memberships expiring within the next seven days
2. ignores memberships that already received a reminder
3. sends an email to the member's email address
4. records `ExpiryReminderSentAtUtc` only after the email succeeds

SMTP is disabled by default.

Configure SMTP using appsettings, user-secrets, or environment variables. Never commit SMTP credentials.

Example configuration shape:

```json
"Smtp": {
  "Enabled": true,
  "Host": "smtp.example.com",
  "Port": 587,
  "UseSsl": true,
  "Username": "YOUR_SMTP_USERNAME",
  "Password": "YOUR_SMTP_PASSWORD",
  "FromAddress": "noreply@example.com",
  "FromName": "Rackroom"
}
```

For local development, keep `Enabled` false until a real SMTP account is configured.

MailKit 5.0.0 is used for SMTP delivery.

## Migration

Stage 5 adds:

- `Membership.ExpiryReminderSentAtUtc`

Generate and apply locally:

```powershell
dotnet build
dotnet test
.\scripts\Generate-Stage5Migration.ps1
```

Commit the generated migration files under `Rackroom.Infrastructure/Migrations/`.

## Testing

Stage 5 tests verify:

- dashboard metric values
- six-month chart shape
- missing chart months are represented as zero
- invalid pagination values are rejected

The full integration/build verification still needs to be run in a local environment with the .NET SDK.
