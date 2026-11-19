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
});

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizePage("/Admin/Test", "AdminOnly");
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
