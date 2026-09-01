using System.Runtime.CompilerServices;
using System.Threading.Channels;
using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.Logging;
using PFXManager.Core.Enums;
using PFXManager.Core.Interfaces;
using PFXManager.Core.Models;

namespace PFXManager.Infrastructure.FileSystem;

/// <summary>
/// Recursive, asynchronous, cancellable *.pfx / *.p12 finder with bounded directory-level
/// concurrency (via a self-feeding <see cref="ActionBlock{T}"/>) and per-directory error
/// isolation: a locked, missing, or inaccessible directory is reported through
/// <c>onError</c> and skipped, never aborting the overall scan.
///
/// Reparse points (junctions/symlinks) are skipped by default; even when
/// <see cref="ScanOptions.FollowReparsePoints"/> is set, a visited-set of canonical directory
/// paths prevents unbounded recursion from a cycle.
/// </summary>
public sealed class FileSystemScanner : IFileSystemScanner
{
    private static readonly string[] TargetExtensions = { ".pfx", ".p12" };
    private readonly ILogger<FileSystemScanner> _logger;

    public FileSystemScanner(ILogger<FileSystemScanner> logger)
    {
        _logger = logger;
    }

    public async IAsyncEnumerable<string> ScanAsync(
        ScanOptions options,
        IProgress<ScanProgress>? progress,
        Action<ScanError>? onError,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var resultChannel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        long filesChecked = 0;
        long pfxFound = 0;
        long errorCount = 0;
        var lastReportTicks = Environment.TickCount64;
        var progressLock = new object();

        void ReportError(string path, ScanErrorKind kind, string message)
        {
            Interlocked.Increment(ref errorCount);
            try
            {
                onError?.Invoke(new ScanError { Path = path, Kind = kind, Message = message });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "onError callback threw while reporting scan error for {Path}", path);
            }
        }

        void ReportProgress(string currentDirectory, bool force = false)
        {
            var now = Environment.TickCount64;
            lock (progressLock)
            {
                if (!force && now - lastReportTicks < 100)
                {
                    return;
                }

                lastReportTicks = now;
            }

            progress?.Report(new ScanProgress
            {
                CurrentDirectory = currentDirectory,
                FilesChecked = Interlocked.Read(ref filesChecked),
                PfxFound = Interlocked.Read(ref pfxFound),
                ErrorCount = Interlocked.Read(ref errorCount)
            });
        }

        var visited = new VisitedDirectorySet();
        var degreeOfParallelism = Math.Max(1, Math.Min(options.MaxDegreeOfParallelism, Environment.ProcessorCount));

        var producerTask = Task.Run(async () =>
        {
            try
            {
                await RunTraversalAsync(
                    options,
                    degreeOfParallelism,
                    visited,
                    resultChannel.Writer,
                    ReportError,
                    ReportProgress,
                    () => Interlocked.Increment(ref filesChecked),
                    () => Interlocked.Increment(ref pfxFound),
                    cancellationToken);
            }
            finally
            {
                resultChannel.Writer.TryComplete();
            }
        }, CancellationToken.None);

        await foreach (var file in resultChannel.Reader.ReadAllAsync(cancellationToken))
        {
            yield return file;
        }

        await producerTask;
        ReportProgress(string.Empty, force: true);
    }

    private static async Task RunTraversalAsync(
        ScanOptions options,
        int degreeOfParallelism,
        VisitedDirectorySet visited,
        ChannelWriter<string> resultWriter,
        Action<string, ScanErrorKind, string> reportError,
        Action<string, bool> reportProgress,
        Action incrementFilesChecked,
        Action incrementPfxFound,
        CancellationToken cancellationToken)
    {
        long outstanding = 0;
        ActionBlock<string>? block = null;

        block = new ActionBlock<string>(
            directory => ProcessDirectoryAsync(
                directory, options, visited, resultWriter, reportError, reportProgress,
                incrementFilesChecked, incrementPfxFound,
                subdirectory =>
                {
                    Interlocked.Increment(ref outstanding);
                    if (!block!.Post(subdirectory))
                    {
                        Interlocked.Decrement(ref outstanding);
                    }
                },
                () =>
                {
                    if (Interlocked.Decrement(ref outstanding) == 0)
                    {
                        block!.Complete();
                    }
                }),
            new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = degreeOfParallelism,
                CancellationToken = cancellationToken,
                EnsureOrdered = false
            });

        var postedAny = false;
        foreach (var root in options.RootPaths)
        {
            if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
            {
                reportError(root ?? string.Empty, ScanErrorKind.DirectoryNotFound, "Root path does not exist or is not accessible.");
                continue;
            }

            Interlocked.Increment(ref outstanding);
            if (block.Post(root))
            {
                postedAny = true;
            }
            else
            {
                Interlocked.Decrement(ref outstanding);
            }
        }

        if (!postedAny)
        {
            block.Complete();
        }

        await block.Completion;
    }

    private static async Task ProcessDirectoryAsync(
        string directory,
        ScanOptions options,
        VisitedDirectorySet visited,
        ChannelWriter<string> resultWriter,
        Action<string, ScanErrorKind, string> reportError,
        Action<string, bool> reportProgress,
        Action incrementFilesChecked,
        Action incrementPfxFound,
        Action<string> enqueueSubdirectory,
        Action onDirectoryComplete)
    {
        try
        {
            if (!TryEnterDirectory(directory, visited, options.FollowReparsePoints, reportError, out var canonicalPath))
            {
                return;
            }

            reportProgress(canonicalPath, false);

            var (files, subdirectories) = EnumerateDirectory(directory, reportError);

            foreach (var file in files)
            {
                incrementFilesChecked();
                var extension = Path.GetExtension(file);
                if (TargetExtensions.Any(ext => string.Equals(ext, extension, StringComparison.OrdinalIgnoreCase)))
                {
                    incrementPfxFound();
                    await resultWriter.WriteAsync(file);
                }
            }

            foreach (var subdirectory in subdirectories)
            {
                enqueueSubdirectory(subdirectory);
            }
        }
        finally
        {
            onDirectoryComplete();
        }
    }

    private static bool TryEnterDirectory(
        string path,
        VisitedDirectorySet visited,
        bool followReparsePoints,
        Action<string, ScanErrorKind, string> reportError,
        out string canonicalPath)
    {
        canonicalPath = path;

        DirectoryInfo info;
        try
        {
            info = new DirectoryInfo(path);
            if (!info.Exists)
            {
                reportError(path, ScanErrorKind.DirectoryNotFound, "Directory no longer exists.");
                return false;
            }
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or PathTooLongException)
        {
            reportError(path, ClassifyException(ex), ex.Message);
            return false;
        }

        var isReparsePoint = info.Attributes.HasFlag(FileAttributes.ReparsePoint);
        if (isReparsePoint && !followReparsePoints)
        {
            reportError(path, ScanErrorKind.ReparsePointSkipped, "Reparse point (junction/symlink) skipped.");
            return false;
        }

        canonicalPath = info.FullName;
        if (isReparsePoint)
        {
            try
            {
                var target = info.LinkTarget;
                if (!string.IsNullOrEmpty(target))
                {
                    canonicalPath = target;
                }
            }
            catch (IOException)
            {
                // Target could not be resolved; fall back to the reparse point's own path for cycle tracking.
            }
        }

        if (!visited.TryAdd(canonicalPath))
        {
            reportError(path, ScanErrorKind.ReparsePointSkipped, "Directory already visited (cycle prevented).");
            return false;
        }

        return true;
    }

    private static (List<string> Files, List<string> Subdirectories) EnumerateDirectory(
        string path,
        Action<string, ScanErrorKind, string> reportError)
    {
        var files = new List<string>();
        var subdirectories = new List<string>();

        IEnumerator<FileSystemInfo> enumerator;
        try
        {
            enumerator = new DirectoryInfo(path).EnumerateFileSystemInfos().GetEnumerator();
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or PathTooLongException or DirectoryNotFoundException)
        {
            reportError(path, ClassifyException(ex), ex.Message);
            return (files, subdirectories);
        }

        using (enumerator)
        {
            while (true)
            {
                FileSystemInfo current;
                try
                {
                    if (!enumerator.MoveNext())
                    {
                        break;
                    }

                    current = enumerator.Current;
                }
                catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or PathTooLongException)
                {
                    reportError(path, ClassifyException(ex), ex.Message);
                    break;
                }

                switch (current)
                {
                    case DirectoryInfo dirInfo:
                        subdirectories.Add(dirInfo.FullName);
                        break;
                    case FileInfo fileInfo:
                        files.Add(fileInfo.FullName);
                        break;
                }
            }
        }

        return (files, subdirectories);
    }

    private static ScanErrorKind ClassifyException(Exception ex) => ex switch
    {
        UnauthorizedAccessException => ScanErrorKind.AccessDenied,
        PathTooLongException => ScanErrorKind.PathTooLong,
        DirectoryNotFoundException => ScanErrorKind.DirectoryNotFound,
        IOException => ScanErrorKind.IoError,
        _ => ScanErrorKind.Unknown
    };
}
