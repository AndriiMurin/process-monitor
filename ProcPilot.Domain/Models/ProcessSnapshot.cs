namespace ProcPilot.Domain.Models
{
    public class ProcessSnapshot
    {
        public int Pid { get; set; }

        public string Name { get; set; } = string.Empty;

        public double? CpuPercent { get; set; }

        public double? RamMb { get; set; }

        public TimeSpan? Uptime { get; set; }
    }
}
