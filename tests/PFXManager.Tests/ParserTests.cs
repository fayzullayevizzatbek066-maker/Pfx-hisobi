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

    [Theory]
    [InlineData("Сетевой пароль указан неверно.")] // Russian-locale Windows CAPI message for the same failure
    [InlineData("The specified network password is not correct.")] // English-locale equivalent
    public void IsPasswordFailure_RecognizesWrongPasswordByHResult_RegardlessOfMessageLanguage(string localizedMessage)
    {
        // The real-world bug this guards: a wrong-password PKCS12 failure on non-English Windows
        // reports via this exact HResult (HRESULT_FROM_WIN32(ERROR_INVALID_PASSWORD)) with a
        // message in the OS UI language - matching only on English substrings like "password"
        // silently misclassified it as a generic ReadError instead of PasswordRequired.
        var ex = new System.Security.Cryptography.CryptographicException(localizedMessage);
        ex.HResult = unchecked((int)0x80070056);

        Assert.True(X509CertificateParser.IsPasswordFailure(ex));
    }

    [Fact]
    public void IsPasswordFailure_RecognizesBouncyCastleWrongPasswordSignal_EvenWithUnrecognizedHResultAndMessage()
    {
        var ex = new System.Security.Cryptography.CryptographicException("some opaque native error text");

        Assert.True(X509CertificateParser.IsPasswordFailure(
            ex, bouncyCastleErrorDetail: "IOException: PKCS12 key store MAC invalid - wrong password or corrupted file."));
    }

    [Fact]
    public void BouncyCastlePfxReader_ReadsStandardPfx_WithThumbprintMatchingDotNet()
    {
        var notBefore = DateTimeOffset.UtcNow.AddDays(-1);
        var notAfter = DateTimeOffset.UtcNow.AddDays(365);
        var path = TestCertificateFactory.WriteTempPfx(_tempDirectory, "bc-readable.pfx", "BouncyCastle Reader", notBefore, notAfter);

        var dotNetCert = new System.Security.Cryptography.X509Certificates.X509Certificate2(
            path, string.Empty,
            System.Security.Cryptography.X509Certificates.X509KeyStorageFlags.EphemeralKeySet);

        var success = PFXManager.Infrastructure.Certificates.BouncyCastlePfxReader.TryRead(
            path, string.Empty, out var result, out var errorDetail);

        Assert.True(success);
        Assert.Null(errorDetail);
        Assert.NotNull(result);
        Assert.True(result!.Success);
        Assert.Equal("BouncyCastle Reader", result.CommonName);
        Assert.True(result.HasPrivateKey);
        Assert.NotNull(result.Thumbprint);
        Assert.Equal(dotNetCert.Thumbprint, result.Thumbprint, ignoreCase: true);
        Assert.Equal(dotNetCert.SerialNumber, result.SerialNumber, ignoreCase: true);
    }

    [Fact]
    public async Task ParseAsync_FallsBackToBouncyCastle_WhenPrimaryParseFails()
    {
        // A file that isn't a valid PKCS12 at all should still fail cleanly through both readers
        // rather than throwing - this exercises the fallback code path in X509CertificateParser
        // (BouncyCastle attempted, fails too, original failure classification is returned).
        var path = Path.Combine(_tempDirectory, "not-a-pfx-at-all.pfx");
        await File.WriteAllTextAsync(path, "this is definitely not a PKCS12 file");

        var parser = new X509CertificateParser(NullLogger<X509CertificateParser>.Instance);
        var result = await parser.ParseAsync(path, null, CancellationToken.None);

        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDirectory, recursive: true); } catch { /* best effort cleanup */ }
    }
}
