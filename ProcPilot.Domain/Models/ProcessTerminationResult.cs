namespace ProcPilot.Domain.Models;

public enum ProcessTerminationStatus
{
    Success,
    NotFound,
    AlreadyExited,
    ProtectedSystemProcess,
    AccessDenied,
    Failed
}

public sealed record ProcessTerminationResult(
    bool IsSuccess,
    ProcessTerminationStatus Status,
    string Message);
