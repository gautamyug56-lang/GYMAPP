using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Rackroom.Application.Abstractions;
using Rackroom.Application.Services;
using Rackroom.Infrastructure.Data;
using Rackroom.Infrastructure.Identity;
using Rackroom.Infrastructure.Tenant;
using Serilog;
using QuestPDF.Infrastructure;
using Rackroom.Infrastructure.Pdf;
using Rackroom.Infrastructure.Security;
using Rackroom.Infrastructure.QrCode;
using Rackroom.Infrastructure.Email;
using Rackroom.Web.Background;
using Rackroom.Application.ML;
using Rackroom.Infrastructure.ML;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

string connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=rackroom.db";

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentGymContext, CurrentGymContext>();
builder.Services.AddScoped<IRackroomDataStore, RackroomDataStore>();
builder.Services.AddScoped<IMemberManagementService, MemberManagementService>();
builder.Services.AddScoped<IMembershipManagementService, MembershipManagementService>();
builder.Services.AddScoped<IPaymentManagementService, PaymentManagementService>();
builder.Services.AddScoped<IInvoicePdfGenerator, QuestPdfInvoiceGenerator>();
builder.Services.AddScoped<IInvoicePdfStorage, FileInvoicePdfStorage>();
builder.Services.AddScoped<IAttendanceManagementService, AttendanceManagementService>();
builder.Services.AddScoped<IAttendanceTokenService, AttendanceTokenService>();
builder.Services.AddScoped<IQrCodeGenerator, QrCodeGeneratorService>();
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.Configure<SmtpOptions>(
    builder.Configuration.GetSection("Smtp"));
builder.Services.AddHostedService<ExpiryReminderBackgroundService>();
builder.Services.AddScoped<IAdminDashboardService, AdminDashboardService>();
builder.Services.AddScoped<IMemberPortalService, MemberPortalService>();
builder.Services.AddScoped<IWorkoutPlanManagementService, WorkoutPlanManagementService>();
builder.Services.AddScoped<IChurnModelStore>(_ => new FileChurnModelStore(builder.Environment.ContentRootPath));
builder.Services.AddScoped<IChurnModelTrainer, ChurnModelTrainer>();
builder.Services.AddScoped<IChurnScoringService, ChurnScoringService>();
builder.Services.AddHostedService<ChurnScoringBackgroundService>();

QuestPDF.Settings.License = LicenseType.Community;

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>,
    CustomUserClaimsPrincipalFactory>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("StaffOrAdmin", policy => policy.RequireRole("Staff", "Admin"));
    options.AddPolicy("MemberOnly", policy => policy.RequireRole("Member"));
    options.AddPolicy("StaffOrAdminOrMember", policy => policy.RequireRole("Staff", "Admin", "Member"));
});

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizePage("/Admin/Test", "AdminOnly");
    options.Conventions.AuthorizePage("/Admin/Dashboard", "AdminOnly");
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseSerilogRequestLogging();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();

await DbSeeder.SeedAsync(app.Services);

app.Run();

public partial class Program { }
