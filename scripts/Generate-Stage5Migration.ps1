$ErrorActionPreference = "Stop"

Write-Host "Generating Rackroom Stage 5 migration..."

dotnet ef migrations add Stage5DashboardAndExpiryReminders `
    --project Rackroom.Infrastructure `
    --startup-project Rackroom.Web `
    --context ApplicationDbContext `
    --output-dir Migrations

Write-Host "Applying migrations..."
dotnet ef database update `
    --project Rackroom.Infrastructure `
    --startup-project Rackroom.Web `
    --context ApplicationDbContext

Write-Host "Stage 5 migration complete."
