using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using ProcPilot.Application.Interfaces;
using ProcPilot.Domain.Models;
using ProcPilot.Infrastructure.Database;

namespace ProcPilot.Infrastructure.Services
{
    public class ProcessExecutionHistoryService : IProcessExecutionHistoryService
    {
        private readonly AppDbContext _dbContext;

        public ProcessExecutionHistoryService(AppDbContext dbContext)
        {
            ArgumentNullException.ThrowIfNull(dbContext);
            _dbContext = dbContext;
        }

        public async Task<ProcessExecution> AddExecutionAsync(
            ProcessExecution execution,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(execution);
            ArgumentException.ThrowIfNullOrWhiteSpace(execution.ProcessName);

            return await ExecuteDbOperationAsync(async () =>
            {
                _dbContext.ProcessExecutions.Add(execution);
                await _dbContext.SaveChangesAsync(cancellationToken);
                return execution;
            }, "Failed to save process execution history entry.", cancellationToken);
        }

        public async Task<bool> UpdateEndedAtAsync(
            int id,
            DateTime endedAt,
            CancellationToken cancellationToken = default)
        {
            return await ExecuteDbOperationAsync(async () =>
            {
                var execution = await _dbContext.ProcessExecutions
                    .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

                if (execution is null)
                {
                    return false;
                }

                execution.EndedAt = endedAt;
                await _dbContext.SaveChangesAsync(cancellationToken);
                return true;
            }, $"Failed to update end time for process execution {id}.", cancellationToken);
        }

        public async Task<IReadOnlyList<ProcessExecution>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await ExecuteDbOperationAsync<IReadOnlyList<ProcessExecution>>(async () =>
                await _dbContext.ProcessExecutions
                    .AsNoTracking()
                    .OrderByDescending(e => e.StartedAt)
                    .ThenByDescending(e => e.Id)
                    .ToListAsync(cancellationToken),
                "Failed to retrieve process execution history.",
                cancellationToken);
        }

        public async Task<IReadOnlyList<ProcessExecution>> GetByProcessNameAsync(
            string processName,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(processName);

            return await ExecuteDbOperationAsync<IReadOnlyList<ProcessExecution>>(async () =>
                await _dbContext.ProcessExecutions
                    .AsNoTracking()
                    .Where(e => e.ProcessName == processName)
                    .OrderByDescending(e => e.StartedAt)
                    .ThenByDescending(e => e.Id)
                    .ToListAsync(cancellationToken),
                $"Failed to retrieve process execution history for '{processName}'.",
                cancellationToken);
        }

        private static async Task<T> ExecuteDbOperationAsync<T>(
            Func<Task<T>> operation,
            string errorMessage,
            CancellationToken cancellationToken)
        {
            try
            {
                return await operation();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex) when (ex is DbUpdateException or DbException)
            {
                throw new InvalidOperationException(errorMessage, ex);
            }
        }
    }
}
