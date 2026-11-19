# Stage 1 domain model

The entities in this folder represent the core Rackroom gym-management domain.

All tenant-owned entities carry GymId. EF Core global query filters enforce tenant isolation at query time.

Business-rule validation such as membership pause limits, monthly body-measurement limits, cancellation timing, and invoice sequencing belongs in Application services rather than in the domain entity setters.
