using PFXManager.Core.Enums;
using PFXManager.Core.Interfaces;

namespace PFXManager.Core.Services;

public sealed class CertificateStatusEngine : ICertificateStatusEngine
{
    private readonly TimeProvider _timeProvider;

    public CertificateStatusEngine(TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public CertificateStatus DetermineStatus(
        DateTime? notAfter,
        bool passwordRequired,
        bool readError,
        int expiringSoonDays = 30,
        int expiringDays = 90)
    {
        if (passwordRequired)
        {
            return CertificateStatus.PasswordRequired;
        }

        if (readError || notAfter is null)
        {
            return CertificateStatus.ReadError;
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        if (notAfter.Value < now)
        {
            return CertificateStatus.Expired;
        }

        var remainingDays = ComputeRemainingDays(notAfter.Value);
        if (remainingDays <= expiringSoonDays)
        {
            return CertificateStatus.ExpiringSoon;
        }

        if (remainingDays <= expiringDays)
        {
            return CertificateStatus.Expiring;
        }

        return CertificateStatus.Active;
    }

    public int ComputeRemainingDays(DateTime notAfter)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var span = notAfter - now;
        return (int)Math.Ceiling(span.TotalDays);
    }
}
