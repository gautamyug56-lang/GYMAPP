# EF Core migrations

Stage 1 uses EF Core migrations instead of EnsureCreated.

Install the EF CLI once:

    dotnet tool install --global dotnet-ef

Create the initial migration from the solution root:

    dotnet ef migrations add InitialCreate --project Rackroom.Infrastructure --startup-project Rackroom.Web --context ApplicationDbContext --output-dir Migrations

Apply it:

    dotnet ef database update --project Rackroom.Infrastructure --startup-project Rackroom.Web --context ApplicationDbContext

The migration command generates the normal EF Core migration file, designer metadata file, and model snapshot. These generated files should be committed to source control.

Because Stage 0 used EnsureCreated, delete the old local rackroom.db before the first Stage 1 migration update if it already exists:

    del rackroom.db

Then run:

    dotnet ef database update --project Rackroom.Infrastructure --startup-project Rackroom.Web --context ApplicationDbContext

The application now uses MigrateAsync() at startup.

Do not use EnsureCreated together with migrations. EF Core documents that EnsureCreated bypasses migration history and should not be used before Migrate/MigrateAsync.
