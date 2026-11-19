using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Rackroom.Application.Abstractions;
using Rackroom.Domain.Entities;
using Rackroom.Infrastructure.Identity;

namespace Rackroom.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    private readonly ICurrentGymContext _currentGymContext;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ICurrentGymContext currentGymContext)
        : base(options)
    {
        _currentGymContext = currentGymContext;
    }

    public DbSet<Gym> Gyms => Set<Gym>();
    public DbSet<Member> Members => Set<Member>();
    public DbSet<MembershipPlan> MembershipPlans => Set<MembershipPlan>();
    public DbSet<Membership> Memberships => Set<Membership>();
    public DbSet<MembershipPause> MembershipPauses => Set<MembershipPause>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<Attendance> Attendances => Set<Attendance>();
    public DbSet<WorkoutPlanTemplate> WorkoutPlanTemplates => Set<WorkoutPlanTemplate>();
    public DbSet<WorkoutPlanAssignment> WorkoutPlanAssignments => Set<WorkoutPlanAssignment>();
    public DbSet<BodyMeasurement> BodyMeasurements => Set<BodyMeasurement>();
    public DbSet<CancellationRecord> CancellationRecords => Set<CancellationRecord>();
    public DbSet<ChurnPrediction> ChurnPredictions => Set<ChurnPrediction>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        ConfigureGym(builder);
        ConfigureMember(builder);
        ConfigureMembershipPlan(builder);
        ConfigureMembership(builder);
        ConfigureMembershipPause(builder);
        ConfigurePayment(builder);
        ConfigureInvoice(builder);
        ConfigureAttendance(builder);
        ConfigureWorkoutPlanTemplate(builder);
        ConfigureWorkoutPlanAssignment(builder);
        ConfigureBodyMeasurement(builder);
        ConfigureCancellationRecord(builder);
        ConfigureChurnPrediction(builder);
    }

    private void ConfigureGym(ModelBuilder builder)
    {
        builder.Entity<Gym>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.PanNumber).HasMaxLength(50);
            entity.Property(x => x.Address).HasMaxLength(500);

            entity.HasQueryFilter(x =>
                _currentGymContext.GymId != null &&
                x.Id == _currentGymContext.GymId);
        });
    }

    private void ConfigureMember(ModelBuilder builder)
    {
        builder.Entity<Member>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.LastName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(320).IsRequired();

            entity.HasIndex(x => x.GymId);
            entity.HasIndex(x => new { x.GymId, x.IsActive });
            entity.HasIndex(x => new { x.GymId, x.Email }).IsUnique();

            entity.HasOne(x => x.Gym)
                .WithMany(x => x.Members)
                .HasForeignKey(x => x.GymId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasQueryFilter(x =>
                _currentGymContext.GymId != null &&
                x.GymId == _currentGymContext.GymId);
        });
    }

    private void ConfigureMembershipPlan(ModelBuilder builder)
    {
        builder.Entity<MembershipPlan>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Price).HasPrecision(18, 2);

            entity.HasIndex(x => x.GymId);
            entity.HasIndex(x => new { x.GymId, x.Name }).IsUnique();

            entity.HasOne(x => x.Gym)
                .WithMany(x => x.MembershipPlans)
                .HasForeignKey(x => x.GymId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasQueryFilter(x =>
                _currentGymContext.GymId != null &&
                x.GymId == _currentGymContext.GymId);
        });
    }

    private void ConfigureMembership(ModelBuilder builder)
    {
        builder.Entity<Membership>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.HasIndex(x => x.GymId);
            entity.HasIndex(x => new { x.GymId, x.MemberId, x.StartDateUtc });

            entity.HasOne(x => x.Gym)
                .WithMany(x => x.Memberships)
                .HasForeignKey(x => x.GymId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Member)
                .WithMany(x => x.Memberships)
                .HasForeignKey(x => x.MemberId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.MembershipPlan)
                .WithMany(x => x.Memberships)
                .HasForeignKey(x => x.MembershipPlanId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasQueryFilter(x =>
                _currentGymContext.GymId != null &&
                x.GymId == _currentGymContext.GymId);
        });
    }

    private void ConfigureMembershipPause(ModelBuilder builder)
    {
        builder.Entity<MembershipPause>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Reason).HasMaxLength(500);

            entity.HasIndex(x => x.GymId);
            entity.HasIndex(x => new { x.GymId, x.MembershipId, x.StartDateUtc });

            entity.HasOne(x => x.Gym)
                .WithMany(x => x.MembershipPauses)
                .HasForeignKey(x => x.GymId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Membership)
                .WithMany(x => x.Pauses)
                .HasForeignKey(x => x.MembershipId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasQueryFilter(x =>
                _currentGymContext.GymId != null &&
                x.GymId == _currentGymContext.GymId);
        });
    }

    private void ConfigurePayment(ModelBuilder builder)
    {
        builder.Entity<Payment>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Amount).HasPrecision(18, 2);
            entity.Property(x => x.Reference).HasMaxLength(200);
            entity.Property(x => x.Notes).HasMaxLength(500);

            entity.HasIndex(x => x.GymId);
            entity.HasIndex(x => new { x.GymId, x.MemberId, x.PaidAtUtc });

            entity.HasOne(x => x.Gym)
                .WithMany(x => x.Payments)
                .HasForeignKey(x => x.GymId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Member)
                .WithMany(x => x.Payments)
                .HasForeignKey(x => x.MemberId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Membership)
                .WithMany(x => x.Payments)
                .HasForeignKey(x => x.MembershipId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasQueryFilter(x =>
                _currentGymContext.GymId != null &&
                x.GymId == _currentGymContext.GymId);
        });
    }

    private void ConfigureInvoice(ModelBuilder builder)
    {
        builder.Entity<Invoice>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.GymPanNumber).HasMaxLength(50);
            entity.Property(x => x.GymBusinessName).HasMaxLength(200);
            entity.Property(x => x.GymAddress).HasMaxLength(500);
            entity.Property(x => x.PdfPath).HasMaxLength(500);

            entity.HasIndex(x => x.GymId);
            entity.HasIndex(x => new { x.GymId, x.InvoiceNumber }).IsUnique();

            entity.HasOne(x => x.Gym)
                .WithMany(x => x.Invoices)
                .HasForeignKey(x => x.GymId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Payment)
                .WithOne(x => x.Invoice)
                .HasForeignKey<Invoice>(x => x.PaymentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasQueryFilter(x =>
                _currentGymContext.GymId != null &&
                x.GymId == _currentGymContext.GymId);
        });
    }

    private void ConfigureAttendance(ModelBuilder builder)
    {
        builder.Entity<Attendance>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Source).HasMaxLength(50).IsRequired();

            entity.HasIndex(x => new { x.GymId, x.MemberId, x.CheckedInAtUtc });
            entity.HasIndex(x => x.GymId);

            entity.HasOne(x => x.Gym)
                .WithMany(x => x.Attendances)
                .HasForeignKey(x => x.GymId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Member)
                .WithMany(x => x.Attendances)
                .HasForeignKey(x => x.MemberId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasQueryFilter(x =>
                _currentGymContext.GymId != null &&
                x.GymId == _currentGymContext.GymId);
        });
    }

    private void ConfigureWorkoutPlanTemplate(ModelBuilder builder)
    {
        builder.Entity<WorkoutPlanTemplate>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(150).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(1000);
            entity.Property(x => x.PlanJson).IsRequired();

            entity.HasIndex(x => new { x.GymId, x.Name }).IsUnique();

            entity.HasOne(x => x.Gym)
                .WithMany(x => x.WorkoutPlanTemplates)
                .HasForeignKey(x => x.GymId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasQueryFilter(x =>
                _currentGymContext.GymId != null &&
                x.GymId == _currentGymContext.GymId);
        });
    }

    private void ConfigureWorkoutPlanAssignment(ModelBuilder builder)
    {
        builder.Entity<WorkoutPlanAssignment>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.HasIndex(x => new { x.GymId, x.MemberId, x.IsActive });
            entity.HasIndex(x => x.GymId);

            entity.HasOne(x => x.Gym)
                .WithMany(x => x.WorkoutPlanAssignments)
                .HasForeignKey(x => x.GymId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Member)
                .WithMany(x => x.WorkoutPlanAssignments)
                .HasForeignKey(x => x.MemberId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.WorkoutPlanTemplate)
                .WithMany(x => x.Assignments)
                .HasForeignKey(x => x.WorkoutPlanTemplateId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasQueryFilter(x =>
                _currentGymContext.GymId != null &&
                x.GymId == _currentGymContext.GymId);
        });
    }

    private void ConfigureBodyMeasurement(ModelBuilder builder)
    {
        builder.Entity<BodyMeasurement>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.WeightKg).HasPrecision(6, 2);
            entity.Property(x => x.HeightCm).HasPrecision(6, 2);

            entity.HasIndex(x => new { x.GymId, x.MemberId, x.RecordedAtUtc });

            entity.HasOne(x => x.Gym)
                .WithMany(x => x.BodyMeasurements)
                .HasForeignKey(x => x.GymId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Member)
                .WithMany(x => x.BodyMeasurements)
                .HasForeignKey(x => x.MemberId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasQueryFilter(x =>
                _currentGymContext.GymId != null &&
                x.GymId == _currentGymContext.GymId);
        });
    }

    private void ConfigureCancellationRecord(ModelBuilder builder)
    {
        builder.Entity<CancellationRecord>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Reason).HasMaxLength(1000).IsRequired();

            entity.HasIndex(x => new { x.GymId, x.MembershipId }).IsUnique();

            entity.HasOne(x => x.Gym)
                .WithMany(x => x.CancellationRecords)
                .HasForeignKey(x => x.GymId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Membership)
                .WithOne(x => x.CancellationRecord)
                .HasForeignKey<CancellationRecord>(x => x.MembershipId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasQueryFilter(x =>
                _currentGymContext.GymId != null &&
                x.GymId == _currentGymContext.GymId);
        });
    }

    private void ConfigureChurnPrediction(ModelBuilder builder)
    {
        builder.Entity<ChurnPrediction>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.RiskLevel).HasMaxLength(20).IsRequired();
            entity.Property(x => x.ModelVersion).HasMaxLength(50).IsRequired();

            entity.HasIndex(x => new { x.GymId, x.MemberId, x.ScoredAtUtc });
            entity.HasIndex(x => new { x.GymId, x.RiskLevel });

            entity.HasOne(x => x.Gym)
                .WithMany(x => x.ChurnPredictions)
                .HasForeignKey(x => x.GymId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Member)
                .WithMany(x => x.ChurnPredictions)
                .HasForeignKey(x => x.MemberId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasQueryFilter(x =>
                _currentGymContext.GymId != null &&
                x.GymId == _currentGymContext.GymId);
        });
    }
}
