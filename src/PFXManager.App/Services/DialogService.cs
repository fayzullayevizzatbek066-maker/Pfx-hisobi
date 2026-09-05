using System.Windows;
using Microsoft.Win32;
using PFXManager.App.Resources;
using PFXManager.App.Views.Dialogs;
using PFXManager.Core.Models;

namespace PFXManager.App.Services;

public sealed class DialogService : IDialogService
{
    public bool ConfirmPermanentDelete(int count)
    {
        var dialog = new ConfirmDeleteDialog(count) { Owner = Application.Current.MainWindow };
        return dialog.ShowDialog() == true && dialog.Confirmed;
    }

    public bool Confirm(string message, string title)
    {
        var result = MessageBox.Show(Application.Current.MainWindow, message, title,
            MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
        return result == MessageBoxResult.Yes;
    }

    public string? PromptForPassword(string fileName)
    {
        var dialog = new PasswordPromptDialog(fileName) { Owner = Application.Current.MainWindow };
        return dialog.ShowDialog() == true ? dialog.EnteredPassword : null;
    }

    public RestoreOptions? ResolveRestoreConflict(string fileName)
    {
        var dialog = new RestoreConflictDialog(fileName) { Owner = Application.Current.MainWindow };
        dialog.ShowDialog();
        return dialog.Result;
    }

    public string? PickFolder(string title)
    {
        var dialog = new OpenFolderDialog { Title = title };
        return dialog.ShowDialog(Application.Current.MainWindow) == true ? dialog.FolderName : null;
    }

    public void ShowInfo(string message, string title = "")
    {
        MessageBox.Show(Application.Current.MainWindow, message, string.IsNullOrEmpty(title) ? Strings.AppTitle : title,
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    public void ShowError(string message, string title = "")
    {
        MessageBox.Show(Application.Current.MainWindow, message, string.IsNullOrEmpty(title) ? Strings.AppTitle : title,
            MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
