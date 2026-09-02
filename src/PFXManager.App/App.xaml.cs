using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PFXManager.App.Resources;
using PFXManager.App.Services;
using PFXManager.App.ViewModels;
using PFXManager.App.Views;
using PFXManager.Core.Interfaces;
using PFXManager.Infrastructure.DependencyInjection;
using PFXManager.Infrastructure.Persistence;

namespace PFXManager.App;

public partial class App : System.Windows.Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

        _host = Host.CreateDefaultBuilder()
            .ConfigureServices(ConfigureServices)
            .Build();

        var migrator = _host.Services.GetRequiredService<DatabaseMigrator>();
        migrator.Migrate();

        var auditLogger = _host.Services.GetRequiredService<IAuditLogger>();
        await auditLogger.LogAsync("application_started");

        var mainViewModel = _host.Services.GetRequiredService<MainViewModel>();
        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.DataContext = mainViewModel;
        MainWindow = mainWindow;
        mainWindow.Show();

        await mainViewModel.InitializeAsync();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddPfxManagerInfrastructure();

        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<IExplorerService, ExplorerService>();
        services.AddSingleton<ICertificateWorkspace, CertificateWorkspace>();

        // Page view models are singletons so navigating away and back preserves filter/selection
        // state (rather than re-querying and losing what the user had set up).
        services.AddSingleton<DashboardViewModel>();
        services.AddSingleton<PfxFilesViewModel>();
        services.AddSingleton<WindowsCertificatesViewModel>();
        services.AddSingleton<DuplicatesViewModel>();
        services.AddSingleton<QuarantineViewModel>();
        services.AddSingleton<ScanHistoryViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<MainViewModel>();

        services.AddSingleton<MainWindow>();
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show(
            $"Kutilmagan xatolik yuz berdi:\n{e.Exception.Message}",
            Strings.AppTitle,
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        e.Handled = true;
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            MessageBox.Show(
                $"Jiddiy xatolik yuz berdi:\n{ex.Message}",
                Strings.AppTitle,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            var auditLogger = _host.Services.GetService<IAuditLogger>();
            if (auditLogger is not null)
            {
                await auditLogger.LogAsync("application_exit");
            }

            _host.Dispose();
        }

        base.OnExit(e);
    }
}
