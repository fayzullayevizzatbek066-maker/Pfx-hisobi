using PFXManager.Core.Models;

namespace PFXManager.Core.Interfaces;

/// <summary>
/// Moves discovered PFX files into a per-session quarantine folder, and can restore or
/// permanently delete them later. Never operates on <see cref="CertificateStatus.PasswordRequired"/>
/// or <see cref="CertificateStatus.ReadError"/> records as part of a bulk-expired workflow — callers
/// are responsible for pre-filtering with the status engine.
/// </summary>
public interface IQuarantineService
{
    Task<IReadOnlyList<QuarantineResult>> QuarantineAsync(
        IReadOnlyList<CertificateRecord> records,
        CancellationToken cancellationToken);

    Task<RestoreResult> RestoreAsync(Guid quarantineItemId, RestoreOptions options, CancellationToken cancellationToken);

    Task<bool> PermanentlyDeleteAsync(Guid quarantineItemId, CancellationToken cancellationToken);
}
