using Microsoft.EntityFrameworkCore;
using ProcPilot.Application.Interfaces;
using ProcPilot.Domain.Models;
using ProcPilot.Infrastructure.Database;

namespace ProcPilot.Infrastructure.Services;

public class BlockedProcessService : IBlockedProcessService
{
    private const int MaxProcessNameLength = 255;
    private readonly AppDbContext _dbContext;

    public BlockedProcessService(AppDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    public async Task<BlockedProcess> AddAsync(
        string processName,
        bool isEnabled = true,
        CancellationToken cancellationToken = default)
    {
        var normalizedName = ValidateAndNormalizeProcessName(processName);

        var exists = await _dbContext.BlockedProcesses
            .AnyAsync(p => p.ProcessName == normalizedName, cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException($"Process '{normalizedName}' is already in the blocked list.");
        }

        var entry = new BlockedProcess
        {
            ProcessName = normalizedName,
            IsEnabled = isEnabled,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.BlockedProcesses.Add(entry);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return entry;
    }

    public async Task<IReadOnlyList<BlockedProcess>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.BlockedProcesses
            .AsNoTracking()
            .OrderBy(p => p.ProcessName)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> SetEnabledAsync(int id, bool isEnabled, CancellationToken cancellationToken = default)
    {
        var entry = await _dbContext.BlockedProcesses
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (entry is null)
        {
            return false;
        }

        entry.IsEnabled = isEnabled;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> RemoveAsync(int id, CancellationToken cancellationToken = default)
    {
        var entry = await _dbContext.BlockedProcesses
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (entry is null)
        {
            return false;
        }

        _dbContext.BlockedProcesses.Remove(entry);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static string ValidateAndNormalizeProcessName(string processName)
    {
        if (string.IsNullOrWhiteSpace(processName))
        {
            throw new ArgumentException("Process name cannot be empty.", nameof(processName));
        }

        var trimmed = processName.Trim();
        if (trimmed.Length > MaxProcessNameLength)
        {
            throw new ArgumentException($"Process name cannot exceed {MaxProcessNameLength} characters.", nameof(processName));
        }

        if (trimmed.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
            trimmed.Contains('/') ||
            trimmed.Contains('\\'))
        {
            throw new ArgumentException("Process name contains invalid characters.", nameof(processName));
        }

        return trimmed.ToLowerInvariant();
    }
}
