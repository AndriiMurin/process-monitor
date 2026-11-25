using ProcPilot.Infrastructure.Services;

namespace ProcPilot.Tests
{
    public class ProcessMonitoringServiceTests
    {
        [Fact]
        public void GetRunningProcesses_ShouldReturnProcessesAndMeasurements()
        {
            var service = new ProcessMonitoringService();
            var processes = service.GetRunningProcess();

            Assert.NotEmpty(processes);
            Assert.All(processes, process =>
            {
                Assert.True(process.Pid >= 0);
                Assert.False(string.IsNullOrWhiteSpace(process.Name));
                Assert.True(process.RamMb is null or >= 0);
                Assert.True(process.Uptime is null || process.Uptime >= TimeSpan.Zero);
                Assert.Null(process.CpuPercent);
            });
            Assert.Contains(processes, process => process.Pid == Environment.ProcessId);

            var nextProcesses = service.GetRunningProcess();
            var currentProcess = Assert.Single(nextProcesses, process => process.Pid == Environment.ProcessId);
            Assert.NotNull(currentProcess.RamMb);
            Assert.NotNull(currentProcess.Uptime);
            Assert.InRange(currentProcess.CpuPercent ?? -1, 0, 100);
        }

        [Fact]
        public void CalculateCpuPercent_UsesElapsedTimeAndProcessorCount()
        {
            var percent = ProcessMonitoringService.CalculateCpuPercent(
                TimeSpan.FromMilliseconds(100),
                TimeSpan.FromMilliseconds(300),
                TimeSpan.FromSeconds(1),
                4);

            Assert.Equal(5, percent);
        }

        [Theory]
        [InlineData(200, 100, 1000, 4)]
        [InlineData(100, 200, 0, 4)]
        [InlineData(100, 200, -1000, 4)]
        [InlineData(100, 200, 1000, 0)]
        public void CalculateCpuPercent_ReturnsNullForInvalidMeasurements(
            int previousMs, int currentMs, int elapsedMs, int processors)
        {
            Assert.Null(ProcessMonitoringService.CalculateCpuPercent(
                TimeSpan.FromMilliseconds(previousMs),
                TimeSpan.FromMilliseconds(currentMs),
                TimeSpan.FromMilliseconds(elapsedMs),
                processors));
        }

        [Fact]
        public void CalculateCpuPercent_ClampsValuesAboveOneHundred()
        {
            var percent = ProcessMonitoringService.CalculateCpuPercent(
                TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(1), 1);

            Assert.Equal(100, percent);
        }

        [Fact]
        public void GetSystemResources_ReturnsProcessAndMemoryData()
        {
            var service = new ProcessMonitoringService();

            var resources = service.GetSystemResources();

            Assert.True(resources.ProcessCount > 0);
            Assert.True(resources.MemoryUsedMb >= 0);
            Assert.True(resources.MemoryAvailableMb >= 0);
            Assert.True(resources.MemoryTotalMb >= resources.MemoryUsedMb);
        }

        [Fact]
        public void TerminateProcess_RejectsTheCurrentProcess()
        {
            var service = new ProcessMonitoringService();

            Assert.Throws<InvalidOperationException>(() => service.TerminateProcess(Environment.ProcessId));
        }

        [Fact]
        public void StartProcess_RejectsAnEmptyPath()
        {
            var service = new ProcessMonitoringService();

            Assert.Throws<ArgumentException>(() => service.StartProcess(" "));
        }
    }
}
