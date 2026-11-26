using System.Diagnostics;
using ProcPilot.Domain.Models;
using ProcPilot.Infrastructure.Services;

namespace ProcPilot.Tests;

public class ProcessControlServiceTests
{
    private static Process StartControlledTestProcess()
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = "-NoProfile -NonInteractive -Command \"Start-Sleep -Seconds 30\"",
            UseShellExecute = false,
            CreateNoWindow = true
        };

        return Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start controlled test process.");
    }

    [Fact]
    public void TerminateProcess_KillsControlledTestProcess()
    {
        using var testProcess = StartControlledTestProcess();
        try
        {
            var service = new ProcessControlService();
            var result = service.TerminateProcess(testProcess.Id, "powershell");

            Assert.True(result.IsSuccess);
            Assert.Equal(ProcessTerminationStatus.Success, result.Status);
            Assert.True(testProcess.WaitForExit(3000));
        }
        finally
        {
            if (!testProcess.HasExited)
            {
                testProcess.Kill();
            }
        }
    }

    [Fact]
    public void TerminateProcess_ReturnsNotFound_WhenExpectedNameDoesNotMatch()
    {
        using var testProcess = StartControlledTestProcess();
        try
        {
            var service = new ProcessControlService();
            var result = service.TerminateProcess(testProcess.Id, "definitely_wrong_name");

            Assert.False(result.IsSuccess);
            Assert.Equal(ProcessTerminationStatus.NotFound, result.Status);
            Assert.False(testProcess.HasExited);
        }
        finally
        {
            if (!testProcess.HasExited)
            {
                testProcess.Kill();
            }
        }
    }

    [Fact]
    public void TerminateProcess_ReturnsNotFound_ForExitedOrMissingProcess()
    {
        using var testProcess = StartControlledTestProcess();
        var pid = testProcess.Id;
        testProcess.Kill();
        testProcess.WaitForExit(3000);

        var service = new ProcessControlService();
        var result = service.TerminateProcess(pid);

        Assert.False(result.IsSuccess);
        Assert.Equal(ProcessTerminationStatus.NotFound, result.Status);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    public void TerminateProcess_ProtectsCriticalSystemPids(int criticalPid)
    {
        var service = new ProcessControlService();
        var result = service.TerminateProcess(criticalPid);

        Assert.False(result.IsSuccess);
        Assert.Equal(ProcessTerminationStatus.ProtectedSystemProcess, result.Status);
    }

    [Fact]
    public void TerminateProcess_ProtectsCurrentProcess()
    {
        var service = new ProcessControlService();
        var result = service.TerminateProcess(Environment.ProcessId);

        Assert.False(result.IsSuccess);
        Assert.Equal(ProcessTerminationStatus.ProtectedSystemProcess, result.Status);
    }

    [Theory]
    [InlineData("csrss")]
    [InlineData("csrss.exe")]
    [InlineData("LSASS.EXE")]
    [InlineData("svchost")]
    public void IsProtectedProcessName_DetectsCriticalProcesses(string processName)
    {
        Assert.True(ProcessControlService.IsProtectedProcessName(processName));
    }
}
