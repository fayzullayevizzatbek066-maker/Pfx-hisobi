using PFXManager.Core.Enums;
using PFXManager.Core.Models;
using PFXManager.Core.Services;
using Xunit;

namespace PFXManager.Tests;

public class BulkSelectionTests
{
    private static CertificateRecord Make(CertificateStatus status, DateTime? notAfter = null) => new()
    {
        FullPath = $@"C:\certs\{Guid.NewGuid():N}.pfx",
        Status = status,
        NotAfter = notAfter ?? (status == CertificateStatus.Expired ? DateTime.UtcNow.AddDays(-5) : DateTime.UtcNow.AddDays(200))
    };

    [Fact]
    public void SelectAllExpired_OnlySelectsVerifiedExpiredRecords()
    {
        var service = new BulkSelectionService();
        var records = new[]
        {
            Make(CertificateStatus.Expired),
            Make(CertificateStatus.Expired),
            Make(CertificateStatus.Active),
            Make(CertificateStatus.ExpiringSoon),
            Make(CertificateStatus.Expiring),
        };

        var selected = service.SelectAllExpired(records);

        Assert.Equal(2, selected.Count);
        Assert.All(selected, r => Assert.Equal(CertificateStatus.Expired, r.Status));
    }

    [Fact]
    public void SelectAllExpired_NeverIncludesPasswordRequired()
    {
        var service = new BulkSelectionService();
        var records = new[]
        {
            Make(CertificateStatus.Expired),
            Make(CertificateStatus.PasswordRequired, notAfter: null),
        };

        var selected = service.SelectAllExpired(records);

        Assert.DoesNotContain(selected, r => r.Status == CertificateStatus.PasswordRequired);
    }

    [Fact]
    public void SelectAllExpired_NeverIncludesReadError()
    {
        var service = new BulkSelectionService();
        var records = new[]
        {
            Make(CertificateStatus.Expired),
            Make(CertificateStatus.ReadError, notAfter: null),
        };

        var selected = service.SelectAllExpired(records);

        Assert.DoesNotContain(selected, r => r.Status == CertificateStatus.ReadError);
    }

    [Fact]
    public void SelectAllExpired_ReturnsEmpty_WhenNoneExpired()
    {
        var service = new BulkSelectionService();
        var records = new[] { Make(CertificateStatus.Active), Make(CertificateStatus.ExpiringSoon) };

        var selected = service.SelectAllExpired(records);

        Assert.Empty(selected);
    }
}
