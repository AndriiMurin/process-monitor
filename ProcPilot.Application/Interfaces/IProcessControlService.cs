using ProcPilot.Domain.Models;

namespace ProcPilot.Application.Interfaces;

public interface IProcessControlService
{
    ProcessTerminationResult TerminateProcess(int pid, string? expectedProcessName = null);
}
