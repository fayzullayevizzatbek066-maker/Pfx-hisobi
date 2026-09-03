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
    public async Task ScanAsync_SkipsReparsePointsByDefault()
    {
        var realDir = Path.Combine(_root, "real-target");
        TestCertificateFactory.WriteTempPfx(realDir, "via-link.pfx", "ViaLink", DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(30));

        var linkPath = Path.Combine(_root, "link-to-real");
        Directory.CreateSymbolicLink(linkPath, realDir);

        var scanner = new FileSystemScanner(NullLogger<FileSystemScanner>.Instance);
        var options = new ScanOptions(new[] { linkPath }, FollowReparsePoints: false);

        var found = new List<string>();
        await foreach (var file in scanner.ScanAsync(options, progress: null, onError: null, CancellationToken.None))
        {
            found.Add(file);
        }

        Assert.Empty(found);
    }

    [Fact]
    public async Task ScanAsync_FollowsReparsePointsWhenEnabled()
    {
        // Mirrors a common real-world case (e.g. Windows Known Folder redirection sending
        // Desktop/Documents/Downloads to OneDrive via a directory junction): a PFX only
        // reachable by walking through a reparse point must still be found once opted in.
        var realDir = Path.Combine(_root, "real-target");
        TestCertificateFactory.WriteTempPfx(realDir, "via-link.pfx", "ViaLink", DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(30));

        var linkPath = Path.Combine(_root, "link-to-real");
        Directory.CreateSymbolicLink(linkPath, realDir);

        var scanner = new FileSystemScanner(NullLogger<FileSystemScanner>.Instance);
        var options = new ScanOptions(new[] { linkPath }, FollowReparsePoints: true);

        var found = new List<string>();
        await foreach (var file in scanner.ScanAsync(options, progress: null, onError: null, CancellationToken.None))
        {
            found.Add(file);
        }

        Assert.Single(found);
        Assert.EndsWith("via-link.pfx", found[0], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ScanAsync_FollowingReparsePoints_DoesNotInfiniteLoopOnACycle()
    {
        // A symlink that points back at an ancestor directory would recurse forever without
        // cycle protection; the scanner must still terminate and still find the real file.
        var outerDir = Path.Combine(_root, "outer");
        TestCertificateFactory.WriteTempPfx(outerDir, "real.pfx", "Real", DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(30));

        var cycleLink = Path.Combine(outerDir, "loops-back-to-outer");
        Directory.CreateSymbolicLink(cycleLink, outerDir);

        var scanner = new FileSystemScanner(NullLogger<FileSystemScanner>.Instance);
        var options = new ScanOptions(new[] { outerDir }, FollowReparsePoints: true);

        var found = new List<string>();
        var completed = await Task.Run(async () =>
        {
            await foreach (var file in scanner.ScanAsync(options, progress: null, onError: null, CancellationToken.None))
            {
                found.Add(file);
            }

            return true;
        }).WaitAsync(TimeSpan.FromSeconds(10));

        Assert.True(completed);
        Assert.Single(found);
        Assert.EndsWith("real.pfx", found[0], StringComparison.OrdinalIgnoreCase);
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
