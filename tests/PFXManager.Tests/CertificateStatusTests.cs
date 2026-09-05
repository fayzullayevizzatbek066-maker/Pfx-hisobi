using PFXManager.Core.Enums;
using PFXManager.Core.Services;
using Xunit;

namespace PFXManager.Tests;

public class CertificateStatusTests
{
    private static CertificateStatusEngine CreateEngine(DateTime fixedUtcNow)
    {
        var timeProvider = new FakeTimeProvider(fixedUtcNow);
        return new CertificateStatusEngine(timeProvider);
    }

    private sealed class FakeTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _now;
        public FakeTimeProvider(DateTime utcNow) => _now = new DateTimeOffset(utcNow, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => _now;
    }

    [Fact]
    public void Expired_WhenNotAfterInThePast()
    {
        var now = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        var engine = CreateEngine(now);

        var status = engine.DetermineStatus(now.AddDays(-1), passwordRequired: false, readError: false);

        Assert.Equal(CertificateStatus.Expired, status);
    }

    [Fact]
    public void Active_WhenMoreThan90DaysRemain()
    {
        var now = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        var engine = CreateEngine(now);

        var status = engine.DetermineStatus(now.AddDays(91), passwordRequired: false, readError: false);

        Assert.Equal(CertificateStatus.Active, status);
    }

    [Fact]
    public void ExpiringSoon_WhenExpiresToday()
    {
        var now = new DateTime(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc);
        var engine = CreateEngine(now);

        // Expires later the same day (a few hours from "now").
        var status = engine.DetermineStatus(now.AddHours(4), passwordRequired: false, readError: false);

        Assert.Equal(CertificateStatus.ExpiringSoon, status);
    }

    [Fact]
    public void ExpiringSoon_WhenExpiresWithin30Days()
    {
        var now = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        var engine = CreateEngine(now);

        var status = engine.DetermineStatus(now.AddDays(30), passwordRequired: false, readError: false);

        Assert.Equal(CertificateStatus.ExpiringSoon, status);
    }

    [Fact]
    public void Expiring_WhenExpiresIn31Days()
    {
        var now = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        var engine = CreateEngine(now);

        var status = engine.DetermineStatus(now.AddDays(31), passwordRequired: false, readError: false);

        Assert.Equal(CertificateStatus.Expiring, status);
    }

    [Fact]
    public void Expiring_WhenExpiresIn90Days()
    {
        var now = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        var engine = CreateEngine(now);

        var status = engine.DetermineStatus(now.AddDays(90), passwordRequired: false, readError: false);

        Assert.Equal(CertificateStatus.Expiring, status);
    }

    [Fact]
    public void PasswordRequired_TakesPrecedenceOverEverything()
    {
        var now = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        var engine = CreateEngine(now);

        var status = engine.DetermineStatus(now.AddDays(-10), passwordRequired: true, readError: false);

        Assert.Equal(CertificateStatus.PasswordRequired, status);
    }

    [Fact]
    public void ReadError_WhenParsingFailed()
    {
        var now = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        var engine = CreateEngine(now);

        var status = engine.DetermineStatus(null, passwordRequired: false, readError: true);

        Assert.Equal(CertificateStatus.ReadError, status);
    }

    [Fact]
    public void ReadError_WhenNotAfterMissingEvenIfNotFlaggedAsError()
    {
        var now = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        var engine = CreateEngine(now);

        var status = engine.DetermineStatus(null, passwordRequired: false, readError: false);

        Assert.Equal(CertificateStatus.ReadError, status);
    }
}
