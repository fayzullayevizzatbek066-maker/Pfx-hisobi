using PFXManager.Core.Models;

namespace PFXManager.App.ViewModels;

public sealed class DuplicateGroupViewModel
{
    public DuplicateGroupViewModel(DuplicateGroup group, IReadOnlyList<CertificateRecordViewModel> copies)
    {
        Thumbprint = group.Thumbprint;
        Copies = copies;
    }

    public string Thumbprint { get; }
    public IReadOnlyList<CertificateRecordViewModel> Copies { get; }
    public int CopyCount => Copies.Count;
    public string OwnerDisplay => Copies.FirstOrDefault()?.OwnerDisplay ?? "—";
}
