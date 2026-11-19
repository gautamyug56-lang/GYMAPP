$ErrorActionPreference = "Stop"

Write-Host "Generating Rackroom Stage 6 migration..."

dotnet ef migrations add Stage6MemberPortal `
    --project Rackroom.Infrastructure `
    --startup-project Rackroom.Web `
    --context ApplicationDbContext `
    --output-dir Migrations

Write-Host "Applying migrations..."
dotnet ef database update `
    --project Rackroom.Infrastructure `
    --startup-project Rackroom.Web `
    --context ApplicationDbContext

Write-Host "Stage 6 migration complete."