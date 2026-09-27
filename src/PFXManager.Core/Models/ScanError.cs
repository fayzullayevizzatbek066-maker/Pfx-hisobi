using PFXManager.Core.Enums;

namespace PFXManager.Core.Models;

public sealed class ScanError
{
    public required string Path { get; init; }
    public ScanErrorKind Kind { get; init; }
    public required string Message { get; init; }
    public DateTime OccurredAtUtc { get; init; } = DateTime.UtcNow;
}
