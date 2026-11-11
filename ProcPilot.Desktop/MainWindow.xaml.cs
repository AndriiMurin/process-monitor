using System.Windows;
using ProcPilot.Desktop.ViewModels;

namespace ProcPilot.Desktop
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainWindowViewModel();
        }
    }
}