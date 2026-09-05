using PFXManager.Core.Models;

namespace PFXManager.App.Services;

public interface IDialogService
{
    bool ConfirmPermanentDelete(int count);

    bool Confirm(string message, string title);

    string? PromptForPassword(string fileName);

    RestoreOptions? ResolveRestoreConflict(string fileName);

    string? PickFolder(string title);

    void ShowInfo(string message, string title = "");

    void ShowError(string message, string title = "");
}
