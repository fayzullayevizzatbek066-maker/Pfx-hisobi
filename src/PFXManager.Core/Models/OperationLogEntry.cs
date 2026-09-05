namespace PFXManager.Core.Models;

public sealed class OperationLogEntry
{
    public long Id { get; set; }
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    public required string Action { get; set; }
    public string? Details { get; set; }
    public bool Success { get; set; } = true;
}
