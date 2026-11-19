# Rackroom

Rackroom is an individual final-year project: a multi-tenant gym management system for gyms in Nepal.

## Technology

- .NET 10 / C#
- ASP.NET Core Razor Pages
- Bootstrap 5
- Chart.js
- EF Core + SQLite
- ASP.NET Core Identity cookie authentication
- Member / Staff / Admin roles
- authorization policies
- QuestPDF for invoice PDFs
- QRCoder for attendance QR codes
- MailKit for SMTP
- Serilog
- BackgroundService
- ML.NET for churn prediction
- xUnit + Moq + ASP.NET Core integration testing
- GitHub Actions
- Docker

## Solution structure

- `Rackroom.Web` — Razor Pages, authentication, background jobs
- `Rackroom.Application` — business rules, services, ML workflows and abstractions
- `Rackroom.Domain` — domain entities and enums
- `Rackroom.Infrastructure` — EF Core, SQLite, Identity, PDF, QR, email and model storage
- `Rackroom.Tests` — unit, integration and security tests

## Core features

### Gym and security

- multi-gym tenant isolation through `GymId`
- current gym and member claims
- EF Core global query filters
- Identity password hashing
- Member, Staff and Admin roles
- AdminOnly, StaffOrAdmin and MemberOnly policies
- member invoice ownership validation
- no generated invoice/model files under `wwwroot`

### Member management

- add/edit/search/deactivate members
- membership plans: monthly, 3-month and annual
- membership assignment
- membership expiry calculation
- pause requests and staff/admin approval
- maximum 30-day pause
- maximum two approved pauses per calendar year
- cancellation effective at the end of the paid period

### Payments and invoices

- staff/admin payment recording
- cash, bank transfer, eSewa, Khalti and other methods
- decimal-safe payment amounts
- per-gym sequential invoice numbers
- QuestPDF invoice generation
- invoice PDFs stored outside `wwwroot`
- member-owned invoice download protection

### Attendance

- rotating desk QR code
- 30-second protected QR tokens
- member check-in
- staff-assisted check-in
- attendance history
- basic attendance statistics

### Admin dashboard

- monthly revenue
- new members
- expiring memberships
- payment deadlines
- today's attendance
- Chart.js revenue/new-member charts
- server-side pagination
- expiry reminder email BackgroundService

### Member portal

- membership status
- pause/cancellation requests
- invoices
- assigned Push/Pull/Legs plan
- monthly weight/height entry
- progress chart

### Churn prediction

- deterministic synthetic dataset generator
- documented synthetic-data assumptions
- visits/week
- recent visits
- attendance decline
- days since last visit
- membership length/type
- payment history
- pauses
- SDCA Logistic Regression vs FastTree vs LightGBM
- precision, recall, F1, ROC AUC and confusion matrix
- permutation feature importance
- saved ML.NET model
- nightly scoring
- low/medium/high risk dashboard
- members currently paused are excluded from scoring

## Local development

Requirements:

- .NET 10 SDK
- Git

Run:

```powershell
dotnet restore
dotnet build
dotnet test
dotnet run --project Rackroom.Web
```

The application uses SQLite by default:

```
Data Source=rackroom.db
```

The application seeds a demo gym and development accounts.

### Demo accounts

```
Admin
admin@rackroom.local
Admin123!

Staff
staff@rackroom.local
Staff123!

Member
member@rackroom.local
Member123!
```

These passwords are for local development only. Do not use them in a deployed environment.

## EF migrations

The repository contains scripts for the staged migrations.

After changing the EF model:

```powershell
dotnet build
dotnet test
.scriptsGenerate-Stage6Migration.ps1
```

Use the matching Stage 3/4/5 script when applying those migrations. Generated migration files must be committed.

## SMTP

Expiry reminder emails are disabled by default.

Use user-secrets or environment variables for SMTP credentials.

Example environment variable names:

```
Smtp__Enabled=true
Smtp__Host=smtp.example.com
Smtp__Port=587
Smtp__UseSsl=true
Smtp__Username=...
Smtp__Password=...
Smtp__FromAddress=noreply@example.com
Smtp__FromName=Rackroom
```

Never commit real SMTP credentials.

## Churn model training

Sign in as Admin and open:

```
/Admin/Churn/Train
```

The training page generates 3,000 synthetic records, compares three ML.NET trainers, evaluates the required metrics, calculates PFI and saves:

```
App_Data/ml/churn-model.zip
App_Data/ml/churn-training-report.json
```

These generated files are ignored by Git.

The synthetic metrics must not be presented as real-world gym prediction accuracy. They measure performance against the synthetic assumptions.

## Docker

Build:

```powershell
docker build -t rackroom .
```

Run:

```powershell
docker run --rm -p 8080:8080 -v rackroom-data:/app/data -v rackroom-appdata:/app/App_Data rackroom
```

The Docker image stores SQLite data under `/app/data` and generated invoices/models under `/app/App_Data`.

## CI

GitHub Actions runs on pushes and pull requests to `main`.

The workflow:

1. installs .NET 10
2. restores packages
3. builds Release
4. runs the complete test suite

## Security checklist

- never store production secrets in source control
- use HTTPS in production
- keep SMTP credentials in environment variables/user-secrets
- keep Identity authentication enabled
- keep authorization policies on protected pages
- validate tenant and member ownership in application services
- keep generated PDFs/models outside `wwwroot`
- do not treat synthetic churn metrics as real-world accuracy
- run `dotnet test` before committing

## Current limitations

- the application currently uses SQLite rather than a server database
- Nepal PAN invoice legal requirements still need verification against current official requirements before production use
- churn training data is synthetic until anonymized real gym data becomes available
- invoice numbering uses application-managed per-gym sequencing appropriate for the current SQLite design

## Project status

Stages 0–8 are implemented in the repository.

GitHub Actions currently reports a successful Release build and test run. The latest CI run completed with all 38 tests passing.

The local .NET SDK is still required to run the application locally and generate any pending EF migrations.
