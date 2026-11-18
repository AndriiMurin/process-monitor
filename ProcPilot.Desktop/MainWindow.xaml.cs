using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using ProcPilot.Infrastructure.Services;
using ProcPilot.Desktop.ViewModels;

namespace ProcPilot.Desktop
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainWindowViewModel(new ProcessMonitoringService());
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ApplyAcrylicBackdrop();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed && e.OriginalSource is not System.Windows.Controls.Button)
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
            WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        }

        private void Window_StateChanged(object? sender, EventArgs e)
        {
            MaximizeButton.Content = WindowState == WindowState.Maximized ? "❐" : "□";
            MaximizeButton.ToolTip = WindowState == WindowState.Maximized ? "Restore" : "Maximize";
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
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