using System.ComponentModel;
using System.Diagnostics;
using ProcPilot.Application.Interfaces;
using ProcPilot.Domain.Models;

namespace ProcPilot.Infrastructure.Services
{
    public class ProcessMonitoringService: IProcessMonitoringService
    {
        public IReadOnlyList<ProcessSnapshot> GetRunningProcess()
        {
            var snapshots = new List<ProcessSnapshot>();

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
            return snapshots;
        }
    }
}
