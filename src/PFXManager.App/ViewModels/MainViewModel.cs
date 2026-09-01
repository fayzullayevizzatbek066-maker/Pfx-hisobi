using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using PFXManager.App.Resources;
using PFXManager.App.Services;
using PFXManager.Core.Enums;
using PFXManager.Core.Interfaces;
using PFXManager.Core.Models;

namespace PFXManager.App.ViewModels;

public sealed partial class MainViewModel : ObservableObject, IRecipient<NavigateToPfxFilesMessage>
{
    private readonly ICertificateWorkspace _workspace;
    private readonly IAppSettingsRepository _settingsRepository;
    private readonly IDriveDiscoveryService _driveDiscoveryService;
    private readonly IDialogService _dialogService;
    private CancellationTokenSource? _scanCts;

    public MainViewModel(
        ICertificateWorkspace workspace,
        IAppSettingsRepository settingsRepository,
        IDriveDiscoveryService driveDiscoveryService,
        IDialogService dialogService,
        DashboardViewModel dashboardViewModel,
        PfxFilesViewModel pfxFilesViewModel,
        WindowsCertificatesViewModel windowsCertificatesViewModel,
        DuplicatesViewModel duplicatesViewModel,
        QuarantineViewModel quarantineViewModel,
        ScanHistoryViewModel scanHistoryViewModel,
        SettingsViewModel settingsViewModel)
    {
        _workspace = workspace;
        _settingsRepository = settingsRepository;
        _driveDiscoveryService = driveDiscoveryService;
        _dialogService = dialogService;

        Dashboard = dashboardViewModel;
        PfxFiles = pfxFilesViewModel;
        WindowsCertificates = windowsCertificatesViewModel;
        Duplicates = duplicatesViewModel;
        Quarantine = quarantineViewModel;
        ScanHistory = scanHistoryViewModel;
        Settings = settingsViewModel;

        _currentPageViewModel = Dashboard;

        WeakReferenceMessenger.Default.RegisterAll(this);
    }

    public DashboardViewModel Dashboard { get; }
    public PfxFilesViewModel PfxFiles { get; }
    public WindowsCertificatesViewModel WindowsCertificates { get; }
    public DuplicatesViewModel Duplicates { get; }
    public QuarantineViewModel Quarantine { get; }
    public ScanHistoryViewModel ScanHistory { get; }
    public SettingsViewModel Settings { get; }

    [ObservableProperty]
    private object _currentPageViewModel;

    [ObservableProperty]
    private bool _isScanning;

    [ObservableProperty]
    private string _scanCurrentDirectory = string.Empty;

    [ObservableProperty]
    private long _scanFilesChecked;

    [ObservableProperty]
    private long _scanPfxFound;

    [ObservableProperty]
    private long _scanErrorCount;

    public async Task InitializeAsync()
    {
        await _workspace.ReloadAsync();
        await Settings.LoadCommand.ExecuteAsync(null);
        await ScanHistory.LoadCommand.ExecuteAsync(null);
        await Quarantine.LoadCommand.ExecuteAsync(null);
    }

    [RelayCommand] private void NavigateDashboard() => CurrentPageViewModel = Dashboard;
    [RelayCommand] private void NavigatePfxFiles() => CurrentPageViewModel = PfxFiles;
    [RelayCommand] private void NavigateWindowsCertificates() => CurrentPageViewModel = WindowsCertificates;
    [RelayCommand] private void NavigateDuplicates() => CurrentPageViewModel = Duplicates;
    [RelayCommand] private void NavigateQuarantine() => CurrentPageViewModel = Quarantine;
    [RelayCommand] private void NavigateScanHistory() => CurrentPageViewModel = ScanHistory;
    [RelayCommand] private void NavigateSettings() => CurrentPageViewModel = Settings;

    public void Receive(NavigateToPfxFilesMessage message) => CurrentPageViewModel = PfxFiles;

    private bool CanStartScan() => !IsScanning;

    [RelayCommand(CanExecute = nameof(CanStartScan))]
    private async Task StartScanAsync()
    {
        var settings = await _settingsRepository.LoadAsync(CancellationToken.None);
        var roots = new List<string>();

        var drives = _driveDiscoveryService.DiscoverDrives();
        foreach (var drive in drives)
        {
            if (!drive.IsReady)
            {
                continue;
            }

            var include = drive.Kind switch
            {
                DriveScanKind.LocalFixed => settings.ScanLocalFixedDrives,
                DriveScanKind.Network => settings.ScanNetworkDrives,
                DriveScanKind.Removable => settings.ScanRemovableDrives,
                _ => false
            };

            if (include)
            {
                roots.Add(drive.RootPath);
            }
        }

        roots.AddRange(settings.CustomFolders.Where(Directory.Exists));

        if (roots.Count == 0)
        {
            _dialogService.ShowError("Skanerlash uchun hech qanday manzil tanlanmagan. Sozlamalarni tekshiring.");
            return;
        }

        _scanCts = new CancellationTokenSource();
        IsScanning = true;
        ScanCurrentDirectory = string.Empty;
        ScanFilesChecked = 0;
        ScanPfxFound = 0;
        ScanErrorCount = 0;
        StartScanCommand.NotifyCanExecuteChanged();

        var progress = new Progress<ScanProgress>(p =>
        {
            ScanCurrentDirectory = p.CurrentDirectory;
            ScanFilesChecked = p.FilesChecked;
            ScanPfxFound = p.PfxFound;
            ScanErrorCount = p.ErrorCount;
        });

        try
        {
            var options = new ScanOptions(roots, FollowReparsePoints: false, MaxDegreeOfParallelism: 4);
            var session = await _workspace.RunScanAsync(options, progress, _scanCts.Token);
            await ScanHistory.LoadCommand.ExecuteAsync(null);

            // ScanOrchestrator absorbs OperationCanceledException internally and reports it via
            // ScanSession.WasCancelled instead of throwing, so cancellation is detected here from
            // the returned session rather than from a caught exception.
            _dialogService.ShowInfo(session.WasCancelled
                ? Strings.ScanCancelled
                : string.Format(Strings.ScanCompletedFormat, session.PfxFound, session.ExpiredCount, session.ErrorCount));
        }
        catch (OperationCanceledException)
        {
            await ScanHistory.LoadCommand.ExecuteAsync(null);
            _dialogService.ShowInfo(Strings.ScanCancelled);
        }
        finally
        {
            IsScanning = false;
            _scanCts?.Dispose();
            _scanCts = null;
            StartScanCommand.NotifyCanExecuteChanged();
        }
    }

    [RelayCommand]
    private void CancelScan() => _scanCts?.Cancel();
}
