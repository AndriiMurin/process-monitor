using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using ProcPilot.Application.Interfaces;
using ProcPilot.Desktop.ViewModels;

namespace ProcPilot.Desktop
{
    public partial class MainWindow : Window
    {
        private Rect? _normalBounds;

        public MainWindow(IProcessMonitoringService processMonitoringService)
        {
            InitializeComponent();
            var viewModel = new MainWindowViewModel(processMonitoringService);
            DataContext = viewModel;
            CpuGraph.Points = viewModel.CpuChartPoints;
            MemoryGraph.Points = viewModel.MemoryChartPoints;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.CanResize;
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed
                && e.OriginalSource is not System.Windows.Controls.Button
                && e.OriginalSource is not System.Windows.Controls.Primitives.ButtonBase)
            {
                DragMove();
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (_normalBounds is Rect normalBounds)
            {
                WindowState = WindowState.Normal;
                Left = normalBounds.Left;
                Top = normalBounds.Top;
                Width = normalBounds.Width;
                Height = normalBounds.Height;
                _normalBounds = null;
                return;
            }

            var workArea = SystemParameters.WorkArea;
            WindowState = WindowState.Normal;
            _normalBounds = new Rect(Left, Top, Width, Height);
            Left = workArea.Left;
            Top = workArea.Top;
            Width = workArea.Width;
            Height = workArea.Height;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_Closed(object? sender, EventArgs e)
        {
            if (DataContext is MainWindowViewModel viewModel)
            {
                viewModel.Dispose();
            }
        }

        private void ApplyAcrylicBackdrop()
        {
            var handle = new WindowInteropHelper(this).Handle;

            var accentPolicy = new AccentPolicy
            {
                AccentState = AccentEnableAcrylicBlurBehind,
                AccentFlags = 2,
                GradientColor = unchecked((int)0xC43B536A)
            };

            var accentSize = Marshal.SizeOf(accentPolicy);
            var accentPointer = Marshal.AllocHGlobal(accentSize);

            try
            {
                Marshal.StructureToPtr(accentPolicy, accentPointer, false);
                var data = new WindowCompositionAttributeData
                {
                    Attribute = WindowCompositionAttribute,
                    Data = accentPointer,
                    SizeOfData = accentSize
                };

                SetWindowCompositionAttribute(handle, ref data);

                // Windows 11 uses the native system backdrop when available.
                var backdropType = 3;
                DwmSetWindowAttribute(handle, DwmwaSystemBackdropType, ref backdropType, sizeof(int));
            }
            finally
            {
                Marshal.FreeHGlobal(accentPointer);
            }
        }

        private const int WindowCompositionAttribute = 0x13;
        private const int AccentEnableAcrylicBlurBehind = 4;
        private const int DwmwaSystemBackdropType = 38;

        [DllImport("user32.dll")]
        private static extern int SetWindowCompositionAttribute(IntPtr windowHandle, ref WindowCompositionAttributeData data);

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr windowHandle, int attribute, ref int value, int valueSize);

        [StructLayout(LayoutKind.Sequential)]
        private struct AccentPolicy
        {
            public int AccentState;
            public int AccentFlags;
            public int GradientColor;
            public int AnimationId;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WindowCompositionAttributeData
        {
            public int Attribute;
            public IntPtr Data;
            public int SizeOfData;
        }
    }
}