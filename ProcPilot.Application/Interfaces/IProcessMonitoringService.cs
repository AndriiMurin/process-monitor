using ProcPilot.Domain.Models;

namespace ProcPilot.Application.Interfaces
{
    public interface IProcessMonitoringService
    {
        IReadOnlyList<ProcessSnapshot> GetRunningProcess();
    }
}
