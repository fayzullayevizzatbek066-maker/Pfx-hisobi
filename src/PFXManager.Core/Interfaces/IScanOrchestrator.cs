using PFXManager.Core.Models;

namespace PFXManager.Core.Interfaces;

/// <summary>
/// Ties together drive/file discovery, certificate parsing, status computation, duplicate
/// detection and persistence into the single "Kompyuterni skanerlash" workflow.
/// </summary>
public interface IScanOrchestrator
{
    Task<ScanSession> RunScanAsync(ScanOptions options, IProgress<ScanProgress>? progress, CancellationToken cancellationToken);
}
