using System.ComponentModel;
using System.Diagnostics;
using ProcPilot.Application.Interfaces;
using ProcPilot.Domain.Models;

namespace ProcPilot.Infrastructure.Services
{
    public class ProcessMonitoringService : IProcessMonitoringService
    {
        private readonly Dictionary<int, CpuMeasurement> _previousMeasurements = new();

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

                        if (startTime.HasValue && totalProcessorTime.HasValue)
                        {
                            if (_previousMeasurements.TryGetValue(snapshot.Pid, out var previous) &&
                                previous.StartTime == startTime.Value)
                            {
                                snapshot.CpuPercent = CalculateCpuPercent(
                                    previous.TotalProcessorTime,
                                    totalProcessorTime.Value,
                                    sampledAt - previous.SampledAt,
                                    Environment.ProcessorCount);
                            }

                            currentMeasurements[snapshot.Pid] = new CpuMeasurement(
                                startTime.Value, totalProcessorTime.Value, sampledAt);
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
        }

        private readonly record struct CpuMeasurement(
            DateTime StartTime, TimeSpan TotalProcessorTime, DateTime SampledAt);
    }
}
