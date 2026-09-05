using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PFXManager.App.Resources;
using PFXManager.App.Services;
using PFXManager.Core.Interfaces;
using PFXManager.Core.Models;

namespace PFXManager.App.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly IAppSettingsRepository _settingsRepository;
    private readonly IDialogService _dialogService;

    public SettingsViewModel(IAppSettingsRepository settingsRepository, IDialogService dialogService)
    {
        _settingsRepository = settingsRepository;
        _dialogService = dialogService;
    }

    [ObservableProperty] private bool _scanLocalFixedDrives = true;
    [ObservableProperty] private bool _scanNetworkDrives;
    [ObservableProperty] private bool _scanRemovableDrives;
    [ObservableProperty] private int _expiringSoonWarningDays = 30;
    [ObservableProperty] private int _expiringWarningDays = 90;
    [ObservableProperty] private string _quarantinePath = string.Empty;
    [ObservableProperty] private AppTheme _theme = AppTheme.System;

    public ObservableCollection<string> CustomFolders { get; } = new();

    [ObservableProperty]
    private string? _selectedCustomFolder;

    public IReadOnlyList<AppTheme> Themes { get; } = new[] { AppTheme.System, AppTheme.Light, AppTheme.Dark };

    [RelayCommand]
    private async Task LoadAsync()
    {
        var settings = await _settingsRepository.LoadAsync(CancellationToken.None);
        Apply(settings);
    }

    private void Apply(AppSettings settings)
    {
        ScanLocalFixedDrives = settings.ScanLocalFixedDrives;
        ScanNetworkDrives = settings.ScanNetworkDrives;
        ScanRemovableDrives = settings.ScanRemovableDrives;
        ExpiringSoonWarningDays = settings.ExpiringSoonWarningDays;
        ExpiringWarningDays = settings.ExpiringWarningDays;
        QuarantinePath = settings.QuarantinePath;
        Theme = settings.Theme;

        CustomFolders.Clear();
        foreach (var folder in settings.CustomFolders)
        {
            CustomFolders.Add(folder);
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        var settings = new AppSettings
        {
            ScanLocalFixedDrives = ScanLocalFixedDrives,
            ScanNetworkDrives = ScanNetworkDrives,
            ScanRemovableDrives = ScanRemovableDrives,
            ExpiringSoonWarningDays = ExpiringSoonWarningDays,
            ExpiringWarningDays = ExpiringWarningDays,
            QuarantinePath = QuarantinePath,
            Theme = Theme,
            CustomFolders = CustomFolders.ToList()
        };

        await _settingsRepository.SaveAsync(settings, CancellationToken.None);
        _dialogService.ShowInfo(Strings.Settings_Saved);
    }

    [RelayCommand]
    private void AddCustomFolder()
    {
        var folder = _dialogService.PickFolder(Strings.Settings_AddFolder);
        if (!string.IsNullOrWhiteSpace(folder) && !CustomFolders.Contains(folder, StringComparer.OrdinalIgnoreCase))
        {
            CustomFolders.Add(folder);
        }
    }

    [RelayCommand]
    private void RemoveCustomFolder()
    {
        if (SelectedCustomFolder is not null)
        {
            CustomFolders.Remove(SelectedCustomFolder);
        }
    }

    [RelayCommand]
    private void PickQuarantinePath()
    {
        var folder = _dialogService.PickFolder(Strings.Settings_QuarantinePath);
        if (!string.IsNullOrWhiteSpace(folder))
        {
            QuarantinePath = folder;
        }
    }
}
