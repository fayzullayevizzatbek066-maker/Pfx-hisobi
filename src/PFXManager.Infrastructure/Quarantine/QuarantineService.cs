using Microsoft.Extensions.Logging;
using PFXManager.Core.Enums;
using PFXManager.Core.Interfaces;
using PFXManager.Core.Models;

namespace PFXManager.Infrastructure.Quarantine;

/// <summary>
/// Moves discovered PFX files into a per-session quarantine folder and can restore or
/// permanently delete them later. Verifies file existence immediately before every move
/// (reducing, never fully eliminating, TOCTOU risk per section 35/36) and only marks a
/// database record as moved/restored/deleted after the corresponding file system operation
/// has actually succeeded, so the database and disk never disagree about outcome.
/// </summary>
public sealed class QuarantineService : IQuarantineService
{
    private readonly IAppSettingsRepository _settingsRepository;
    private readonly ICertificateRecordRepository _recordRepository;
    private readonly IQuarantineRepository _quarantineRepository;
    private readonly ICertificateRecordFactory _recordFactory;
    private readonly IAuditLogger _auditLogger;
    private readonly ILogger<QuarantineService> _logger;

    public QuarantineService(
        IAppSettingsRepository settingsRepository,
        ICertificateRecordRepository recordRepository,
        IQuarantineRepository quarantineRepository,
        ICertificateRecordFactory recordFactory,
        IAuditLogger auditLogger,
        ILogger<QuarantineService> logger)
    {
        _settingsRepository = settingsRepository;
        _recordRepository = recordRepository;
        _quarantineRepository = quarantineRepository;
        _recordFactory = recordFactory;
        _auditLogger = auditLogger;
        _logger = logger;
    }

    public async Task<IReadOnlyList<QuarantineResult>> QuarantineAsync(
        IReadOnlyList<CertificateRecord> records,
        CancellationToken cancellationToken)
    {
        var settings = await _settingsRepository.LoadAsync(cancellationToken);
        var sessionFolder = Path.Combine(settings.QuarantinePath, DateTime.Now.ToString("yyyy-MM-dd_HHmmss"));
        Directory.CreateDirectory(sessionFolder);

        var results = new List<QuarantineResult>(records.Count);

        foreach (var record in records)
        {
            var result = await QuarantineOneAsync(record, sessionFolder, cancellationToken);
            results.Add(result);
        }

        await _auditLogger.LogAsync(
            "quarantine",
            $"requested={records.Count}, succeeded={results.Count(r => r.Success)}, folder={sessionFolder}",
            cancellationToken: cancellationToken);

        return results;
    }

    private async Task<QuarantineResult> QuarantineOneAsync(CertificateRecord record, string sessionFolder, CancellationToken cancellationToken)
    {
        try
        {
            // Re-verify existence immediately before the move: the file may have been deleted,
            // renamed, or already quarantined by a previous operation since it was listed.
            if (!File.Exists(record.FullPath))
            {
                return new QuarantineResult(record.Id, false, "Fayl topilmadi (allaqachon ko'chirilgan yoki o'chirilgan bo'lishi mumkin).");
            }

            var destinationPath = GetUniqueDestinationPath(sessionFolder, Path.GetFileName(record.FullPath));

            DateTime originalLastWriteUtc;
            long originalFileSize;
            try
            {
                // Capture metadata before the move: FileInfo properties are read lazily, and
                // reading them after the file has moved would stat a path that no longer exists.
                var sourceInfo = new FileInfo(record.FullPath);
                originalLastWriteUtc = sourceInfo.LastWriteTimeUtc;
                originalFileSize = sourceInfo.Length;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                return new QuarantineResult(record.Id, false, ex.Message);
            }

            File.Move(record.FullPath, destinationPath);

            var item = new QuarantineItem
            {
                CertificateRecordId = record.Id,
                OriginalPath = record.FullPath,
                QuarantinePath = destinationPath,
                FileName = Path.GetFileName(record.FullPath),
                Thumbprint = record.Thumbprint,
                SerialNumber = record.SerialNumber,
                NotAfter = record.NotAfter,
                OriginalLastModifiedUtc = originalLastWriteUtc,
                FileSizeBytes = originalFileSize,
                Reason = record.Status == CertificateStatus.Expired
                    ? "Muddati o'tgan sertifikat"
                    : "Foydalanuvchi tomonidan karantinga o'tkazildi"
            };
            await _quarantineRepository.AddAsync(item, cancellationToken);

            // The file no longer exists at its scanned location; drop the stale PFX-files row.
            await _recordRepository.DeleteAsync(record.Id, cancellationToken);

            return new QuarantineResult(record.Id, true, null);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(ex, "Failed to quarantine {Path}", record.FullPath);
            return new QuarantineResult(record.Id, false, ex.Message);
        }
    }

    private static string GetUniqueDestinationPath(string folder, string fileName)
    {
        var candidate = Path.Combine(folder, fileName);
        if (!File.Exists(candidate))
        {
            return candidate;
        }

        var nameOnly = Path.GetFileNameWithoutExtension(fileName);
        var extension = Path.GetExtension(fileName);
        for (var i = 1; ; i++)
        {
            candidate = Path.Combine(folder, $"{nameOnly} ({i}){extension}");
            if (!File.Exists(candidate))
            {
                return candidate;
            }
        }
    }

    public async Task<RestoreResult> RestoreAsync(Guid quarantineItemId, RestoreOptions options, CancellationToken cancellationToken)
    {
        var item = await _quarantineRepository.GetByIdAsync(quarantineItemId, cancellationToken);
        if (item is null || item.IsResolved)
        {
            return new RestoreResult(quarantineItemId, false, "Karantin elementi topilmadi.", null, false);
        }

        if (!File.Exists(item.QuarantinePath))
        {
            return new RestoreResult(quarantineItemId, false, "Karantindagi fayl topilmadi.", null, false);
        }

        var destination = item.OriginalPath;
        var hadConflict = File.Exists(destination);

        if (hadConflict)
        {
            switch (options.ConflictAction)
            {
                case RestoreConflictAction.Cancel:
                    return new RestoreResult(quarantineItemId, false, null, null, true);

                case RestoreConflictAction.RenameNew:
                    destination = GetUniqueDestinationPath(Path.GetDirectoryName(item.OriginalPath)!, Path.GetFileName(item.OriginalPath));
                    break;

                case RestoreConflictAction.ChooseDestination:
                    if (string.IsNullOrWhiteSpace(options.ExplicitDestinationPath))
                    {
                        return new RestoreResult(quarantineItemId, false, "Manzil tanlanmagan.", null, true);
                    }

                    destination = options.ExplicitDestinationPath;
                    if (File.Exists(destination))
                    {
                        return new RestoreResult(quarantineItemId, false, "Tanlangan manzilda ham fayl mavjud.", null, true);
                    }

                    break;
            }
        }

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Move(item.QuarantinePath, destination);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(ex, "Failed to restore {Path}", item.QuarantinePath);
            return new RestoreResult(quarantineItemId, false, ex.Message, null, hadConflict);
        }

        item.IsResolved = true;
        item.ResolutionKind = "Restored";
        item.ResolvedAtUtc = DateTime.UtcNow;
        await _quarantineRepository.UpdateAsync(item, cancellationToken);

        try
        {
            var record = await _recordFactory.BuildAsync(destination, password: null, Guid.Empty, cancellationToken);
            await _recordRepository.UpsertManyAsync(new[] { record }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Restored {Path} but could not re-index it; a rescan will pick it up.", destination);
        }

        await _auditLogger.LogAsync("restore", destination, cancellationToken: cancellationToken);

        return new RestoreResult(quarantineItemId, true, null, destination, hadConflict);
    }

    public async Task<bool> PermanentlyDeleteAsync(Guid quarantineItemId, CancellationToken cancellationToken)
    {
        var item = await _quarantineRepository.GetByIdAsync(quarantineItemId, cancellationToken);
        if (item is null || item.IsResolved)
        {
            return false;
        }

        try
        {
            if (File.Exists(item.QuarantinePath))
            {
                File.Delete(item.QuarantinePath);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(ex, "Failed to permanently delete {Path}", item.QuarantinePath);
            await _auditLogger.LogAsync("permanent_delete", item.QuarantinePath, success: false, cancellationToken: cancellationToken);
            return false;
        }

        item.IsResolved = true;
        item.ResolutionKind = "Deleted";
        item.ResolvedAtUtc = DateTime.UtcNow;
        await _quarantineRepository.UpdateAsync(item, cancellationToken);

        await _auditLogger.LogAsync("permanent_delete", item.FileName, cancellationToken: cancellationToken);
        return true;
    }
}
