# Stage 8 — Authorization Review

## Protected pages

| Area | Pages | Policy |
|---|---|---|
| Admin | `/Admin/Dashboard`, `/Admin/Churn/Index`, `/Admin/Churn/Train` | AdminOnly |
| Admin test | `/Admin/Test` | AdminOnly convention |
| Staff | Members, Plans, Memberships, Payments | StaffOrAdmin |
| Staff | Attendance Desk, Staff Check-In | StaffOrAdmin |
| Staff | Workout Plans | StaffOrAdmin |
| Member | Member Portal | MemberOnly |
| Member | QR Check-In | MemberOnly |
| Invoice | Invoice PDF download | StaffOrAdminOrMember + member ownership check |
| Attendance history | Member or Staff/Admin | authenticated; service selects the correct view |

Login and logout remain intentionally available without an authorization policy because they are authentication entry/exit points.

## Tenant isolation

Tenant-owned domain entities use `GymId` and EF Core global query filters.

The current gym comes from the authenticated user's `GymId` claim.

Application services also receive the current gym context before performing writes.

Member portal operations additionally use the authenticated `MemberId` claim.

## Member ownership

The member portal does not accept a member ID from the browser to decide whose data is returned.

The authenticated MemberId is used to load:

- membership
- payments
- invoices
- workout assignment
- measurements
- attendance

Pause and cancellation requests compare the requested membership's MemberId with the authenticated MemberId.

Invoice downloads perform a second ownership check before reading the PDF.

## Tests

Stage 8 adds authorization-review tests for the protected page model policies.

Existing tests cover:

- unauthenticated admin access
- multi-gym isolation
- membership pause rules
- cancellation rules
- payment validation
- invoice generation
- invoice numbering and decimal totals
- attendance validation
- QR PNG generation
- Stage 6 monthly measurement rules
- Stage 7 synthetic dataset determinism

The GitHub Actions workflow runs the complete test suite on push and pull request.

## Final manual security review

Before production deployment, manually verify:

1. Member cannot access Staff/Admin URLs.
2. Staff cannot access Admin-only pages.
3. Member A cannot download Member B's invoice.
4. Member A cannot submit a pause/cancellation for Member B.
5. Gym A users cannot retrieve Gym B records.
6. SMTP credentials are not in source control.
7. production HTTPS is enabled.
8. generated invoice/model directories are not public static files.
