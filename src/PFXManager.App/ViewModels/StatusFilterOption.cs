using PFXManager.App.Resources;
using PFXManager.Core.Enums;

namespace PFXManager.App.ViewModels;

public sealed class StatusFilterOption
{
    public string Display { get; }
    public CertificateStatus? Status { get; }
    public bool DuplicatesOnly { get; }

    private StatusFilterOption(string display, CertificateStatus? status, bool duplicatesOnly = false)
    {
        Display = display;
        Status = status;
        DuplicatesOnly = duplicatesOnly;
    }

    public static readonly StatusFilterOption All = new(Strings.FilterAll, null);
    public static readonly StatusFilterOption Active = new(Strings.Active, CertificateStatus.Active);
    public static readonly StatusFilterOption Expired = new(Strings.Expired, CertificateStatus.Expired);
    public static readonly StatusFilterOption ExpiringSoon = new(Strings.ExpiringSoon30, CertificateStatus.ExpiringSoon);
    public static readonly StatusFilterOption Expiring = new(Strings.Expiring90, CertificateStatus.Expiring);
    public static readonly StatusFilterOption PasswordRequired = new(Strings.PasswordRequired, CertificateStatus.PasswordRequired);
    public static readonly StatusFilterOption ReadError = new(Strings.ReadError, CertificateStatus.ReadError);
    public static readonly StatusFilterOption Duplicate = new(Strings.DuplicatesLabel, null, duplicatesOnly: true);

    public static IReadOnlyList<StatusFilterOption> All_Options { get; } = new[]
    {
        All, Active, Expired, ExpiringSoon, Expiring, PasswordRequired, ReadError, Duplicate
    };

    public bool Matches(CertificateRecordViewModel record)
    {
        if (DuplicatesOnly)
        {
            return record.IsDuplicate;
        }

        return Status is null || record.Status == Status;
    }

    public override string ToString() => Display;
}
