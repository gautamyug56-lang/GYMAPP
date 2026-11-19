# Rackroom

Rackroom is a multi-tenant gym management system for gyms in Nepal.

## Stage 0

- ASP.NET Core Razor Pages on .NET 10
- SQLite + EF Core
- ASP.NET Core Identity
- Member, Staff and Admin roles
- GymId claim and EF Core global query filter
- Serilog request logging
- Seeded demo gym and demo users
- Login/logout and Admin-protected test page
- xUnit tests for tenant isolation and authorization

## Demo accounts

Admin: admin@rackroom.local / Admin123!

Staff: staff@rackroom.local / Staff123!

Member: member@rackroom.local / Member123!

These credentials are for local development only.

## Run

    dotnet restore
    dotnet build
    dotnet test
    dotnet run --project Rackroom.Web
