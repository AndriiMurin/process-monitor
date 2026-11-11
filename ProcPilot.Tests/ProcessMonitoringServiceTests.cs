using ProcPilot.Infrastructure.Services;

namespace ProcPilot.Tests
{
    public class ProcessMonitoringServiceTests
    {
        [Fact]
        public void GetRunningProcesses_ShouldReturnProcesses()
        {
            var service = new ProcessMonitoringService();
            var processes = service.GetRunningProcess();

            Assert.NotEmpty(processes);

            Assert.Contains(processes, p => p.Pid == Environment.ProcessId);
        }
    }
}
