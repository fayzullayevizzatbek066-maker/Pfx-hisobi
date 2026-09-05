namespace PFXManager.Core.Interfaces;

/// <summary>
/// Structured audit trail for user- and system-initiated actions. Implementations and callers
/// must never pass a password, private key material, or other secret in <paramref name="details"/>.
/// </summary>
public interface IAuditLogger
{
    Task LogAsync(string action, string? details = null, bool success = true, CancellationToken cancellationToken = default);
}
