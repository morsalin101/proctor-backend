using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PROCTOR.Infrastructure.Data;

/// <summary>
/// Keeps EF design-time commands independent from API startup, database migration, and seeding.
/// </summary>
public sealed class ProctorDbContextFactory : IDesignTimeDbContextFactory<ProctorDbContext>
{
    public ProctorDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ProctorDbContext>()
            .UseNpgsql("Host=localhost;Database=proctor_design;Username=postgres;Password=postgres")
            .Options;
        return new ProctorDbContext(options);
    }
}
