using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace PFXManager.Tests.TestSupport;

/// <summary>
/// Generates throwaway, self-signed test certificates entirely in-memory. Tests must never read,
/// move, or delete a real user PFX file — everything here is synthetic and lives under the OS
/// temp directory inside a per-test unique folder.
/// </summary>
internal static class TestCertificateFactory
{
    public static byte[] CreatePfxBytes(string subjectCn, DateTimeOffset notBefore, DateTimeOffset notAfter, string? password = null)
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            $"CN={subjectCn}, O=Test Org, OU=QA",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        using var certificate = request.CreateSelfSigned(notBefore, notAfter);
        return certificate.Export(X509ContentType.Pfx, password);
    }

    public static string WriteTempPfx(string directory, string fileName, string subjectCn, DateTimeOffset notBefore, DateTimeOffset notAfter, string? password = null)
    {
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, fileName);
        File.WriteAllBytes(path, CreatePfxBytes(subjectCn, notBefore, notAfter, password));
        return path;
    }

    public static string CreateUniqueTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "PFXManagerTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
