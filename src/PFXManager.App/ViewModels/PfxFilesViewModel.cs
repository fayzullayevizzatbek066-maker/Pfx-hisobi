using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using PFXManager.App.Resources;
using PFXManager.App.Services;
using PFXManager.Core.Enums;
using PFXManager.Core.Interfaces;
using PFXManager.Core.Models;
using PFXManager.Core.Services;

namespace PFXManager.App.ViewModels;

public sealed partial class PfxFilesViewModel : ObservableObject, IRecipient<NavigateToPfxFilesMessage>
{
    private readonly ICertificateWorkspace _workspace;
    private readonly IBulkSelectionService _bulkSelectionService;
    private readonly IQuarantineService _quarantineService;
    private readonly ICertificateRecordFactory _recordFactory;
    private readonly ICertificateRecordRepository _recordRepository;
    private readonly IDialogService _dialogService;
    private readonly IExplorerService _explorerService;
    private readonly DispatcherTimer _searchDebounceTimer;

    public PfxFilesViewModel(
        ICertificateWorkspace workspace,
        IBulkSelectionService bulkSelectionService,
        IQuarantineService quarantineService,
        ICertificateRecordFactory recordFactory,
        ICertificateRecordRepository recordRepository,
        IDialogService dialogService,
        IExplorerService explorerService)
    {
        _workspace = workspace;
        _bulkSelectionService = bulkSelectionService;
        _quarantineService = quarantineService;
        _recordFactory = recordFactory;
        _recordRepository = recordRepository;
        _dialogService = dialogService;
        _explorerService = explorerService;

        _workspace.Records.CollectionChanged += OnSourceRecordsChanged;

        _searchDebounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        _searchDebounceTimer.Tick += (_, _) =>
        {
            _searchDebounceTimer.Stop();
            ApplyFilter();
        };

        StatusFilterOptions = StatusFilterOption.All_Options;
        _selectedStatusFilter = StatusFilterOption.All;

        ApplyFilter();
        WeakReferenceMessenger.Default.RegisterAll(this);
    }

    public ObservableCollection<CertificateRecordViewModel> FilteredRecords { get; } = new();
    public IReadOnlyList<StatusFilterOption> StatusFilterOptions { get; }
    public IReadOnlyList<string> Drives => new[] { Strings.FilterAllDrives }
        .Concat(_workspace.Records.Select(r => r.Drive).Where(d => !string.IsNullOrWhiteSpace(d)).Distinct().OrderBy(d => d))
        .ToList();

    [ObservableProperty]
    private string _searchText = string.Empty;

    partial void OnSearchTextChanged(string value)
    {
        _searchDebounceTimer.Stop();
        _searchDebounceTimer.Start();
    }

    [ObservableProperty]
    private StatusFilterOption _selectedStatusFilter;

    partial void OnSelectedStatusFilterChanged(StatusFilterOption value) => ApplyFilter();

    [ObservableProperty]
    private string _selectedDrive = Strings.FilterAllDrives;

    partial void OnSelectedDriveChanged(string value) => ApplyFilter();

    [ObservableProperty]
    private string _selectionSummary = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    private void OnSourceRecordsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(Drives));
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var search = SearchText.Trim();
        IEnumerable<CertificateRecordViewModel> query = _workspace.Records;

        query = query.Where(SelectedStatusFilter.Matches);

        if (!string.Equals(SelectedDrive, Strings.FilterAllDrives, StringComparison.Ordinal))
        {
            query = query.Where(r => string.Equals(r.Drive, SelectedDrive, StringComparison.OrdinalIgnoreCase));
        }

        if (search.Length > 0)
        {
            query = query.Where(r => MatchesSearch(r, search));
        }

        var matched = query.ToList();

        FilteredRecords.Clear();
        foreach (var record in matched)
        {
            FilteredRecords.Add(record);
        }

        UpdateSelectionSummary();
    }

    private static bool MatchesSearch(CertificateRecordViewModel r, string search)
    {
        return Contains(r.Record.Organization, search)
               || Contains(r.Record.OwnerDisplayName, search)
               || Contains(r.Record.CommonName, search)
               || Contains(r.Record.Stir, search)
               || Contains(r.Record.Pinfl, search)
               || Contains(r.Record.SerialNumber, search)
               || Contains(r.Record.Thumbprint, search)
               || Contains(r.FileName, search)
               || Contains(r.FullPath, search);
    }

    private static bool Contains(string? haystack, string needle) =>
        !string.IsNullOrEmpty(haystack) && haystack.Contains(needle, StringComparison.OrdinalIgnoreCase);

    private void UpdateSelectionSummary()
    {
        var selectedCount = FilteredRecords.Count(r => r.IsSelected);
        SelectionSummary = selectedCount > 0 ? string.Format(Strings.SelectedCount_Format, selectedCount) : string.Empty;
    }

    public void NotifySelectionChanged() => UpdateSelectionSummary();

    [RelayCommand]
    private void SelectAllExpired()
    {
        var expiredIds = _bulkSelectionService
            .SelectAllExpired(_workspace.Records.Select(r => r.Record))
            .Select(r => r.Id)
            .ToHashSet();

        foreach (var record in FilteredRecords)
        {
            record.IsSelected = expiredIds.Contains(record.Id);
        }

        UpdateSelectionSummary();
        _dialogService.ShowInfo(string.Format(Strings.DiscoveredCount_Format, expiredIds.Count));
    }

    [RelayCommand]
    private void ClearSelection()
    {
        foreach (var record in FilteredRecords)
        {
            record.IsSelected = false;
        }

        UpdateSelectionSummary();
    }

    [RelayCommand]
    private async Task MoveSelectedToQuarantineAsync()
    {
        var selected = FilteredRecords.Where(r => r.IsSelected).ToList();
        if (selected.Count == 0)
        {
            _dialogService.ShowInfo(Strings.NoRecordsSelected);
            return;
        }

        IsBusy = true;
        try
        {
            var results = await _quarantineService.QuarantineAsync(selected.Select(r => r.Record).ToList(), CancellationToken.None);
            await _workspace.ReloadAsync();

            var success = results.Count(r => r.Success);
            var failed = results.Count - success;
            _dialogService.ShowInfo(string.Format(Strings.OperationSucceededFormat, success, failed));
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task EnterPasswordAsync(CertificateRecordViewModel? record)
    {
        if (record is null || record.Status != CertificateStatus.PasswordRequired)
        {
            return;
        }

        var password = _dialogService.PromptForPassword(record.FileName);
        if (password is null)
        {
            return;
        }

        try
        {
            var updated = await _recordFactory.BuildAsync(record.FullPath, password, record.Record.ScanSessionId, CancellationToken.None);
            await _recordRepository.UpsertManyAsync(new[] { updated }, CancellationToken.None);
            record.UpdateRecord(updated);

            if (updated.Status == CertificateStatus.PasswordRequired)
            {
                _dialogService.ShowError("Parol noto'g'ri yoki fayl hali ham o'qib bo'lmadi.");
            }
        }
        finally
        {
            // The plaintext password variable goes out of scope here and is never stored,
            // logged, or placed on the clipboard (section 11).
        }
    }

    [RelayCommand]
    private void ShowInExplorer(CertificateRecordViewModel? record)
    {
        if (record is not null)
        {
            _explorerService.ShowFileInExplorer(record.FullPath);
        }
    }

    [RelayCommand]
    private void CopyPath(CertificateRecordViewModel? record)
    {
        if (record is not null)
        {
            _explorerService.CopyToClipboard(record.FullPath);
        }
    }

    [RelayCommand]
    private async Task RefreshAsync() => await _workspace.ReloadAsync();

    public void Receive(NavigateToPfxFilesMessage message)
    {
        SelectedStatusFilter = message.DuplicatesOnly
            ? StatusFilterOption.Duplicate
            : StatusFilterOptions.FirstOrDefault(o => o.Status == message.StatusFilter && !o.DuplicatesOnly) ?? StatusFilterOption.All;
        SearchText = string.Empty;
        SelectedDrive = Strings.FilterAllDrives;
    }
}
