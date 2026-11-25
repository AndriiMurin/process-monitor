using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Threading;
using System.Threading;
using System.Threading.Tasks;
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
        private bool _isAutoRefreshEnabled = true;
        private string _processFilterText = string.Empty;
        private string _startProcessPath = string.Empty;
        private ProcessRowViewModel? _selectedProcess;
        private string _cpuUsage = "N/A";
        private string _memoryUsage = "N/A";
        private string _selectedPerformanceMetric = "CPU";
        private int _processCount;
        private readonly DispatcherTimer _refreshTimer;
        private readonly SemaphoreSlim _refreshGate = new(1, 1);
        private readonly SemaphoreSlim _metricsRefreshGate = new(1, 1);
        private readonly Dispatcher _dispatcher = Dispatcher.CurrentDispatcher;
        private readonly Queue<double> _cpuHistory = new();
        private readonly Queue<double> _memoryHistory = new();
        private const int ChartSampleCount = 30;
        private const double ChartWidth = 800;
        private const double ChartHeight = 300;

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
            History = new HistoryViewModel();
            Processes = new ObservableCollection<ProcessRowViewModel>();
            ProcessesView = CollectionViewSource.GetDefaultView(Processes);
            ProcessesView.Filter = FilterProcess;
            ProcessesView.SortDescriptions.Add(new SortDescription(nameof(ProcessRowViewModel.Name), ListSortDirection.Ascending));
            ProcessesView.SortDescriptions.Add(new SortDescription(nameof(ProcessRowViewModel.ProcessId), ListSortDirection.Ascending));

            NavigateCommand = new RelayCommand(parameter => Navigate(parameter as string));
            RefreshCommand = new RelayCommand(_ => _ = RefreshAsync());
            SaveSettingsCommand = new RelayCommand(_ => SaveSettings());
            TerminateProcessCommand = new RelayCommand(_ => _ = TerminateSelectedProcessAsync(), _ => SelectedProcess is not null);
            StartProcessCommand = new RelayCommand(_ => StartProcess());
            SelectPerformanceCommand = new RelayCommand(parameter => SelectPerformance(parameter as string));

            _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _refreshTimer.Tick += RefreshTimer_Tick;

            _ = RefreshAsync();
            _refreshTimer.Start();
        }

        private readonly IProcessMonitoringService _processMonitoringService;

        public ObservableCollection<ThemeOption> Themes { get; }

        public HistoryViewModel History { get; }

        public ObservableCollection<ProcessRowViewModel> Processes { get; }

        public ICollectionView ProcessesView { get; }

        public int ProcessCount => _processCount;

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

        public string CpuUsage
        {
            get => _cpuUsage;
            private set
            {
                if (!SetProperty(ref _cpuUsage, value))
                {
                    return;
                }

                OnPropertyChanged(nameof(SelectedPerformanceValue));
            }
        }

        public string MemoryUsage
        {
            get => _memoryUsage;
            private set => SetProperty(ref _memoryUsage, value);
        }

        public PointCollection CpuChartPoints { get; } = new();

        public PointCollection MemoryChartPoints { get; } = new();

        private PointCollection _cpuAreaPoints = new();

        private PointCollection _memoryAreaPoints = new();

        public PointCollection CpuAreaPoints
        {
            get => _cpuAreaPoints;
            private set => SetProperty(ref _cpuAreaPoints, value);
        }

        public PointCollection MemoryAreaPoints
        {
            get => _memoryAreaPoints;
            private set => SetProperty(ref _memoryAreaPoints, value);
        }

        public ICommand NavigateCommand { get; }

        public ICommand RefreshCommand { get; }

        public ICommand SaveSettingsCommand { get; }

        public ICommand TerminateProcessCommand { get; }

        public ICommand StartProcessCommand { get; }

        public ICommand SelectPerformanceCommand { get; }

        public string SelectedPerformanceMetric
        {
            get => _selectedPerformanceMetric;
            private set
            {
                if (!SetProperty(ref _selectedPerformanceMetric, value))
                {
                    return;
                }

                OnPropertyChanged(nameof(IsCpuPerformance));
                OnPropertyChanged(nameof(IsMemoryPerformance));
                OnPropertyChanged(nameof(SelectedPerformanceTitle));
                OnPropertyChanged(nameof(SelectedPerformanceValue));
            }
        }

        public bool IsCpuPerformance => SelectedPerformanceMetric == "CPU";

        public bool IsMemoryPerformance => SelectedPerformanceMetric == "Memory";

        public string SelectedPerformanceTitle => IsCpuPerformance ? "CPU" : "Memory";

        public string SelectedPerformanceValue => IsCpuPerformance ? CpuUsage : MemoryUsage;

        public string CpuSummary => $"{CpuUsage}  {CpuFrequencyDisplay}";

        public string CpuFrequencyDisplay => "Live";

        public string MemorySummary => MemoryUsage;

        public string MemoryPercentDisplay
        {
            get
            {
                if (_memoryHistory.Count == 0)
                {
                    return "N/A";
                }

                return $"{_memoryHistory.Last():F0}%";
            }
        }

        public ProcessRowViewModel? SelectedProcess
        {
            get => _selectedProcess;
            set
            {
                if (!SetProperty(ref _selectedProcess, value))
                {
                    return;
                }

                (TerminateProcessCommand as RelayCommand)?.RaiseCanExecuteChanged();
                OnPropertyChanged(nameof(HasSelectedProcess));
                OnPropertyChanged(nameof(SelectedProcessName));
                OnPropertyChanged(nameof(SelectedProcessPid));
                OnPropertyChanged(nameof(SelectedProcessCpu));
                OnPropertyChanged(nameof(SelectedProcessMemory));
                OnPropertyChanged(nameof(SelectedProcessUptime));
            }
        }

        public bool HasSelectedProcess => SelectedProcess is not null;

        public string SelectedProcessName => SelectedProcess?.Name ?? "No process selected";

        public string SelectedProcessPid => SelectedProcess is null ? "—" : SelectedProcess.Pid;

        public string SelectedProcessCpu => SelectedProcess?.Cpu ?? "—";

        public string SelectedProcessMemory => SelectedProcess?.Memory ?? "—";

        public string SelectedProcessUptime => SelectedProcess?.Uptime ?? "—";

        public string StartProcessPath
        {
            get => _startProcessPath;
            set => SetProperty(ref _startProcessPath, value);
        }

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
                OnPropertyChanged(nameof(IsHistory));
                OnPropertyChanged(nameof(PageTitle));
                OnPropertyChanged(nameof(PageSubtitle));
            }
        }

        public bool IsDashboard => CurrentPage == "Dashboard";

        public bool IsProcesses => CurrentPage == "Processes";

        public bool IsPerformance => CurrentPage == "Performance";

        public bool IsSettings => CurrentPage == "Settings";

        public bool IsHistory => CurrentPage == "History";

        public string PageTitle => CurrentPage;

        public string PageSubtitle => CurrentPage switch
        {
            "Processes" => "Browse the monitored process list",
            "Performance" => "Review live CPU and memory usage",
            "Settings" => "Customize the appearance of your dashboard",
            "History" => "Review completed process runs",
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

        public bool IsAutoRefreshEnabled
        {
            get => _isAutoRefreshEnabled;
            set
            {
                if (!SetProperty(ref _isAutoRefreshEnabled, value))
                {
                    return;
                }

                if (value)
                {
                    _ = RefreshAsync();
                }
            }
        }

        public string ProcessFilterText
        {
            get => _processFilterText;
            set
            {
                if (!SetProperty(ref _processFilterText, value))
                {
                    return;
                }

                ProcessesView.Refresh();
            }
        }

        private void SelectPerformance(string? metric)
        {
            if (metric is "CPU" or "Memory")
            {
                SelectedPerformanceMetric = metric;
            }
        }

        public bool ShowSystemProcesses { get; set; } = true;

        private void Navigate(string? page)
        {
            if (page is "Dashboard" or "Processes" or "Performance" or "Settings" or "History")
            {
                CurrentPage = page;
            }
        }

        private async Task RefreshAsync()
        {
            if (!await _refreshGate.WaitAsync(0))
            {
                return;
            }

            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                var snapshots = (await Task.Run(() => _processMonitoringService.GetProcessList()))
                    .OrderBy(snapshot => snapshot.Name, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(snapshot => snapshot.Pid)
                    .ToList();

                await _dispatcher.InvokeAsync(() =>
                {
                    UpdateProcessRows(snapshots, removeMissing: true);
                    SetProperty(ref _processCount, snapshots.Count, nameof(ProcessCount));
                    LastUpdated = $"Live data refreshed {DateTime.Now:HH:mm:ss}";
                });
                _ = RefreshMetricsAsync();
            }
            catch (Exception exception)
            {
                ErrorMessage = $"Unable to load process data: {exception.Message}";
                LastUpdated = "Refresh failed";
            }
            finally
            {
                IsLoading = false;
                _refreshGate.Release();
            }
        }

        private async Task RefreshMetricsAsync()
        {
            if (!await _metricsRefreshGate.WaitAsync(0))
            {
                return;
            }

            try
            {
                var result = await Task.Run(() =>
                {
                    var snapshots = _processMonitoringService.GetRunningProcess();
                    var resources = _processMonitoringService.GetSystemResources();
                    return (snapshots, resources);
                });
                await _dispatcher.InvokeAsync(() =>
                {
                    UpdateProcessRows(result.snapshots, removeMissing: false);
                    UpdatePerformanceMetrics(result.resources);
                });
            }
            catch (Exception exception)
            {
                ErrorMessage = $"Unable to load process metrics: {exception.Message}";
            }
            finally
            {
                _metricsRefreshGate.Release();
            }
        }

        private void UpdatePerformanceMetrics(ProcPilot.Domain.Models.SystemResourceSnapshot resources)
        {
            // Keep the last known values while the next sampling interval is in progress.
            // CPU is intentionally unavailable for the first sample, so do not replace a
            // previously displayed value with N/A when that happens on a later refresh.
            if (resources.CpuPercent is double cpuPercent)
            {
                CpuUsage = $"{cpuPercent:F1}%";
                OnPropertyChanged(nameof(CpuSummary));
                AddChartSample(_cpuHistory, cpuPercent, CpuChartPoints);
                CpuAreaPoints = CreateAreaPoints(_cpuHistory);
                OnPropertyChanged(nameof(CpuChartPoints));
                OnPropertyChanged(nameof(CpuAreaPoints));
            }

            if (resources.MemoryTotalMb > 0)
            {
                MemoryUsage = $"{resources.MemoryUsedMb:N0} / {resources.MemoryTotalMb:N0} MB";
                var memoryPercent = resources.MemoryUsedMb / resources.MemoryTotalMb * 100;
                AddChartSample(_memoryHistory, memoryPercent, MemoryChartPoints);
                MemoryAreaPoints = CreateAreaPoints(_memoryHistory);
                OnPropertyChanged(nameof(MemorySummary));
                OnPropertyChanged(nameof(MemoryPercentDisplay));
                OnPropertyChanged(nameof(MemoryChartPoints));
                OnPropertyChanged(nameof(MemoryAreaPoints));
            }
        }

        private static void AddChartSample(
            Queue<double> history,
            double value,
            PointCollection points)
        {
            if (!double.IsFinite(value))
            {
                return;
            }

            history.Enqueue(Math.Clamp(value, 0, 100));
            while (history.Count > ChartSampleCount)
            {
                history.Dequeue();
            }

            points.Clear();
            var values = history.ToArray();
            for (var index = 0; index < values.Length; index++)
            {
                var x = values.Length == 1
                    ? ChartWidth
                    : index * ChartWidth / (values.Length - 1);
                var y = ChartHeight - values[index] / 100 * ChartHeight;
                points.Add(new Point(x, y));
            }
        }

        private static PointCollection CreateAreaPoints(Queue<double> history)
        {
            var areaPoints = new PointCollection();
            var values = history.ToArray();
            for (var index = 0; index < values.Length; index++)
            {
                var x = values.Length == 1
                    ? ChartWidth
                    : index * ChartWidth / (values.Length - 1);
                var y = ChartHeight - values[index] / 100 * ChartHeight;
                areaPoints.Add(new Point(x, y));
            }

            if (areaPoints.Count > 0)
            {
                areaPoints.Insert(0, new Point(areaPoints[0].X, ChartHeight));
                areaPoints.Add(new Point(areaPoints[^1].X, ChartHeight));
            }

            return areaPoints;
        }

        private async Task TerminateSelectedProcessAsync()
        {
            if (SelectedProcess is null)
            {
                return;
            }

            var process = SelectedProcess;
            var result = MessageBox.Show(
                $"End {process.Name} (PID {process.ProcessId})? Unsaved work may be lost.",
                "End process",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                await Task.Run(() => _processMonitoringService.TerminateProcess(process.ProcessId));
                SelectedProcess = null;
                await RefreshAsync();
            }
            catch (Exception exception)
            {
                ErrorMessage = $"Unable to end process: {exception.Message}";
            }
        }

        private void StartProcess()
        {
            try
            {
                _processMonitoringService.StartProcess(StartProcessPath);
                StartProcessPath = string.Empty;
                LastUpdated = "Process started";
                _ = RefreshAsync();
            }
            catch (Exception exception)
            {
                ErrorMessage = $"Unable to start process: {exception.Message}";
            }
        }

        private void UpdateProcessRows(
            IReadOnlyList<ProcPilot.Domain.Models.ProcessSnapshot> snapshots,
            bool removeMissing)
        {
            var snapshotsByPid = snapshots.ToDictionary(snapshot => snapshot.Pid);

            if (removeMissing)
            {
                for (var index = Processes.Count - 1; index >= 0; index--)
                {
                    if (!snapshotsByPid.ContainsKey(Processes[index].ProcessId))
                    {
                        Processes.RemoveAt(index);
                    }
                }
            }

            var rowsByPid = Processes.ToDictionary(row => row.ProcessId);
            foreach (var snapshot in snapshots)
            {
                if (rowsByPid.TryGetValue(snapshot.Pid, out var row))
                {
                    row.Update(snapshot);
                }
                else if (removeMissing)
                {
                    Processes.Add(new ProcessRowViewModel(snapshot));
                }
            }

            ProcessesView.Refresh();
        }

        public void Dispose()
        {
            _refreshTimer.Stop();
            _refreshTimer.Tick -= RefreshTimer_Tick;
        }

        private void RefreshTimer_Tick(object? sender, EventArgs e)
        {
            if (IsAutoRefreshEnabled && !IsLoading)
            {
                _ = RefreshAsync();
            }
        }

        private bool FilterProcess(object item)
        {
            return item is ProcessRowViewModel process
                && (string.IsNullOrWhiteSpace(ProcessFilterText)
                    || process.Name.Contains(ProcessFilterText, StringComparison.OrdinalIgnoreCase));
        }

        private void SaveSettings()
        {
            LastUpdated = IsAutoRefreshEnabled
                ? "Settings saved locally"
                : "Settings saved locally (auto-refresh off)";
        }
    }

    public sealed class ProcessRowViewModel : ViewModelBase
    {
        private string _name = string.Empty;
        private string _pid = string.Empty;
        private string _cpu = "N/A";
        private string _memory = "N/A";
        private string _uptime = "N/A";

        public ProcessRowViewModel(ProcPilot.Domain.Models.ProcessSnapshot snapshot)
        {
            ArgumentNullException.ThrowIfNull(snapshot);

            ProcessId = snapshot.Pid;
            IndicatorColor = new SolidColorBrush(Color.FromRgb(91, 108, 255));
            IndicatorColor.Freeze();
            Update(snapshot);
        }

        public int ProcessId { get; }

        public string Name => _name;

        public string Pid => _pid;

        public string Cpu => _cpu;

        public string Memory => _memory;

        public string Uptime => _uptime;

        public Brush IndicatorColor { get; }

        public void Update(ProcPilot.Domain.Models.ProcessSnapshot snapshot)
        {
            ArgumentNullException.ThrowIfNull(snapshot);

            SetProperty(ref _name, snapshot.Name);
            SetProperty(ref _pid, snapshot.Pid.ToString());

            // A process sample can temporarily omit CPU, memory, or start-time data
            // (for example for protected processes). Preserve the previous value until
            // a new valid sample arrives instead of flashing N/A in the grid.
            if (snapshot.CpuPercent is double cpu)
            {
                SetProperty(ref _cpu, $"{cpu:F1}%");
            }

            if (snapshot.RamMb is double memory)
            {
                SetProperty(ref _memory, $"{memory:N1} MB");
            }

            if (snapshot.Uptime is TimeSpan uptime)
            {
                SetProperty(ref _uptime, FormatUptime(uptime));
            }
        }

        private static string FormatUptime(TimeSpan uptime)
        {
            return uptime.TotalDays >= 1
                ? $"{uptime.Days}d {uptime.Hours}h"
                : $"{uptime.Hours}h {uptime.Minutes}m";
        }
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
