using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using ProcPilot.Infrastructure;
using ProcPilot.Infrastructure.Database;

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

            Services = services.BuildServiceProvider();

            // połączenia z bazą SQLite
            using var scope = Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            dbContext.Database.EnsureCreated();
        }
    }
}