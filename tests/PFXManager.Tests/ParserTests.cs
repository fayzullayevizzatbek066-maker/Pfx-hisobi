using Microsoft.Extensions.Logging.Abstractions;
using PFXManager.Infrastructure.Certificates;
using PFXManager.Tests.TestSupport;
using Xunit;

namespace PFXManager.Tests;

public class ParserTests : IDisposable
{
    private readonly string _tempDirectory = TestCertificateFactory.CreateUniqueTempDirectory();

    [Fact]
    public async Task ParseAsync_ReadableCertificate_ReturnsExpectedMetadata()
    {
        var notBefore = DateTimeOffset.UtcNow.AddDays(-1);
        var notAfter = DateTimeOffset.UtcNow.AddDays(365);
        var path = TestCertificateFactory.WriteTempPfx(_tempDirectory, "readable.pfx", "Test Reader", notBefore, notAfter);

        var parser = new X509CertificateParser(NullLogger<X509CertificateParser>.Instance);
        var result = await parser.ParseAsync(path, null, CancellationToken.None);

        Assert.True(result.Success);
        Assert.False(result.PasswordRequired);
        Assert.Contains("Test Reader", result.Subject);
        Assert.Equal("Test Reader", result.CommonName);
        Assert.NotNull(result.Thumbprint);
        Assert.NotNull(result.SerialNumber);
        Assert.True(result.HasPrivateKey);
    }

    [Fact]
    public async Task ParseAsync_PasswordProtected_ReturnsPasswordRequiredWithoutThrowing()
    {
        var notBefore = DateTimeOffset.UtcNow.AddDays(-1);
        var notAfter = DateTimeOffset.UtcNow.AddDays(365);
        var path = TestCertificateFactory.WriteTempPfx(_tempDirectory, "locked.pfx", "Locked Cert", notBefore, notAfter, password: "S3cret!");

        var parser = new X509CertificateParser(NullLogger<X509CertificateParser>.Instance);
        var result = await parser.ParseAsync(path, null, CancellationToken.None);

        Assert.False(result.Success);
        Assert.True(result.PasswordRequired);
    }

    [Fact]
    public async Task ParseAsync_PasswordProtected_SucceedsWhenCorrectPasswordSupplied()
    {
        var notBefore = DateTimeOffset.UtcNow.AddDays(-1);
        var notAfter = DateTimeOffset.UtcNow.AddDays(365);
        var path = TestCertificateFactory.WriteTempPfx(_tempDirectory, "locked2.pfx", "Locked Cert 2", notBefore, notAfter, password: "S3cret!");

        var parser = new X509CertificateParser(NullLogger<X509CertificateParser>.Instance);
        var result = await parser.ParseAsync(path, "S3cret!", CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal("Locked Cert 2", result.CommonName);
    }

    [Fact]
    public async Task ParseAsync_CorruptedFile_ReturnsFailureWithoutThrowing()
    {
        var path = Path.Combine(_tempDirectory, "corrupted.pfx");
        await File.WriteAllBytesAsync(path, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });

        var parser = new X509CertificateParser(NullLogger<X509CertificateParser>.Instance);
        var result = await parser.ParseAsync(path, null, CancellationToken.None);

        Assert.False(result.Success);
        Assert.False(result.PasswordRequired);
        Assert.NotNull(result.ErrorMessage);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDirectory, recursive: true); } catch { /* best effort cleanup */ }
    }
}
