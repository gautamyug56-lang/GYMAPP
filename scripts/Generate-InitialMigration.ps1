$ErrorActionPreference = "Stop"

Write-Host "Generating Rackroom InitialCreate migration..."

dotnet ef migrations add InitialCreate `
    --project Rackroom.Infrastructure `
    --startup-project Rackroom.Web `
    --context ApplicationDbContext `
    --output-dir Migrations

Write-Host "Applying migrations..."
dotnet ef database update `
    --project Rackroom.Infrastructure `
    --startup-project Rackroom.Web `
    --context ApplicationDbContext

Write-Host "Stage 1 migration complete."
