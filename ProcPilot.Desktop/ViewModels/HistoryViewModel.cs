using System.Collections.ObjectModel;
using ProcPilot.Desktop.Infrastructure;

namespace ProcPilot.Desktop.ViewModels
{
    public sealed class HistoryViewModel : ViewModelBase
    {
        public HistoryViewModel()
        {
            Entries = new ObservableCollection<ProcessHistoryEntryViewModel>
            {
                new("ProcPilot.Desktop", 14820, DateTime.Today.AddHours(8).AddMinutes(12), DateTime.Today.AddHours(8).AddMinutes(47)),
                new("VisualStudio", 9324, DateTime.Today.AddHours(7).AddMinutes(35), DateTime.Today.AddHours(9).AddMinutes(18)),
                new("msedge", 22116, DateTime.Today.AddHours(6).AddMinutes(52), DateTime.Today.AddHours(7).AddMinutes(28)),
                new("explorer", 6040, DateTime.Today.AddHours(6).AddMinutes(10), DateTime.Today.AddHours(9).AddMinutes(2))
            };

            Entries.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasHistory));
        }

        public ObservableCollection<ProcessHistoryEntryViewModel> Entries { get; }

        public bool HasHistory => Entries.Count > 0;
    }

    public sealed class ProcessHistoryEntryViewModel
    {
        public ProcessHistoryEntryViewModel(string processName, int pid, DateTime startedAt, DateTime endedAt)
        {
            ProcessName = processName;
            Pid = pid.ToString();
            StartedAt = startedAt.ToString("MMM d, yyyy HH:mm:ss");
            EndedAt = endedAt.ToString("MMM d, yyyy HH:mm:ss");
            Duration = (endedAt - startedAt).ToString("hh\\:mm\\:ss");
        }

        public string ProcessName { get; }

        public string Pid { get; }

        public string StartedAt { get; }

        public string EndedAt { get; }

        public string Duration { get; }
    }
}
