using System.Collections.Specialized;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using PFXManager.App.Services;
using PFXManager.Core.Enums;

namespace PFXManager.App.ViewModels;

public sealed partial class DashboardViewModel : ObservableObject
{
    private readonly ICertificateWorkspace _workspace;

    public DashboardViewModel(ICertificateWorkspace workspace)
    {
        _workspace = workspace;
        _workspace.Records.CollectionChanged += OnRecordsChanged;
        RecomputeCounts();
    }

    private void OnRecordsChanged(object? sender, NotifyCollectionChangedEventArgs e) => RecomputeCounts();

    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private int _activeCount;
    [ObservableProperty] private int _expiredCount;
    [ObservableProperty] private int _expiringSoonCount;
    [ObservableProperty] private int _expiringCount;
    [ObservableProperty] private int _duplicateCount;
    [ObservableProperty] private int _passwordRequiredCount;
    [ObservableProperty] private int _readErrorCount;

    private void RecomputeCounts()
    {
        var records = _workspace.Records;
        TotalCount = records.Count;
        ActiveCount = records.Count(r => r.Status == CertificateStatus.Active);
        ExpiredCount = records.Count(r => r.Status == CertificateStatus.Expired);
        ExpiringSoonCount = records.Count(r => r.Status == CertificateStatus.ExpiringSoon);
        ExpiringCount = records.Count(r => r.Status == CertificateStatus.Expiring);
        PasswordRequiredCount = records.Count(r => r.Status == CertificateStatus.PasswordRequired);
        ReadErrorCount = records.Count(r => r.Status == CertificateStatus.ReadError);
        DuplicateCount = records.Count(r => r.IsDuplicate);
    }

    [RelayCommand]
    private void ShowTotal() => WeakReferenceMessenger.Default.Send(new NavigateToPfxFilesMessage(null));

    [RelayCommand]
    private void ShowActive() => WeakReferenceMessenger.Default.Send(new NavigateToPfxFilesMessage(CertificateStatus.Active));

    [RelayCommand]
    private void ShowExpired() => WeakReferenceMessenger.Default.Send(new NavigateToPfxFilesMessage(CertificateStatus.Expired));

    [RelayCommand]
    private void ShowExpiringSoon() => WeakReferenceMessenger.Default.Send(new NavigateToPfxFilesMessage(CertificateStatus.ExpiringSoon));

    [RelayCommand]
    private void ShowExpiring() => WeakReferenceMessenger.Default.Send(new NavigateToPfxFilesMessage(CertificateStatus.Expiring));

    [RelayCommand]
    private void ShowPasswordRequired() => WeakReferenceMessenger.Default.Send(new NavigateToPfxFilesMessage(CertificateStatus.PasswordRequired));

    [RelayCommand]
    private void ShowReadError() => WeakReferenceMessenger.Default.Send(new NavigateToPfxFilesMessage(CertificateStatus.ReadError));

    [RelayCommand]
    private void ShowDuplicates() => WeakReferenceMessenger.Default.Send(new NavigateToPfxFilesMessage(null, DuplicatesOnly: true));
}
