using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProcPilot.Infrastructure;
using ProcPilot.Infrastructure.Database;
using ProcPilot.Desktop.ViewModels;

namespace ProcPilot.Desktop
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        public static IServiceProvider Services { get; private set; } = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var services = new ServiceCollection();
            services.AddInfrastructure();
            services.AddTransient<MainWindow>();

            Services = services.BuildServiceProvider();

            // zastosowanie migracji
            using var scope = Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            dbContext.Database.Migrate();

            var window = Services.GetRequiredService<MainWindow>();
            window.Show();
        }
    }
}