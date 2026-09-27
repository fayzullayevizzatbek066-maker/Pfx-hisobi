using PFXManager.Core.Enums;

namespace PFXManager.App.ViewModels;

/// <summary>Sent by a dashboard card so the shell can switch to PFX Files pre-filtered.</summary>
public sealed record NavigateToPfxFilesMessage(CertificateStatus? StatusFilter, bool DuplicatesOnly = false);
