using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Rackroom.Application.Abstractions;

namespace Rackroom.Infrastructure.Data;

public sealed class ApplicationDbContextFactory
    : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        string connectionString = args.FirstOrDefault()
            ?? "Data Source=rackroom.db";

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connectionString)
            .Options;

        return new ApplicationDbContext(
            options,
            new DesignTimeCurrentGymContext());
    }

    private sealed class DesignTimeCurrentGymContext : ICurrentGymContext
    {
        public Guid? GymId => null;
        public Guid? MemberId => null;
    }
}
