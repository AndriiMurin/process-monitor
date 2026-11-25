using ProcPilot.Domain.Models;

namespace ProcPilot.Application.Interfaces
{
    public interface IProcessExecutionHistoryService
    {
        Task<ProcessExecution> AddExecutionAsync(ProcessExecution execution, CancellationToken cancellationToken = default);

        Task<bool> UpdateEndedAtAsync(int id, DateTime endedAt, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<ProcessExecution>> GetAllAsync(CancellationToken cancellationToken = default);

        Task<IReadOnlyList<ProcessExecution>> GetByProcessNameAsync(string processName, CancellationToken cancellationToken = default);
    }
}
