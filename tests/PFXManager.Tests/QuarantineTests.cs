using Microsoft.Extensions.DependencyInjection;
using PFXManager.Core.Enums;
using PFXManager.Core.Interfaces;
using PFXManager.Core.Models;
using PFXManager.Infrastructure.DependencyInjection;
using PFXManager.Infrastructure.Persistence;
using PFXManager.Tests.TestSupport;
using Xunit;

namespace PFXManager.Tests;

/// <summary>
/// Integration-style tests against a real (temp-file) SQLite database and real file moves under
/// the OS temp directory — never against any real user PFX file (rule 38).
/// </summary>
public class QuarantineTests : IDisposable
{
    private readonly string _workDirectory = TestCertificateFactory.CreateUniqueTempDirectory();
    private readonly ServiceProvider _provider;

    public QuarantineTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddPfxManagerInfrastructure(Path.Combine(_workDirectory, "test.db"));
        _provider = services.BuildServiceProvider();
        _provider.GetRequiredService<DatabaseMigrator>().Migrate();
    }

    private async Task<string> ConfigureQuarantinePathAsync()
    {
        var quarantineDir = Path.Combine(_workDirectory, "Quarantine");
        var settingsRepo = _provider.GetRequiredService<IAppSettingsRepository>();
        var settings = await settingsRepo.LoadAsync(CancellationToken.None);
        settings.QuarantinePath = quarantineDir;
        await settingsRepo.SaveAsync(settings, CancellationToken.None);
        return quarantineDir;
    }

    private CertificateRecord MakeExpiredRecord(string filePath) => new()
    {
        FullPath = filePath,
        Status = CertificateStatus.Expired,
        Thumbprint = "TESTTHUMB",
        NotAfter = DateTime.UtcNow.AddDays(-5)
    };

    [Fact]
    public async Task Quarantine_MovesFileAndRecordsMetadata()
    {
        await ConfigureQuarantinePathAsync();
        var sourcePath = TestCertificateFactory.WriteTempPfx(_workDirectory, "expired.pfx", "Expired", DateTimeOffset.UtcNow.AddYears(-2), DateTimeOffset.UtcNow.AddDays(-5));
        var record = MakeExpiredRecord(sourcePath);
        var recordRepo = _provider.GetRequiredService<ICertificateRecordRepository>();
        await recordRepo.UpsertManyAsync(new[] { record }, CancellationToken.None);

        var quarantineService = _provider.GetRequiredService<IQuarantineService>();
        var results = await quarantineService.QuarantineAsync(new[] { record }, CancellationToken.None);

        Assert.True(results.Single().Success, results.Single().ErrorMessage);
        Assert.False(File.Exists(sourcePath));
        Assert.Null(await recordRepo.GetByIdAsync(record.Id, CancellationToken.None));

        var quarantineRepo = _provider.GetRequiredService<IQuarantineRepository>();
        var active = await quarantineRepo.GetActiveAsync(CancellationToken.None);
        var item = Assert.Single(active);
        Assert.Equal(sourcePath, item.OriginalPath);
        Assert.True(File.Exists(item.QuarantinePath));
    }

    [Fact]
    public async Task Restore_MovesFileBackAndReindexesIt()
    {
        await ConfigureQuarantinePathAsync();
        var sourcePath = TestCertificateFactory.WriteTempPfx(_workDirectory, "restore-me.pfx", "RestoreMe", DateTimeOffset.UtcNow.AddYears(-2), DateTimeOffset.UtcNow.AddDays(-5));
        var record = MakeExpiredRecord(sourcePath);
        var recordRepo = _provider.GetRequiredService<ICertificateRecordRepository>();
        await recordRepo.UpsertManyAsync(new[] { record }, CancellationToken.None);

        var quarantineService = _provider.GetRequiredService<IQuarantineService>();
        await quarantineService.QuarantineAsync(new[] { record }, CancellationToken.None);

        var quarantineRepo = _provider.GetRequiredService<IQuarantineRepository>();
        var item = (await quarantineRepo.GetActiveAsync(CancellationToken.None)).Single();

        var restoreResult = await quarantineService.RestoreAsync(item.Id, new RestoreOptions(RestoreConflictAction.Cancel), CancellationToken.None);

        Assert.True(restoreResult.Success);
        Assert.True(File.Exists(sourcePath));
        Assert.Empty(await quarantineRepo.GetActiveAsync(CancellationToken.None));

        var allRecords = await recordRepo.GetAllAsync(CancellationToken.None);
        Assert.Contains(allRecords, r => r.FullPath == sourcePath);
    }

    [Fact]
    public async Task Restore_FilenameConflict_CancelLeavesQuarantineIntact()
    {
        await ConfigureQuarantinePathAsync();
        var sourcePath = TestCertificateFactory.WriteTempPfx(_workDirectory, "conflict.pfx", "Conflict", DateTimeOffset.UtcNow.AddYears(-2), DateTimeOffset.UtcNow.AddDays(-5));
        var record = MakeExpiredRecord(sourcePath);
        var recordRepo = _provider.GetRequiredService<ICertificateRecordRepository>();
        await recordRepo.UpsertManyAsync(new[] { record }, CancellationToken.None);

        var quarantineService = _provider.GetRequiredService<IQuarantineService>();
        await quarantineService.QuarantineAsync(new[] { record }, CancellationToken.None);

        // Simulate a new, unrelated file appearing at the original path after quarantine.
        await File.WriteAllTextAsync(sourcePath, "a different file now lives here");

        var quarantineRepo = _provider.GetRequiredService<IQuarantineRepository>();
        var item = (await quarantineRepo.GetActiveAsync(CancellationToken.None)).Single();

        var cancelResult = await quarantineService.RestoreAsync(item.Id, new RestoreOptions(RestoreConflictAction.Cancel), CancellationToken.None);
        Assert.False(cancelResult.Success);
        Assert.True(cancelResult.HadConflict);
        Assert.True(File.Exists(item.QuarantinePath)); // still in quarantine, not silently overwritten
        Assert.Single(await quarantineRepo.GetActiveAsync(CancellationToken.None));

        var renameResult = await quarantineService.RestoreAsync(item.Id, new RestoreOptions(RestoreConflictAction.RenameNew), CancellationToken.None);
        Assert.True(renameResult.Success);
        Assert.NotEqual(sourcePath, renameResult.RestoredPath);
        Assert.True(File.Exists(sourcePath)); // the unrelated file is untouched
        Assert.True(File.Exists(renameResult.RestoredPath!));
    }

    [Fact]
    public async Task Quarantine_FailedMove_WhenSourceFileAlreadyGone()
    {
        await ConfigureQuarantinePathAsync();
        var missingPath = Path.Combine(_workDirectory, "already-gone.pfx");
        var record = MakeExpiredRecord(missingPath);

        var quarantineService = _provider.GetRequiredService<IQuarantineService>();
        var results = await quarantineService.QuarantineAsync(new[] { record }, CancellationToken.None);

        var result = Assert.Single(results);
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
    }

    public void Dispose()
    {
        _provider.Dispose();
        try { Directory.Delete(_workDirectory, recursive: true); } catch { /* best effort cleanup */ }
    }
}
