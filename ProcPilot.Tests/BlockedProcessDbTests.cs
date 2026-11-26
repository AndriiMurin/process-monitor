using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ProcPilot.Domain.Models;
using ProcPilot.Infrastructure.Database;

namespace ProcPilot.Tests;

public class BlockedProcessDbTests
{
    [Fact]
    public async Task MigrationCreatesBlockedProcessesTableAndCanWriteRead()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        await using (var writeContext = new AppDbContext(options))
        {
            await writeContext.Database.MigrateAsync();

            writeContext.BlockedProcesses.Add(new BlockedProcess
            {
                ProcessName = "notepad.exe",
                IsEnabled = true,
                CreatedAt = new DateTime(2026, 11, 26, 12, 0, 0, DateTimeKind.Utc)
            });
            await writeContext.SaveChangesAsync();
        }

        await using (var readContext = new AppDbContext(options))
        {
            var saved = await readContext.BlockedProcesses.FirstOrDefaultAsync(b => b.ProcessName == "notepad.exe");
            Assert.NotNull(saved);
            Assert.True(saved.Id > 0);
            Assert.True(saved.IsEnabled);
        }
    }
}
