using PFXManager.Core.Enums;

namespace PFXManager.Core.Interfaces;

/// <summary>
/// Centralized, single source of truth for turning parsed certificate validity data into a
/// <see cref="CertificateStatus"/>. Every part of the app (grid, dashboard cards, bulk selection)
/// must go through this service instead of re-deriving status locally.
/// </summary>
public interface ICertificateStatusEngine
{
    /// <summary>
    /// Determines status purely from certificate validity / read outcome. File name and file
    /// system timestamps are never inputs to this decision.
    /// </summary>
    /// <param name="expiringSoonDays">Threshold (default 30) for the ExpiringSoon bucket.</param>
    /// <param name="expiringDays">Threshold (default 90) for the Expiring bucket.</param>
    CertificateStatus DetermineStatus(
        DateTime? notAfter,
        bool passwordRequired,
        bool readError,
        int expiringSoonDays = 30,
        int expiringDays = 90);

    /// <summary>Whole days remaining until <paramref name="notAfter"/>; negative if already expired.</summary>
    int ComputeRemainingDays(DateTime notAfter);
}
