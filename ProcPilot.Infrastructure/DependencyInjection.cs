using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProcPilot.Application.Interfaces;
using ProcPilot.Infrastructure.Database;
using ProcPilot.Infrastructure.Services;

namespace ProcPilot.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string? customConnectionString = null)
    {
        var connectionString = customConnectionString ?? GetDefaultSqliteConnectionString();

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(connectionString));
        services.AddSingleton<IProcessMonitoringService, ProcessMonitoringService>();
        services.AddSingleton<IProcessControlService, ProcessControlService>();
        services.AddScoped<IBlockedProcessService, BlockedProcessService>();

        services.AddScoped<IProcessExecutionHistoryService, ProcessExecutionHistoryService>();

        return services;
    }

    public static string GetDefaultSqliteConnectionString()
    {
        var appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dbDirectory = Path.Combine(appDataFolder, "ProcPilot");
        Directory.CreateDirectory(dbDirectory);

        var dbPath = Path.Combine(dbDirectory, "procpilot.db");
        return $"Data Source={dbPath}";
    }
}