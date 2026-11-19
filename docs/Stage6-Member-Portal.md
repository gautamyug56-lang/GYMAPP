# Stage 6 — Member Portal

The Member portal is mobile-first and restricted to the Member role.

## Features

- active membership status and expiry
- cancellation-request status
- membership pause request
- cancellation request
- member invoices
- recent attendance
- assigned Push/Pull/Legs workout plan
- monthly weight and height entry
- Chart.js progress chart

Member requests use the existing business rules. A member cannot modify another member's membership because the portal service checks the authenticated MemberId.

Staff/Admin can create workout templates and assign them to members. Assigning a new template ends the previous active assignment.

Body measurements are limited to one entry per calendar month and values are rounded to two decimals.

Members can download only their own invoices.

## Migration

No new Stage 6 table is required because the domain tables were created in Stage 1. Run the migration script locally to verify the current model:

```powershell
dotnet build
dotnet test
.\scripts\Generate-Stage6Migration.ps1
```

Commit generated migration files only when EF reports an actual model change.
