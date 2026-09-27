using System.Collections.Concurrent;

namespace PFXManager.Infrastructure.FileSystem;

/// <summary>Thread-safe set of canonical directory paths already entered, used to break reparse-point cycles.</summary>
internal sealed class VisitedDirectorySet
{
    private readonly ConcurrentDictionary<string, byte> _visited = new(StringComparer.OrdinalIgnoreCase);

    public bool TryAdd(string canonicalPath) => _visited.TryAdd(canonicalPath, 0);
}
