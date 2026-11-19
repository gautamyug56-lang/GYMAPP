# Stage 1 — Domain model

## Relationship diagram

    Gym
    ├── Members
    │   ├── Memberships ──> MembershipPlan
    │   │      ├── MembershipPauses
    │   │      ├── Payments ──> Invoice
    │   │      └── CancellationRecord
    │   ├── Attendance
    │   ├── WorkoutPlanAssignments ──> WorkoutPlanTemplate
    │   ├── BodyMeasurements
    │   └── ChurnPredictions
    ├── MembershipPlans
    ├── Memberships
    ├── Payments
    ├── Invoices
    ├── Attendance
    ├── WorkoutPlanTemplates
    ├── WorkoutPlanAssignments
    ├── BodyMeasurements
    ├── CancellationRecords
    └── ChurnPredictions

## Tenant rule

Every tenant-owned entity has GymId. ApplicationDbContext applies a global query filter to each tenant-owned entity so normal queries only see the current gym.

Identity users also store GymId, but Identity users are intentionally not given a global query filter because login occurs before the GymId claim exists.

## Important indexes

| Entity | Index |
|---|---|
| Member | GymId |
| Member | unique GymId + Email |
| MembershipPlan | GymId |
| MembershipPlan | unique GymId + Name |
| Membership | GymId |
| Membership | GymId + MemberId + StartDateUtc |
| MembershipPause | GymId |
| MembershipPause | GymId + MembershipId + StartDateUtc |
| Payment | GymId |
| Payment | GymId + MemberId + PaidAtUtc |
| Invoice | GymId |
| Invoice | unique GymId + InvoiceNumber |
| Attendance | GymId |
| Attendance | GymId + MemberId + CheckedInAtUtc |
| WorkoutPlanTemplate | unique GymId + Name |
| WorkoutPlanAssignment | GymId + MemberId + IsActive |
| BodyMeasurement | GymId + MemberId + RecordedAtUtc |
| CancellationRecord | unique GymId + MembershipId |
| ChurnPrediction | GymId + MemberId + ScoredAtUtc |
| ChurnPrediction | GymId + RiskLevel |

Business limits such as maximum 30-day pauses, two pauses per year, and one body measurement per month belong in Application services. Database indexes enforce identity/uniqueness constraints, not those time-based business rules.
