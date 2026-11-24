using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ProcPilot.Domain.Models;
using ProcPilot.Infrastructure.Database;

namespace ProcPilot.Tests;

public class ProcessExecutionDbTests
{
    [Fact]
    public async Task CanWriteAndReadProcessExecution()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        await using (var writeContext = new AppDbContext(options))
        {
            await writeContext.Database.MigrateAsync();

            var execution = new ProcessExecution
            {
                ProcessName = "notepad.exe",
                Pid = 4321,
                StartedAt = DateTime.UtcNow.AddMinutes(-5),
                EndedAt = DateTime.UtcNow
            };

            writeContext.ProcessExecutions.Add(execution);
            await writeContext.SaveChangesAsync();
        }

        await using (var readContext = new AppDbContext(options))
        {
            var saved = await readContext.ProcessExecutions.FirstOrDefaultAsync(p => p.Pid == 4321);

            Assert.NotNull(saved);
            Assert.Equal("notepad.exe", saved.ProcessName);
            Assert.NotNull(saved.EndedAt);
        }
    }
}