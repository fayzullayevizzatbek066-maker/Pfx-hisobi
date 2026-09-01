using Microsoft.Extensions.Logging.Abstractions;
using PFXManager.Core.Enums;
using PFXManager.Core.Interfaces;
using PFXManager.Core.Models;
using PFXManager.Infrastructure.FileSystem;
using PFXManager.Tests.TestSupport;
using Xunit;

namespace PFXManager.Tests;

public class PathHandlingTests : IDisposable
{
    private readonly string _root = TestCertificateFactory.CreateUniqueTempDirectory();

    [Fact]
    public async Task ScanAsync_NormalPath_FindsPfxAndP12CaseInsensitively()
    {
        TestCertificateFactory.WriteTempPfx(_root, "one.pfx", "One", DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(30));
        TestCertificateFactory.WriteTempPfx(Path.Combine(_root, "sub"), "TWO.P12", "Two", DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(30));
        await File.WriteAllTextAsync(Path.Combine(_root, "notes.txt"), "not a certificate");

        var scanner = new FileSystemScanner(NullLogger<FileSystemScanner>.Instance);
        var options = new ScanOptions(new[] { _root });

        var found = new List<string>();
        await foreach (var file in scanner.ScanAsync(options, progress: null, onError: null, CancellationToken.None))
        {
            found.Add(file);
        }

        Assert.Equal(2, found.Count);
        Assert.Contains(found, f => f.EndsWith("one.pfx", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(found, f => f.EndsWith("TWO.P12", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ScanAsync_MissingRootDirectory_ReportsErrorAndDoesNotThrow()
    {
        var missingRoot = Path.Combine(_root, "does-not-exist");
        var scanner = new FileSystemScanner(NullLogger<FileSystemScanner>.Instance);
        var options = new ScanOptions(new[] { missingRoot });

        var errors = new List<ScanError>();
        var found = new List<string>();

        await foreach (var file in scanner.ScanAsync(options, progress: null, onError: errors.Add, CancellationToken.None))
        {
            found.Add(file);
        }

        Assert.Empty(found);
        Assert.Contains(errors, e => e.Kind == ScanErrorKind.DirectoryNotFound);
    }

    [Fact]
    public async Task ScanAsync_OneBadRootAmongMany_StillScansTheGoodOnes()
    {
        var goodRoot = Path.Combine(_root, "good");
        TestCertificateFactory.WriteTempPfx(goodRoot, "ok.pfx", "Ok", DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(30));
        var badRoot = Path.Combine(_root, "missing");

        var scanner = new FileSystemScanner(NullLogger<FileSystemScanner>.Instance);
        var options = new ScanOptions(new[] { badRoot, goodRoot });

        var found = new List<string>();
        await foreach (var file in scanner.ScanAsync(options, progress: null, onError: null, CancellationToken.None))
        {
            found.Add(file);
        }

        Assert.Single(found);
    }

    [Fact]
    public async Task ScanAsync_DeeplyNestedLongPath_FindsFileWithoutThrowing()
    {
        var current = _root;
        for (var i = 0; i < 25; i++)
        {
            current = Path.Combine(current, $"nested-folder-segment-{i:000}");
        }

        var path = TestCertificateFactory.WriteTempPfx(current, "deep.pfx", "Deep", DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(30));
        Assert.True(path.Length > 260);

        var scanner = new FileSystemScanner(NullLogger<FileSystemScanner>.Instance);
        var options = new ScanOptions(new[] { _root });

        var found = new List<string>();
        await foreach (var file in scanner.ScanAsync(options, progress: null, onError: null, CancellationToken.None))
        {
            found.Add(file);
        }

        Assert.Single(found);
    }

    [Fact]
    public async Task ScanAsync_CanBeCancelled()
    {
        for (var i = 0; i < 20; i++)
        {
            TestCertificateFactory.WriteTempPfx(Path.Combine(_root, $"dir{i}"), $"cert{i}.pfx", $"Cert{i}", DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(30));
        }

        var scanner = new FileSystemScanner(NullLogger<FileSystemScanner>.Instance);
        var options = new ScanOptions(new[] { _root });
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (var _ in scanner.ScanAsync(options, progress: null, onError: null, cts.Token))
            {
            }
        });
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); } catch { /* best effort cleanup */ }
    }
}
