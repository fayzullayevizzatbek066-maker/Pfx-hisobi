using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PFXManager.App.Resources;
using PFXManager.App.Services;
using PFXManager.Core.Interfaces;
using PFXManager.Core.Models;

namespace PFXManager.App.ViewModels;

public sealed partial class QuarantineViewModel : ObservableObject
{
    private readonly IQuarantineRepository _quarantineRepository;
    private readonly IQuarantineService _quarantineService;
    private readonly ICertificateWorkspace _workspace;
    private readonly IDialogService _dialogService;

    public QuarantineViewModel(
        IQuarantineRepository quarantineRepository,
        IQuarantineService quarantineService,
        ICertificateWorkspace workspace,
        IDialogService dialogService)
    {
        _quarantineRepository = quarantineRepository;
        _quarantineService = quarantineService;
        _workspace = workspace;
        _dialogService = dialogService;
    }

    public ObservableCollection<QuarantineItem> Items { get; } = new();

    [ObservableProperty]
    private bool _isBusy;

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var items = await _quarantineRepository.GetActiveAsync(CancellationToken.None);
            Items.Clear();
            foreach (var item in items)
            {
                Items.Add(item);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task RestoreAsync(QuarantineItem? item)
    {
        if (item is null)
        {
            return;
        }

        var fileExistsAtOriginal = File.Exists(item.OriginalPath);
        RestoreOptions options = fileExistsAtOriginal
            ? _dialogService.ResolveRestoreConflict(item.FileName) ?? new RestoreOptions(RestoreConflictAction.Cancel)
            : new RestoreOptions(RestoreConflictAction.Cancel);

        var result = await _quarantineService.RestoreAsync(item.Id, options, CancellationToken.None);
        if (result.Success)
        {
            await _workspace.ReloadAsync();
            await LoadAsync();
        }
        else if (result.ErrorMessage is not null)
        {
            _dialogService.ShowError(result.ErrorMessage);
        }
    }

    [RelayCommand]
    private async Task PermanentDeleteAsync(QuarantineItem? item)
    {
        if (item is null)
        {
            return;
        }

        if (!_dialogService.ConfirmPermanentDelete(1))
        {
            return;
        }

        var success = await _quarantineService.PermanentlyDeleteAsync(item.Id, CancellationToken.None);
        if (success)
        {
            await LoadAsync();
        }
        else
        {
            _dialogService.ShowError("O'chirishda xatolik yuz berdi.");
        }
    }

    [RelayCommand]
    private async Task PermanentDeleteAllSelectedAsync(IReadOnlyList<QuarantineItem> items)
    {
        if (items.Count == 0)
        {
            _dialogService.ShowInfo(Strings.NoRecordsSelected);
            return;
        }

        if (!_dialogService.ConfirmPermanentDelete(items.Count))
        {
            return;
        }

        var succeeded = 0;
        foreach (var item in items)
        {
            if (await _quarantineService.PermanentlyDeleteAsync(item.Id, CancellationToken.None))
            {
                succeeded++;
            }
        }

        _dialogService.ShowInfo(string.Format(Strings.OperationSucceededFormat, succeeded, items.Count - succeeded));
        await LoadAsync();
    }
}
