using System.Collections.ObjectModel;
using System.Windows.Media;
using System.Windows.Input;
using ProcPilot.Desktop.Infrastructure;

namespace ProcPilot.Desktop.ViewModels
{
    public sealed class MainWindowViewModel : ViewModelBase
    {
        private string _currentPage = "Dashboard";
        private ThemeOption _selectedTheme;
        private string _lastUpdated = "Last updated 10:42:18";

        public MainWindowViewModel()
        {
            Themes = new ObservableCollection<ThemeOption>
            {
                ThemeOption.Create("Indigo Night", "#5B6CFF", "#172033", "#202B43"),
                ThemeOption.Create("Ocean Breeze", "#0EA5E9", "#102A43", "#164E63"),
                ThemeOption.Create("Emerald", "#10B981", "#102A26", "#164E3B"),
                ThemeOption.Create("Royal Purple", "#8B5CF6", "#211A35", "#392561"),
                ThemeOption.Create("Sunset", "#F97316", "#30201A", "#57301F")
            };

            _selectedTheme = Themes[0];

            Processes = new ObservableCollection<ProcessRowViewModel>
            {
                new("ProcPilot.Desktop", "4820", "12.4%", "128 MB", "02:14:36", "#5B6CFF"),
                new("explorer", "3912", "4.8%", "86 MB", "05:42:11", "#42A879"),
                new("ServiceHost", "1084", "2.1%", "64 MB", "01:08:52", "#F1B95A")
            };

            NavigateCommand = new RelayCommand(parameter => Navigate(parameter as string));
            RefreshCommand = new RelayCommand(_ => Refresh());
            SaveSettingsCommand = new RelayCommand(_ => SaveSettings());
        }

        public ObservableCollection<ThemeOption> Themes { get; }

        public ObservableCollection<ProcessRowViewModel> Processes { get; }

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
            LastUpdated = $"Preview refreshed {DateTime.Now:HH:mm:ss}";
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
        public ProcessRowViewModel(string name, string pid, string cpu, string memory, string uptime, string indicatorColor)
        {
            Name = name;
            Pid = pid;
            Cpu = cpu;
            Memory = memory;
            Uptime = uptime;
            IndicatorColor = (SolidColorBrush)new BrushConverter().ConvertFrom(indicatorColor)!;
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
