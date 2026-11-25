using ProcPilot.Domain.Models;

namespace ProcPilot.Application.Interfaces
{
    public interface IProcessMonitoringService
    {
        IReadOnlyList<ProcessSnapshot> GetProcessList();

        IReadOnlyList<ProcessSnapshot> GetRunningProcess();

        SystemResourceSnapshot GetSystemResources();

        void TerminateProcess(int processId);

        void StartProcess(string fileNameOrPath);
    }
}
