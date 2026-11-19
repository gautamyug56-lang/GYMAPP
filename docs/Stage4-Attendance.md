# Stage 4 — Attendance

## What was added

Rackroom now supports:

- rotating QR code at the gym desk
- member QR check-in
- validation of QR token, gym, member status, and active membership
- staff-assisted check-in
- member attendance history
- staff/admin gym attendance history
- basic attendance statistics
- `MemberId` linkage on member Identity accounts

## QR security

The desk QR contains a URL with a short-lived, protected token.

The token:

1. is created for the current gym
2. expires after 30 seconds
3. is protected with ASP.NET Core Data Protection
4. is validated again when the member submits the check-in

The QR token does not contain a member ID. The authenticated member account supplies the member identity.

The QR page refreshes every 20 seconds so the displayed code is replaced before expiry.

## Check-in validation

A QR check-in is accepted only when:

- the user is authenticated as a Member
- the account has a linked `MemberId`
- the QR token is valid and not expired
- the token belongs to the current gym
- the member exists in the current gym
- the member is active
- the member has an active membership at the check-in time

Staff-assisted check-in uses the same member and active-membership validation but records `Source = StaffAssisted`.

## Statistics

The attendance page shows:

- total check-ins
- current-month check-ins
- last-30-days check-ins
- distinct days visited

The staff/admin view shows gym-wide statistics. A member sees only their own statistics.

## Security

The check-in confirmation is a Razor Pages POST and therefore receives the normal Razor Pages antiforgery protection.

The QR token is cryptographically protected and time-limited using ASP.NET Core Data Protection.

Tenant filtering continues to be enforced through `GymId` and EF Core global query filters.

## Package

QRCoder `1.8.1` is used for PNG QR generation.

## Migration

Stage 4 changes the Identity user table by adding nullable `MemberId`.

Generate and apply the migration locally:

```powershell
dotnet build
dotnet test
.\scripts\Generate-Stage4Migration.ps1
```

The generated migration files under `Rackroom.Infrastructure/Migrations/` should be committed to Git.

## Important design choice

The QR code is intentionally short-lived rather than being a permanent gym URL. A copied screenshot therefore becomes unusable after its 30-second lifetime.

This stage does not add duplicate check-in suppression because the current product requirements do not say that a member may check in only once per day. Multiple genuine visits remain representable.
