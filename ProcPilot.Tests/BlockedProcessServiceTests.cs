using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProcPilot.Application.Interfaces;
using ProcPilot.Infrastructure;
using ProcPilot.Infrastructure.Database;
using ProcPilot.Infrastructure.Services;

namespace ProcPilot.Tests;

public class BlockedProcessServiceTests
{
    [Fact]
    public async Task AddAndGetAll_PersistsAndReturnsOrderedEntries()
    {
        await using var setup = await TestDbSetup.CreateAsync();
        var service = new BlockedProcessService(setup.Context);

        var addedB = await service.AddAsync("  Zoom.exe ");
        var addedA = await service.AddAsync("chrome.exe", isEnabled: false);

        Assert.True(addedB.Id > 0);
        Assert.Equal("zoom.exe", addedB.ProcessName);
        Assert.True(addedB.IsEnabled);

        var all = await service.GetAllAsync();
        Assert.Equal(2, all.Count);
        Assert.Equal("chrome.exe", all[0].ProcessName);
        Assert.False(all[0].IsEnabled);
        Assert.Equal("zoom.exe", all[1].ProcessName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("folder\\app.exe")]
    [InlineData("folder/app.exe")]
    [InlineData("bad*name.exe")]
    public async Task AddAsync_RejectsInvalidProcessNames(string invalidName)
    {
        await using var setup = await TestDbSetup.CreateAsync();
        var service = new BlockedProcessService(setup.Context);

        await Assert.ThrowsAsync<ArgumentException>(() => service.AddAsync(invalidName));
    }

    [Fact]
    public async Task AddAsync_RejectsDuplicateProcessNames_CaseInsensitive()
    {
        await using var setup = await TestDbSetup.CreateAsync();
        var service = new BlockedProcessService(setup.Context);

        await service.AddAsync("Notepad.exe");

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.AddAsync("notepad.exe"));
    }

    [Fact]
    public async Task SetEnabledAsync_UpdatesState_AndReturnsFalseWhenMissing()
    {
        await using var setup = await TestDbSetup.CreateAsync();
        var service = new BlockedProcessService(setup.Context);

        var added = await service.AddAsync("notepad.exe", isEnabled: true);

        Assert.True(await service.SetEnabledAsync(added.Id, false));
        Assert.False(await service.SetEnabledAsync(99999, true));

        var all = await service.GetAllAsync();
        Assert.False(Assert.Single(all).IsEnabled);
    }

    [Fact]
    public async Task RemoveAsync_DeletesEntry_AndReturnsFalseWhenMissing()
    {
        await using var setup = await TestDbSetup.CreateAsync();
        var service = new BlockedProcessService(setup.Context);

        var added = await service.AddAsync("notepad.exe");

        Assert.True(await service.RemoveAsync(added.Id));
        Assert.False(await service.RemoveAsync(added.Id));
        Assert.Empty(await service.GetAllAsync());
    }

    [Fact]
    public void AddInfrastructure_RegistersProcessControlAndBlockedProcessServices()
    {
        var services = new ServiceCollection();
        services.AddInfrastructure("DataSource=:memory:");

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        Assert.IsType<ProcessControlService>(scope.ServiceProvider.GetService<IProcessControlService>());
        Assert.IsType<BlockedProcessService>(scope.ServiceProvider.GetService<IBlockedProcessService>());
    }

    private sealed class TestDbSetup : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        public AppDbContext Context { get; }

        private TestDbSetup(SqliteConnection connection, AppDbContext context)
        {
            _connection = connection;
            Context = context;
        }

        public static async Task<TestDbSetup> CreateAsync()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection)
                .Options;

            var context = new AppDbContext(options);
            await context.Database.MigrateAsync();

            return new TestDbSetup(connection, context);
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
