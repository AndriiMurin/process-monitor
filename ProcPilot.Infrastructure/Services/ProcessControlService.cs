using System.ComponentModel;
using System.Diagnostics;
using ProcPilot.Application.Interfaces;
using ProcPilot.Domain.Models;

namespace ProcPilot.Infrastructure.Services;

public class ProcessControlService : IProcessControlService
{
    private const int ErrorAccessDenied = 5;

    private static readonly HashSet<string> ProtectedProcessNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "idle",
        "system",
        "registry",
        "smss",
        "csrss",
        "wininit",
        "services",
        "lsass",
        "winlogon",
        "svchost",
        "dwm",
        "fontdrvhost",
        "memory compression"
    };

    public ProcessTerminationResult TerminateProcess(int pid, string? expectedProcessName = null)
    {
        if (pid is 0 or 4 || pid == Environment.ProcessId)
        {
            return new ProcessTerminationResult(
                false,
                ProcessTerminationStatus.ProtectedSystemProcess,
                $"Cannot terminate protected process (PID {pid}).");
        }

        if (pid < 0)
        {
            return new ProcessTerminationResult(
                false,
                ProcessTerminationStatus.NotFound,
                $"Process with PID {pid} was not found.");
        }

        Process? process = null;
        try
        {
            try
            {
                process = Process.GetProcessById(pid);
            }
            catch (ArgumentException)
            {
                return new ProcessTerminationResult(
                    false,
                    ProcessTerminationStatus.NotFound,
                    $"Process with PID {pid} is not running.");
            }

            if (process.HasExited)
            {
                return new ProcessTerminationResult(
                    false,
                    ProcessTerminationStatus.AlreadyExited,
                    $"Process (PID {pid}) has already exited.");
            }

            var actualName = process.ProcessName;
            if (!string.IsNullOrWhiteSpace(expectedProcessName))
            {
                var normalizedExpected = NormalizeProcessName(expectedProcessName);
                if (!string.Equals(actualName, normalizedExpected, StringComparison.OrdinalIgnoreCase))
                {
                    return new ProcessTerminationResult(
                        false,
                        ProcessTerminationStatus.NotFound,
                        $"Process PID {pid} ('{actualName}') does not match expected '{normalizedExpected}'.");
                }
            }

            if (IsProtectedProcessName(actualName))
            {
                return new ProcessTerminationResult(
                    false,
                    ProcessTerminationStatus.ProtectedSystemProcess,
                    $"Cannot terminate critical system process '{actualName}' (PID {pid}).");
            }

            process.Kill();
            return new ProcessTerminationResult(
                true,
                ProcessTerminationStatus.Success,
                $"Process '{actualName}' (PID {pid}) was terminated.");
        }
        catch (InvalidOperationException)
        {
            return new ProcessTerminationResult(
                false,
                ProcessTerminationStatus.AlreadyExited,
                $"Process (PID {pid}) has already exited.");
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == ErrorAccessDenied)
        {
            return new ProcessTerminationResult(
                false,
                ProcessTerminationStatus.AccessDenied,
                $"Access denied when terminating process (PID {pid}).");
        }
        catch (Win32Exception ex)
        {
            return new ProcessTerminationResult(
                false,
                ProcessTerminationStatus.Failed,
                $"Failed to terminate process (PID {pid}): {ex.Message}");
        }
        catch (NotSupportedException ex)
        {
            return new ProcessTerminationResult(
                false,
                ProcessTerminationStatus.Failed,
                $"Cannot terminate process (PID {pid}): {ex.Message}");
        }
        finally
        {
            process?.Dispose();
        }
    }

    internal static bool IsProtectedProcessName(string? processName)
    {
        if (string.IsNullOrWhiteSpace(processName))
        {
            return false;
        }

        return ProtectedProcessNames.Contains(NormalizeProcessName(processName));
    }

    private static string NormalizeProcessName(string processName)
    {
        var trimmed = processName.Trim();
        return trimmed.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
            ? trimmed[..^4]
            : trimmed;
    }
}
