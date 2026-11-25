using System.ComponentModel;
using System.Diagnostics;
using ProcPilot.Application.Interfaces;
using ProcPilot.Domain.Models;

namespace ProcPilot.Infrastructure.Services
{
    public class ProcessMonitoringService : IProcessMonitoringService
    {
        private readonly Dictionary<int, CpuMeasurement> _previousMeasurements = new();

        public IReadOnlyList<ProcessSnapshot> GetProcessList()
        {
            return Process.GetProcesses()
                .Select(process =>
                {
                    using (process)
                    {
                        try
                        {
                            return new ProcessSnapshot { Pid = process.Id, Name = process.ProcessName };
                        }
                        catch (Win32Exception)
                        {
                            return null;
                        }
                        catch (InvalidOperationException)
                        {
                            return null;
                        }
                    }
                })
                .Where(snapshot => snapshot is not null)
                .Cast<ProcessSnapshot>()
                .ToList();
        }

        public IReadOnlyList<ProcessSnapshot> GetRunningProcess()
        {
            var snapshots = new List<ProcessSnapshot>();
            var currentMeasurements = new Dictionary<int, CpuMeasurement>();

            foreach (var process in Process.GetProcesses())
            {
                using (process)
                {
                    try
                    {
                        var snapshot = new ProcessSnapshot
                        {
                            Pid = process.Id,
                            Name = process.ProcessName
                        };

                        var workingSetBytes = TryRead(() => process.WorkingSet64);
                        if (workingSetBytes >= 0)
                        {
                            snapshot.RamMb = workingSetBytes / (1024.0 * 1024.0);
                        }

                        var startTime = TryRead(() => process.StartTime)?.ToUniversalTime();
                        var totalProcessorTime = TryRead(() => process.TotalProcessorTime);
                        var sampledAt = DateTime.UtcNow;
                        if (startTime.HasValue)
                        {
                            var uptime = sampledAt - startTime.Value;
                            if (uptime >= TimeSpan.Zero)
                            {
                                snapshot.Uptime = uptime;
                            }
                        }

                        if (totalProcessorTime.HasValue)
                        {
                            if (_previousMeasurements.TryGetValue(snapshot.Pid, out var previous) &&
                                (previous.StartTime is null || startTime is null || previous.StartTime == startTime))
                            {
                                snapshot.CpuPercent = CalculateCpuPercent(
                                    previous.TotalProcessorTime,
                                    totalProcessorTime.Value,
                                    sampledAt - previous.SampledAt,
                                    Environment.ProcessorCount);
                            }

                            currentMeasurements[snapshot.Pid] = new CpuMeasurement(
                                startTime, totalProcessorTime.Value, sampledAt);
                        }

                        snapshots.Add(snapshot);
                    }
                    catch (Win32Exception)
                    {
                        // brak dostępu do informacji o procesie
                    }
                    catch (InvalidOperationException)
                    {
                        // process mógł się skończyć
                    }
                }
            }

            _previousMeasurements.Clear();
            foreach (var measurement in currentMeasurements)
            {
                _previousMeasurements[measurement.Key] = measurement.Value;
            }

            return snapshots;
        }

        public SystemResourceSnapshot GetSystemResources()
        {
            var processes = GetRunningProcess();
            var cpuValues = processes
                .Where(process => process.CpuPercent.HasValue)
                .Select(process => process.CpuPercent!.Value)
                .ToList();
            var memoryUsedMb = processes
                .Where(process => process.RamMb.HasValue)
                .Sum(process => process.RamMb!.Value);

            return new SystemResourceSnapshot
            {
                CpuPercent = cpuValues.Count == 0 ? null : Math.Clamp(cpuValues.Sum(), 0, 100),
                MemoryUsedMb = memoryUsedMb,
                MemoryAvailableMb = GetAvailableMemoryMb(),
                ProcessCount = processes.Count
            };
        }

        public void TerminateProcess(int processId)
        {
            if (processId <= 0 || processId == Environment.ProcessId)
            {
                throw new InvalidOperationException("The selected process cannot be terminated.");
            }

            using var process = Process.GetProcessById(processId);
            process.Kill(entireProcessTree: true);
            process.WaitForExit(2000);
        }

        public void StartProcess(string fileNameOrPath)
        {
            if (string.IsNullOrWhiteSpace(fileNameOrPath))
            {
                throw new ArgumentException("Enter an executable, command, or file path.", nameof(fileNameOrPath));
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = fileNameOrPath.Trim(),
                UseShellExecute = true
            });
        }

        internal static double? CalculateCpuPercent(
            TimeSpan previousProcessorTime,
            TimeSpan currentProcessorTime,
            TimeSpan elapsedTime,
            int processorCount)
        {
            if (processorCount <= 0 || elapsedTime <= TimeSpan.Zero ||
                currentProcessorTime < previousProcessorTime)
            {
                return null;
            }

            var processorMilliseconds =
                (double)currentProcessorTime.Ticks / TimeSpan.TicksPerMillisecond -
                (double)previousProcessorTime.Ticks / TimeSpan.TicksPerMillisecond;
            var percent = processorMilliseconds / elapsedTime.TotalMilliseconds / processorCount * 100;
            return double.IsFinite(percent) ? Math.Clamp(percent, 0, 100) : null;
        }

        private static T? TryRead<T>(Func<T> read) where T : struct
        {
            try
            {
                return read();
            }
            catch (Win32Exception)
            {
                return null;
            }
            catch (InvalidOperationException)
            {
                return null;
            }
            catch (UnauthorizedAccessException)
            {
                return null;
            }
        }

        private static double GetAvailableMemoryMb()
        {
            var status = new MemoryStatusEx
            {
                Length = (uint)System.Runtime.InteropServices.Marshal.SizeOf<MemoryStatusEx>()
            };

            return GlobalMemoryStatusEx(ref status)
                ? status.AvailablePhysicalMemory / (1024.0 * 1024.0)
                : 0;
        }

        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx status);

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct MemoryStatusEx
        {
            public uint Length;
            public uint MemoryLoad;
            public ulong TotalPhysicalMemory;
            public ulong AvailablePhysicalMemory;
            public ulong TotalPageFile;
            public ulong AvailablePageFile;
            public ulong TotalVirtualMemory;
            public ulong AvailableVirtualMemory;
            public ulong AvailableExtendedVirtualMemory;
        }

        private readonly record struct CpuMeasurement(
            DateTime? StartTime, TimeSpan TotalProcessorTime, DateTime SampledAt);
    }
}
