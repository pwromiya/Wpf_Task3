using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Wpf_Task3.Data;
using Wpf_Task3.Services;
using Wpf_Task3.ViewModels;

namespace Wpf_Task3;

/// <summary>
/// Interaction logic for App.xaml
/// Handles application lifecycle, Dependency Injection, and Configuration
/// </summary>
public partial class App : Application
{
    private readonly IHost _host;

    public App()
    {
        // Initialize the Generic Host to manage services and configuration
        _host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                // Setup configuration to read from appsettings.json
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            })
            .ConfigureServices((context, services) =>
            {
                // Retrieve the connection string from configuration
                var connectionString = context.Configuration.GetConnectionString("DefaultConnection");

                // Register the Database Context Factory for thread-safe DB operations
                services.AddDbContextFactory<AppDbContext>(options =>
                    options.UseSqlServer(connectionString));

                // Register application services and viewmodels (Dependency Injection)
                services.AddSingleton<IDataService, DataService>();
                services.AddSingleton<FileService>();
                services.AddSingleton<MainViewModel>();
                services.AddSingleton<MainWindow>();
            })
            .Build();
    }

    /// <summary>
    /// Executes when the application starts
    /// </summary>
    protected override async void OnStartup(StartupEventArgs e)
    {
        // Start the host and its background services
        await _host.StartAsync();

        // Ensure the database is created at startup
        var factory = _host.Services.GetRequiredService<IDbContextFactory<AppDbContext>>();
        using (var db = factory.CreateDbContext())
        {
            db.Database.EnsureCreated();
        }

        // Resolve the MainWindow and show it
        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();

        base.OnStartup(e);
    }

    /// <summary>
    /// Executes when the application shuts down
    /// </summary>
    protected override async void OnExit(ExitEventArgs e)
    {
        // Gracefully stop the host and release resources
        await _host.StopAsync();
        _host.Dispose();

        base.OnExit(e);
    }
}