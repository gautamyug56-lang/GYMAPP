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

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Gym>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();

            entity.HasMany(x => x.Members)
                .WithOne(x => x.Gym)
                .HasForeignKey(x => x.GymId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Member>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.LastName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(320).IsRequired();
            entity.HasIndex(x => x.GymId);

            entity.HasQueryFilter(
                x => _currentGymContext.GymId != null &&
                     x.GymId == _currentGymContext.GymId);
        });
    }
}
