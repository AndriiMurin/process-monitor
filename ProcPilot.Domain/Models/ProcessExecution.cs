namespace ProcPilot.Domain.Models;

public class ProcessExecution
{
    public int Id { get; set; }
    public string ProcessName { get; set; } = string.Empty;
    public int Pid { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
}