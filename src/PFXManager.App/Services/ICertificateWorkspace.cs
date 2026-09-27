using System.Collections.ObjectModel;
using PFXManager.App.ViewModels;
using PFXManager.Core.Interfaces;
using PFXManager.Core.Models;

namespace PFXManager.App.Services;

/// <summary>
/// Single, shared, in-memory view of everything discovered so far (backed by SQLite). Dashboard,
/// PFX Files, and Duplicates all read the same <see cref="Records"/> collection instead of each
/// re-querying the database, so a quarantine/restore/scan on one page is instantly reflected
/// everywhere else.
/// </summary>
public interface ICertificateWorkspace
{
    ObservableCollection<CertificateRecordViewModel> Records { get; }

    Task ReloadAsync(CancellationToken cancellationToken = default);

    Task<ScanSession> RunScanAsync(ScanOptions options, IProgress<ScanProgress> progress, CancellationToken cancellationToken);
}
