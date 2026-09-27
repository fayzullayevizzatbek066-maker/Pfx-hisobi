using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PFXManager.App.Resources;
using PFXManager.App.Services;
using PFXManager.Core.Interfaces;
using PFXManager.Core.Models;

namespace PFXManager.App.ViewModels;

public sealed partial class WindowsCertificatesViewModel : ObservableObject
{
    private readonly IWindowsCertificateStoreService _storeService;
    private readonly IDialogService _dialogService;

    public WindowsCertificatesViewModel(IWindowsCertificateStoreService storeService, IDialogService dialogService)
    {
        _storeService = storeService;
        _dialogService = dialogService;
        Load();
    }

    public ObservableCollection<WindowsCertificateEntry> Certificates { get; } = new();

    [ObservableProperty]
    private bool _useLocalMachine;

    partial void OnUseLocalMachineChanged(bool value) => Load();

    public string StoreDisplay => UseLocalMachine ? Strings.WindowsCert_LocalMachine : Strings.WindowsCert_CurrentUser;

    [RelayCommand]
    private void Load()
    {
        Certificates.Clear();
        var location = UseLocalMachine ? CertStoreLocation.LocalMachine : CertStoreLocation.CurrentUser;
        foreach (var entry in _storeService.GetCertificates(location))
        {
            Certificates.Add(entry);
        }

        OnPropertyChanged(nameof(StoreDisplay));
    }

    [RelayCommand]
    private void Remove(WindowsCertificateEntry? entry)
    {
        if (entry is null)
        {
            return;
        }

        var message = string.Format(Strings.WindowsCert_RemoveConfirmFormat, entry.Subject);
        if (!_dialogService.Confirm(message, Strings.WindowsCert_Remove))
        {
            return;
        }

        var removed = _storeService.RemoveCertificate(entry.StoreLocation, entry.StoreName, entry.Thumbprint);
        if (!removed)
        {
            _dialogService.ShowError(Strings.WindowsCert_ElevationRequired);
            return;
        }

        Load();
    }
}
