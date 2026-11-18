using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Input;
using ProcPilot.Application.Interfaces;
using ProcPilot.Desktop.Infrastructure;

namespace ProcPilot.Desktop.ViewModels
{
    public sealed class MainWindowViewModel : ViewModelBase
    {
        private string _currentPage = "Dashboard";
        private ThemeOption _selectedTheme;
        private string _lastUpdated = "Not refreshed yet";
        private string _errorMessage = string.Empty;
        private bool _isLoading;

        public MainWindowViewModel(IProcessMonitoringService processMonitoringService)
        {
            ArgumentNullException.ThrowIfNull(processMonitoringService);

            _processMonitoringService = processMonitoringService;
            Themes = new ObservableCollection<ThemeOption>
            {
                ThemeOption.Create("Indigo Night", "#5B6CFF", "#172033", "#202B43"),
                ThemeOption.Create("Ocean Breeze", "#0EA5E9", "#102A43", "#164E63"),
                ThemeOption.Create("Emerald", "#10B981", "#102A26", "#164E3B"),
                ThemeOption.Create("Royal Purple", "#8B5CF6", "#211A35", "#392561"),
                ThemeOption.Create("Sunset", "#F97316", "#30201A", "#57301F")
            };

            _selectedTheme = Themes[0];
            Processes = new ObservableCollection<ProcessRowViewModel>();

            NavigateCommand = new RelayCommand(parameter => Navigate(parameter as string));
            RefreshCommand = new RelayCommand(_ => Refresh());
            SaveSettingsCommand = new RelayCommand(_ => SaveSettings());

            Refresh();
        }

        private readonly IProcessMonitoringService _processMonitoringService;

        public ObservableCollection<ThemeOption> Themes { get; }

        public ObservableCollection<ProcessRowViewModel> Processes { get; }

        public int ProcessCount => Processes.Count;

        public string ErrorMessage
        {
            get => _errorMessage;
            private set => SetProperty(ref _errorMessage, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            private set => SetProperty(ref _isLoading, value);
        }

        // Mock chart data for the UI prototype. Values use the chart's 620x190 coordinate space.
        public PointCollection CpuChartPoints { get; } = new()
        {
            new Point(0, 145), new Point(45, 132), new Point(90, 138), new Point(135, 98),
            new Point(180, 112), new Point(225, 76), new Point(270, 92), new Point(315, 54),
            new Point(360, 67), new Point(405, 42), new Point(450, 61), new Point(495, 30),
            new Point(540, 48), new Point(585, 25), new Point(620, 35)
        };

        public PointCollection MemoryChartPoints { get; } = new()
        {
            new Point(0, 128), new Point(45, 124), new Point(90, 130), new Point(135, 116),
            new Point(180, 120), new Point(225, 100), new Point(270, 108), new Point(315, 92),
            new Point(360, 98), new Point(405, 74), new Point(450, 80), new Point(495, 65),
            new Point(540, 70), new Point(585, 52), new Point(620, 58)
        };

        public ICommand NavigateCommand { get; }

        public ICommand RefreshCommand { get; }

        public ICommand SaveSettingsCommand { get; }

        public string CurrentPage
        {
            get => _currentPage;
            private set
            {
                if (!SetProperty(ref _currentPage, value))
                {
                    return;
                }

                OnPropertyChanged(nameof(IsDashboard));
                OnPropertyChanged(nameof(IsProcesses));
                OnPropertyChanged(nameof(IsPerformance));
                OnPropertyChanged(nameof(IsSettings));
                OnPropertyChanged(nameof(PageTitle));
                OnPropertyChanged(nameof(PageSubtitle));
            }
        }

        public bool IsDashboard => CurrentPage == "Dashboard";

        public bool IsProcesses => CurrentPage == "Processes";

        public bool IsPerformance => CurrentPage == "Performance";

        public bool IsSettings => CurrentPage == "Settings";

        public string PageTitle => CurrentPage;

        public string PageSubtitle => CurrentPage switch
        {
            "Processes" => "Browse the monitored process list",
            "Performance" => "Review static resource usage indicators",
            "Settings" => "Customize the appearance of your dashboard",
            _ => "Monitor your system resources in real time"
        };

        public string LastUpdated
        {
            get => _lastUpdated;
            private set => SetProperty(ref _lastUpdated, value);
        }

        public ThemeOption SelectedTheme
        {
            get => _selectedTheme;
            set => SetProperty(ref _selectedTheme, value);
        }

        public bool IsAutoRefreshEnabled { get; set; } = true;

        public bool ShowSystemProcesses { get; set; } = true;

        private void Navigate(string? page)
        {
            if (page is "Dashboard" or "Processes" or "Performance" or "Settings")
            {
                CurrentPage = page;
            }
        }

        private void Refresh()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                var snapshots = _processMonitoringService
                    .GetRunningProcess()
                    .OrderBy(snapshot => snapshot.Name, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(snapshot => snapshot.Pid);

                Processes.Clear();
                foreach (var snapshot in snapshots)
                {
                    Processes.Add(new ProcessRowViewModel(snapshot.Name, snapshot.Pid));
                }

                OnPropertyChanged(nameof(ProcessCount));
                LastUpdated = $"Live data refreshed {DateTime.Now:HH:mm:ss}";
            }
            catch (Exception exception)
            {
                ErrorMessage = $"Unable to load process data: {exception.Message}";
                LastUpdated = "Refresh failed";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void SaveSettings()
        {
            LastUpdated = IsAutoRefreshEnabled
                ? "Settings saved locally"
                : "Settings saved locally (auto-refresh off)";
        }
    }

    public sealed class ProcessRowViewModel
    {
        public ProcessRowViewModel(string name, int pid)
        {
            Name = name;
            Pid = pid.ToString();
            Cpu = "N/A";
            Memory = "N/A";
            Uptime = "N/A";
            IndicatorColor = (SolidColorBrush)new BrushConverter().ConvertFrom("#5B6CFF")!;
        }

        public string Name { get; }

        public string Pid { get; }

        public string Cpu { get; }

        public string Memory { get; }

        public string Uptime { get; }

        public Brush IndicatorColor { get; }
    }

    public sealed class ThemeOption
    {
        private ThemeOption(string name, string accent, string sidebar, string sidebarPanel)
        {
            Name = name;
            AccentBrush = ToBrush(accent);
            SidebarBrush = ToBrush(sidebar);
            SidebarPanelBrush = ToBrush(sidebarPanel);
        }

        public string Name { get; }

        public Brush AccentBrush { get; }

        public Brush SidebarBrush { get; }

        public Brush SidebarPanelBrush { get; }

        public Brush WindowBrush { get; } = ToBrush("#F5F7FB");

        public Brush CardBrush { get; } = Brushes.White;

        public Brush HeadingBrush { get; } = ToBrush("#1D2738");

        public Brush TextBrush { get; } = ToBrush("#263247");

        public Brush MutedBrush { get; } = ToBrush("#7B879C");

        public static ThemeOption Create(string name, string accent, string sidebar, string sidebarPanel)
        {
            return new ThemeOption(name, accent, sidebar, sidebarPanel);
        }

        private static SolidColorBrush ToBrush(string color)
        {
            return (SolidColorBrush)new BrushConverter().ConvertFrom(color)!;
        }
    }
}
