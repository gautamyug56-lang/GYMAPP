# Stage 2 — Member and Membership Management

## What was added

Stage 2 adds the first real business-management workflow to Rackroom:

- Add members.
- Edit member details.
- Search members by name or email.
- Deactivate members without deleting their history.
- Create monthly, 3-month, and annual membership plans.
- Assign an active plan to an active member.
- Calculate membership expiry from the plan duration.
- Request membership pauses.
- Staff/Admin approval for pauses.
- Maximum 30 days per pause.
- Maximum two approved pauses per calendar year.
- Extend membership expiry by the approved paused days.
- Record membership cancellation reasons.
- Cancellation becomes effective at the end of the paid membership period.

## Business rules

### Members

- Email is required and normalized to lowercase.
- Email must be unique within the current gym.
- Deactivation is a soft state change (IsActive = false); member history remains intact.
- Inactive members cannot receive a new membership.

### Plans

Allowed durations are:

- 1 month
- 3 months
- 12 months

Price must be greater than zero.

Plan names must be unique within a gym.

### Membership assignment

- Member and plan must belong to the current gym.
- Member must be active.
- Plan must be active.
- A member cannot have another active membership covering the assignment start date.
- Expiry is calculated with StartDateUtc.AddMonths(DurationMonths).

### Pause

- Only active memberships can be paused.
- Start/end values must be UTC.
- The pause must be inside the membership period.
- Pause dates are treated as inclusive calendar dates.
- Maximum duration is 30 days.
- A pause cannot overlap another pending or approved pause.
- Approval is limited to Staff/Admin.
- Maximum two approved pauses per calendar year.
- Approval extends Membership.EndDateUtc by exactly PausedDays.

### Cancellation

- Only active memberships can be cancelled.
- A cancellation reason is required.
- Only one cancellation record can exist for a membership.
- EffectiveAtUtc is the membership's current end date.
- The membership is not immediately marked Cancelled; it remains active until the paid period ends. A later expiry/background workflow can transition the status.

## Architecture

Razor Pages are responsible for:

- binding form input,
- displaying validation errors,
- authorization,
- redirecting after successful operations.

Application services contain the business rules.

IRackroomDataStore provides the persistence boundary used by the Application services. The EF Core implementation lives in Infrastructure.

This keeps business rules out of Razor Page handlers and makes them unit-testable with Moq.

## Security

All management pages use the StaffOrAdmin authorization policy.

Gym identity is obtained from the authenticated user's GymId claim through ICurrentGymContext; pages do not accept a GymId from form input.

The services also query through the current gym boundary and validate referenced entities before creating relationships.

## Tests

Stage2ServiceTests.cs covers:

- member normalization,
- duplicate member email,
- member deactivation,
- invalid plan duration,
- membership expiry calculation,
- inactive member rejection,
- pause duration limit,
- overlapping pause rejection,
- staff/admin approval,
- annual pause limit,
- expiry extension after approval,
- duplicate cancellation rejection,
- cancellation effective date.

## Migration

The Stage 1 migration was not generated in the current development environment because the .NET SDK is unavailable there. Generate the migrations locally after pulling the repository.

If you have not yet generated InitialCreate, generate that first. Then generate Stage 2:

    dotnet ef migrations add Stage2MemberManagement --project Rackroom.Infrastructure --startup-project Rackroom.Web --context ApplicationDbContext --output-dir Migrations

    dotnet ef database update --project Rackroom.Infrastructure --startup-project Rackroom.Web --context ApplicationDbContext

Or run:

    .\\scripts\\Generate-Stage2Migration.ps1

The generated EF migration files should be committed to source control.
