using System.Collections.ObjectModel;
using System.Collections.Specialized;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PFXManager.App.Services;
using PFXManager.Core.Interfaces;

namespace PFXManager.App.ViewModels;

public sealed partial class DuplicatesViewModel : ObservableObject
{
    private readonly ICertificateWorkspace _workspace;
    private readonly IDuplicateDetectionService _duplicateDetectionService;
    private readonly IExplorerService _explorerService;

    public DuplicatesViewModel(ICertificateWorkspace workspace, IDuplicateDetectionService duplicateDetectionService, IExplorerService explorerService)
    {
        _workspace = workspace;
        _duplicateDetectionService = duplicateDetectionService;
        _explorerService = explorerService;
        _workspace.Records.CollectionChanged += OnRecordsChanged;
        Recompute();
    }

    public ObservableCollection<DuplicateGroupViewModel> Groups { get; } = new();

    private void OnRecordsChanged(object? sender, NotifyCollectionChangedEventArgs e) => Recompute();

    [RelayCommand]
    private void Recompute()
    {
        var byId = _workspace.Records.ToDictionary(r => r.Id);
        var groups = _duplicateDetectionService.FindDuplicates(_workspace.Records.Select(r => r.Record));

        Groups.Clear();
        foreach (var group in groups)
        {
            var copies = group.Copies
                .Select(c => byId.TryGetValue(c.Id, out var vm) ? vm : null)
                .Where(vm => vm is not null)
                .Select(vm => vm!)
                .ToList();

            if (copies.Count > 1)
            {
                Groups.Add(new DuplicateGroupViewModel(group, copies));
            }
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
}
