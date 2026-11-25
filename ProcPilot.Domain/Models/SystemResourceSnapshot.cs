namespace ProcPilot.Domain.Models;

public sealed class SystemResourceSnapshot
{
    public double? CpuPercent { get; init; }

    public double MemoryUsedMb { get; init; }

    public double MemoryAvailableMb { get; init; }

    public double MemoryTotalMb => MemoryUsedMb + MemoryAvailableMb;

    public int ProcessCount { get; init; }
}
