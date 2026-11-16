using Microsoft.EntityFrameworkCore;
using ProcPilot.Domain.Models;

namespace ProcPilot.Infrastructure.Database;

public class AppDbContext : DbContext
{
    public DbSet<ProcessExecution> ProcessExecutions => Set<ProcessExecution>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ProcessExecution>(entity =>
        {
            entity.ToTable("ProcessExecutions");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.ProcessName)
                  .IsRequired()
                  .HasMaxLength(255);

            entity.Property(e => e.Pid)
                  .IsRequired();

            entity.Property(e => e.StartedAt)
                  .IsRequired();
        });
    }
}