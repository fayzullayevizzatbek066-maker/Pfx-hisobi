using Microsoft.Extensions.Logging;
using PFXManager.Core.Enums;
using PFXManager.Core.Interfaces;
using PFXManager.Core.Models;

namespace PFXManager.Infrastructure.Scanning;

/// <summary>
/// Drives a full "Kompyuterni skanerlash" run: discover files, parse each one, compute status,
/// detect duplicates, and persist everything as one ScanSession. Files are parsed with bounded
/// concurrency and results are flushed to the database in batches so memory stays flat even for
/// very large scans (AC-04, section 34 performance requirements).
/// </summary>
public sealed class ScanOrchestrator : IScanOrchestrator
{
    private const int PersistBatchSize = 250;
    private const int ParseConcurrency = 4;

    private readonly IFileSystemScanner _scanner;
    private readonly ICertificateRecordFactory _recordFactory;
    private readonly IDuplicateDetectionService _duplicateDetectionService;
    private readonly ICertificateRecordRepository _recordRepository;
    private readonly IScanSessionRepository _scanSessionRepository;
    private readonly IAuditLogger _auditLogger;
    private readonly ILogger<ScanOrchestrator> _logger;

    public ScanOrchestrator(
        IFileSystemScanner scanner,
        ICertificateRecordFactory recordFactory,
        IDuplicateDetectionService duplicateDetectionService,
        ICertificateRecordRepository recordRepository,
        IScanSessionRepository scanSessionRepository,
        IAuditLogger auditLogger,
        ILogger<ScanOrchestrator> logger)
    {
        _scanner = scanner;
        _recordFactory = recordFactory;
        _duplicateDetectionService = duplicateDetectionService;
        _recordRepository = recordRepository;
        _scanSessionRepository = scanSessionRepository;
        _auditLogger = auditLogger;
        _logger = logger;
    }

    public async Task<ScanSession> RunScanAsync(ScanOptions options, IProgress<ScanProgress>? progress, CancellationToken cancellationToken)
    {
        var session = new ScanSession { RootsScanned = options.RootPaths };
        await _scanSessionRepository.AddAsync(session, cancellationToken);
        await _auditLogger.LogAsync("scan_started", string.Join(", ", options.RootPaths), cancellationToken: cancellationToken);

        var allRecords = new List<CertificateRecord>();
        var pendingBatch = new List<CertificateRecord>(PersistBatchSize);
        var errorCount = 0L;
        var lastFilesChecked = 0L;

        void OnError(ScanError error)
        {
            Interlocked.Increment(ref errorCount);
        }

        var forwardingProgress = new Progress<ScanProgress>(p =>
        {
            Interlocked.Exchange(ref lastFilesChecked, p.FilesChecked);
            progress?.Report(p);
        });

        using var parseGate = new SemaphoreSlim(ParseConcurrency);
        var parseTasks = new List<Task>();
        var wasCancelled = false;

        try
        {
            await foreach (var filePath in _scanner.ScanAsync(options, forwardingProgress, OnError, cancellationToken))
            {
                await parseGate.WaitAsync(cancellationToken);
                var task = Task.Run(async () =>
                {
                    try
                    {
                        var record = await _recordFactory.BuildAsync(filePath, password: null, session.Id, cancellationToken);
                        lock (pendingBatch)
                        {
                            allRecords.Add(record);
                            pendingBatch.Add(record);
                        }
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        _logger.LogWarning(ex, "Failed to build certificate record for {Path}", filePath);
                        Interlocked.Increment(ref errorCount);
                    }
                    finally
                    {
                        parseGate.Release();
                    }
                }, cancellationToken);
                parseTasks.Add(task);

                List<CertificateRecord>? flush = null;
                lock (pendingBatch)
                {
                    if (pendingBatch.Count >= PersistBatchSize)
                    {
                        flush = new List<CertificateRecord>(pendingBatch);
                        pendingBatch.Clear();
                    }
                }

                if (flush is not null)
                {
                    await _recordRepository.UpsertManyAsync(flush, cancellationToken);
                }
            }

            await Task.WhenAll(parseTasks);
        }
        catch (OperationCanceledException)
        {
            wasCancelled = true;
        }

        if (pendingBatch.Count > 0)
        {
            await _recordRepository.UpsertManyAsync(pendingBatch, CancellationToken.None);
        }

        var duplicateGroups = _duplicateDetectionService.FindDuplicates(allRecords);
        if (duplicateGroups.Count > 0)
        {
            var assignments = new Dictionary<Guid, Guid?>();
            foreach (var group in duplicateGroups)
            {
                foreach (var copy in group.Copies)
                {
                    assignments[copy.Id] = group.GroupId;
                }
            }

            await _recordRepository.UpdateDuplicateGroupsAsync(assignments, CancellationToken.None);
        }

        session.FinishedAtUtc = DateTime.UtcNow;
        session.WasCancelled = wasCancelled;
        session.PfxFound = allRecords.Count;
        session.ExpiredCount = allRecords.Count(r => r.Status == CertificateStatus.Expired);
        session.ErrorCount = errorCount;
        session.FilesChecked = Interlocked.Read(ref lastFilesChecked);
        await _scanSessionRepository.UpdateAsync(session, CancellationToken.None);

        await _auditLogger.LogAsync(
            wasCancelled ? "scan_cancelled" : "scan_completed",
            $"found={session.PfxFound}, expired={session.ExpiredCount}, errors={session.ErrorCount}",
            cancellationToken: CancellationToken.None);

        return session;
    }
}
