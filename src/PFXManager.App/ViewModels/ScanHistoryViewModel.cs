using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PFXManager.Core.Interfaces;
using PFXManager.Core.Models;

namespace PFXManager.App.ViewModels;

public sealed partial class ScanHistoryViewModel : ObservableObject
{
    private readonly IScanSessionRepository _scanSessionRepository;

    public ScanHistoryViewModel(IScanSessionRepository scanSessionRepository)
    {
        _scanSessionRepository = scanSessionRepository;
    }

    public ObservableCollection<ScanSession> Sessions { get; } = new();

    [RelayCommand]
    public async Task LoadAsync()
    {
        var sessions = await _scanSessionRepository.GetAllAsync(CancellationToken.None);
        Sessions.Clear();
        foreach (var session in sessions)
        {
            Sessions.Add(session);
        }
    }
}
