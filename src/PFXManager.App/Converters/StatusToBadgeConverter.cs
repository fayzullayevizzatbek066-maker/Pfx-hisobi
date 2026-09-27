using System.Globalization;
using System.Windows.Data;
using PFXManager.App.Resources;
using PFXManager.Core.Enums;

namespace PFXManager.App.Converters;

public sealed class StatusToBadgeConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        CertificateStatus.Active => Strings.StatusBadge_Active,
        CertificateStatus.Expiring => Strings.StatusBadge_Expiring,
        CertificateStatus.ExpiringSoon => Strings.StatusBadge_ExpiringSoon,
        CertificateStatus.Expired => Strings.StatusBadge_Expired,
        CertificateStatus.PasswordRequired => Strings.StatusBadge_PasswordRequired,
        CertificateStatus.ReadError => Strings.StatusBadge_ReadError,
        _ => value?.ToString() ?? string.Empty
    };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
