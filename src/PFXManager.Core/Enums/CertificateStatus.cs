namespace PFXManager.Core.Enums;

/// <summary>
/// Certificate lifecycle status as determined solely from parsed X.509 validity data.
/// Never inferred from file name or file system timestamps.
/// </summary>
public enum CertificateStatus
{
    /// <summary>NotAfter is more than 90 days in the future.</summary>
    Active,

    /// <summary>NotAfter is between 31 and 90 days in the future.</summary>
    Expiring,

    /// <summary>NotAfter is between 0 and 30 days in the future.</summary>
    ExpiringSoon,

    /// <summary>NotAfter is in the past.</summary>
    Expired,

    /// <summary>The PFX/P12 container is password protected and has not been unlocked yet.</summary>
    PasswordRequired,

    /// <summary>The file could not be parsed (corrupted, unsupported format, I/O error, etc).</summary>
    ReadError
}
