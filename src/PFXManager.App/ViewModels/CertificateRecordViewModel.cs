using CommunityToolkit.Mvvm.ComponentModel;
using PFXManager.App.Resources;
using PFXManager.Core.Enums;
using PFXManager.Core.Models;

namespace PFXManager.App.ViewModels;

/// <summary>Thin, bindable wrapper around a <see cref="CertificateRecord"/> for the PFX Files grid.</summary>
public sealed partial class CertificateRecordViewModel : ObservableObject
{
    public CertificateRecordViewModel(CertificateRecord record)
    {
        Record = record;
    }

    public CertificateRecord Record { get; private set; }

    /// <summary>Swaps in a freshly parsed record (e.g. after a password was supplied) and refreshes every bound display property.</summary>
    public void UpdateRecord(CertificateRecord record)
    {
        Record = record;
        OnPropertyChanged(string.Empty);
    }

    [ObservableProperty]
    private bool _isSelected;

    public Guid Id => Record.Id;
    public CertificateStatus Status => Record.Status;
    public string StatusBadge => Status switch
    {
        CertificateStatus.Active => Strings.StatusBadge_Active,
        CertificateStatus.Expiring => Strings.StatusBadge_Expiring,
        CertificateStatus.ExpiringSoon => Strings.StatusBadge_ExpiringSoon,
        CertificateStatus.Expired => Strings.StatusBadge_Expired,
        CertificateStatus.PasswordRequired => Strings.StatusBadge_PasswordRequired,
        CertificateStatus.ReadError => Strings.StatusBadge_ReadError,
        _ => Status.ToString()
    };

    public string OwnerDisplay => Record.OwnerDisplayName ?? Record.Organization ?? Record.CommonName ?? "—";
    public string StirDisplay => Record.Stir ?? Record.Pinfl ?? "—";
    public string? SerialNumber => Record.SerialNumber;
    public string? Thumbprint => Record.Thumbprint;
    public DateTime? NotBefore => Record.NotBefore;
    public DateTime? NotAfter => Record.NotAfter;
    public int? RemainingDays => Record.RemainingDays;
    public string FileName => Record.FileName;
    public string FullPath => Record.FullPath;
    public string Drive => Record.Drive;
    public bool IsDuplicate => Record.DuplicateGroupId is not null;

    /// <summary>Raw parser failure text (never null for a Success record) - shown so a ReadError can be diagnosed instead of guessed at.</summary>
    public string? ReadErrorDetail => Record.ReadErrorMessage;

    public string SizeDisplay
    {
        get
        {
            double size = Record.FileSizeBytes;
            string[] units = { "B", "KB", "MB", "GB" };
            var unitIndex = 0;
            while (size >= 1024 && unitIndex < units.Length - 1)
            {
                size /= 1024;
                unitIndex++;
            }

            return $"{size:0.#} {units[unitIndex]}";
        }
    }
}
