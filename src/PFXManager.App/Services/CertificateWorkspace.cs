using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using PFXManager.App.ViewModels;
using PFXManager.Core.Interfaces;
using PFXManager.Core.Models;

namespace PFXManager.App.Services;

public sealed class CertificateWorkspace : ICertificateWorkspace
{
    private readonly ICertificateRecordRepository _recordRepository;
    private readonly IDuplicateDetectionService _duplicateDetectionService;
    private readonly IScanOrchestrator _scanOrchestrator;
    private readonly Dispatcher _dispatcher;

    public CertificateWorkspace(
        ICertificateRecordRepository recordRepository,
        IDuplicateDetectionService duplicateDetectionService,
        IScanOrchestrator scanOrchestrator)
    {
        _recordRepository = recordRepository;
        _duplicateDetectionService = duplicateDetectionService;
        _scanOrchestrator = scanOrchestrator;
        _dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
    }

    public ObservableCollection<CertificateRecordViewModel> Records { get; } = new();

    public async Task ReloadAsync(CancellationToken cancellationToken = default)
    {
        var records = await _recordRepository.GetAllAsync(cancellationToken);

        // Duplicate group ids are persisted from the last scan, but recomputing here keeps the
        // Duplicates page correct even if records were added/removed since (e.g. after a restore).
        _duplicateDetectionService.FindDuplicates(records);

        await _dispatcher.InvokeAsync(() =>
        {
            Records.Clear();
            foreach (var record in records.OrderBy(r => r.FullPath, StringComparer.OrdinalIgnoreCase))
            {
                Records.Add(new CertificateRecordViewModel(record));
            }
        });
    }

    public async Task<ScanSession> RunScanAsync(ScanOptions options, IProgress<ScanProgress> progress, CancellationToken cancellationToken)
    {
        var session = await _scanOrchestrator.RunScanAsync(options, progress, cancellationToken);
        await ReloadAsync(CancellationToken.None);
        return session;
    }
}
