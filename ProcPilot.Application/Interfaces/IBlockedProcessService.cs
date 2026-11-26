using ProcPilot.Domain.Models;

namespace ProcPilot.Application.Interfaces;

public interface IBlockedProcessService
{
    Task<BlockedProcess> AddAsync(string processName, bool isEnabled = true, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BlockedProcess>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<bool> SetEnabledAsync(int id, bool isEnabled, CancellationToken cancellationToken = default);

    Task<bool> RemoveAsync(int id, CancellationToken cancellationToken = default);
}
