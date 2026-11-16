using Microsoft.EntityFrameworkCore;

namespace ProcPilot.Infrastructure.Database;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // Konfiguracja encji zostanie dodana w kolejnych etapach
    }
}