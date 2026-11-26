using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProcPilot.Application.Interfaces;
using ProcPilot.Domain.Models;
using ProcPilot.Infrastructure;
using ProcPilot.Infrastructure.Database;
using ProcPilot.Infrastructure.Services;

namespace ProcPilot.Tests;

public class ProcessExecutionHistoryServiceTests
{
    private static async Task<(SqliteConnection Connection, AppDbContext Context)> CreateMigratedContextAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new AppDbContext(options);
        await context.Database.MigrateAsync();

        return (connection, context);
    }

    [Fact]
    public async Task AddExecutionAsync_PersistsEntryAndAssignsId()
    {
        await using var setup = await CreateSetupAsync();
        var service = new ProcessExecutionHistoryService(setup.Context);
        var startedAt = new DateTime(2026, 11, 25, 10, 0, 0, DateTimeKind.Utc);

        var saved = await service.AddExecutionAsync(new ProcessExecution
        {
            ProcessName = "notepad.exe",
            Pid = 1234,
            StartedAt = startedAt
        });

        Assert.True(saved.Id > 0);
        Assert.Equal("notepad.exe", saved.ProcessName);
        Assert.Equal(1234, saved.Pid);
        Assert.Equal(startedAt, saved.StartedAt);
        Assert.Null(saved.EndedAt);
    }

    [Fact]
    public async Task UpdateEndedAtAsync_UpdatesExistingEntry_AndReturnsFalseWhenMissing()
    {
        await using var setup = await CreateSetupAsync();
        var service = new ProcessExecutionHistoryService(setup.Context);
        var startedAt = new DateTime(2026, 11, 25, 10, 0, 0, DateTimeKind.Utc);
        var endedAt = startedAt.AddMinutes(15);

        var saved = await service.AddExecutionAsync(new ProcessExecution
        {
            ProcessName = "calc.exe",
            Pid = 2222,
            StartedAt = startedAt
        });

        var updated = await service.UpdateEndedAtAsync(saved.Id, endedAt);
        var missing = await service.UpdateEndedAtAsync(99999, endedAt);

        Assert.True(updated);
        Assert.False(missing);

        var all = await service.GetAllAsync();
        var item = Assert.Single(all);
        Assert.Equal(endedAt, item.EndedAt);
    }

    [Fact]
    public async Task GetAllAsync_And_GetByProcessNameAsync_ReturnFilteredOrderedHistory()
    {
        await using var setup = await CreateSetupAsync();
        var service = new ProcessExecutionHistoryService(setup.Context);
        var t1 = new DateTime(2026, 11, 25, 8, 0, 0, DateTimeKind.Utc);
        var t2 = t1.AddHours(1);
        var t3 = t1.AddHours(2);

        await service.AddExecutionAsync(new ProcessExecution { ProcessName = "chrome.exe", Pid = 101, StartedAt = t1 });
        await service.AddExecutionAsync(new ProcessExecution { ProcessName = "code.exe", Pid = 102, StartedAt = t2 });
        await service.AddExecutionAsync(new ProcessExecution { ProcessName = "chrome.exe", Pid = 103, StartedAt = t3 });

        var all = await service.GetAllAsync();
        Assert.Equal(3, all.Count);
        Assert.Equal(103, all[0].Pid);
        Assert.Equal(102, all[1].Pid);
        Assert.Equal(101, all[2].Pid);

        var chromeHistory = await service.GetByProcessNameAsync("chrome.exe");
        Assert.Equal(2, chromeHistory.Count);
        Assert.All(chromeHistory, e => Assert.Equal("chrome.exe", e.ProcessName));
        Assert.Equal(103, chromeHistory[0].Pid);
        Assert.Equal(101, chromeHistory[1].Pid);
    }

    [Fact]
    public async Task DatabaseError_IsWrappedInInvalidOperationExceptionWithDbInnerException()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new AppDbContext(options);
        // Intentionally skip MigrateAsync so ProcessExecutions table does not exist.
        var service = new ProcessExecutionHistoryService(context);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetAllAsync());
        Assert.IsAssignableFrom<DbException>(ex.InnerException);
    }

    [Fact]
    public void AddInfrastructure_RegistersHistoryService()
    {
        var services = new ServiceCollection();
        services.AddInfrastructure("DataSource=:memory:");

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var historyService = scope.ServiceProvider.GetService<IProcessExecutionHistoryService>();

        Assert.NotNull(historyService);
        Assert.IsType<ProcessExecutionHistoryService>(historyService);
    }

    private static async Task<TestDbSetup> CreateSetupAsync()
    {
        var (connection, context) = await CreateMigratedContextAsync();
        return new TestDbSetup(connection, context);
    }

    private sealed class TestDbSetup(SqliteConnection connection, AppDbContext context) : IAsyncDisposable
    {
        public AppDbContext Context { get; } = context;

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}
