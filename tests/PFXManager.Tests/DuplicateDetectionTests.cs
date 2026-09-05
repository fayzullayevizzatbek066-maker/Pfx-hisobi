using PFXManager.Core.Enums;
using PFXManager.Core.Models;
using PFXManager.Core.Services;
using Xunit;

namespace PFXManager.Tests;

public class DuplicateDetectionTests
{
    private static CertificateRecord MakeRecord(string path, string? thumbprint, CertificateStatus status = CertificateStatus.Active) => new()
    {
        FullPath = path,
        Thumbprint = thumbprint,
        Status = status
    };

    [Fact]
    public void GroupsRecordsBySameThumbprint()
    {
        var service = new DuplicateDetectionService();
        var records = new[]
        {
            MakeRecord(@"C:\Users\User\Desktop\key.pfx", "ABC123"),
            MakeRecord(@"D:\Backup\key-copy.pfx", "ABC123"),
            MakeRecord(@"E:\Archive\2025\certificate.pfx", "ABC123"),
        };

        var groups = service.FindDuplicates(records);

        var group = Assert.Single(groups);
        Assert.Equal(3, group.CopyCount);
        Assert.Equal("ABC123", group.Thumbprint);
    }

    [Fact]
    public void DifferentFileNameSameCertificate_IsStillOneGroup()
    {
        var service = new DuplicateDetectionService();
        var records = new[]
        {
            MakeRecord(@"C:\a\old-name.pfx", "SAMEPRINT"),
            MakeRecord(@"C:\b\totally-different-name.p12", "SAMEPRINT"),
        };

        var groups = service.FindDuplicates(records);

        Assert.Single(groups);
        Assert.Equal(2, groups[0].CopyCount);
    }

    [Fact]
    public void SameFileNameDifferentCertificate_IsNotGrouped()
    {
        var service = new DuplicateDetectionService();
        var records = new[]
        {
            MakeRecord(@"C:\a\key.pfx", "THUMB-ONE"),
            MakeRecord(@"C:\b\key.pfx", "THUMB-TWO"),
        };

        var groups = service.FindDuplicates(records);

        Assert.Empty(groups);
    }

    [Fact]
    public void PasswordRequiredOrReadError_NeverGrouped()
    {
        var service = new DuplicateDetectionService();
        var records = new[]
        {
            MakeRecord(@"C:\a\locked1.pfx", null, CertificateStatus.PasswordRequired),
            MakeRecord(@"C:\b\locked2.pfx", null, CertificateStatus.ReadError),
        };

        var groups = service.FindDuplicates(records);

        Assert.Empty(groups);
    }

    [Fact]
    public void SingleCopy_IsNotADuplicateGroup()
    {
        var service = new DuplicateDetectionService();
        var records = new[] { MakeRecord(@"C:\a\key.pfx", "ONLYONE") };

        var groups = service.FindDuplicates(records);

        Assert.Empty(groups);
    }
}
