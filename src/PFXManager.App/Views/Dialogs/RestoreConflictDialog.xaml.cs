using System.Windows;
using Microsoft.Win32;
using PFXManager.App.Resources;
using PFXManager.Core.Models;

namespace PFXManager.App.Views.Dialogs;

public partial class RestoreConflictDialog : Window
{
    public RestoreOptions? Result { get; private set; }

    public RestoreConflictDialog(string fileName)
    {
        InitializeComponent();
        Title = Strings.Dialog_RestoreConflictTitle;
        MessageText.Text = string.Format(Strings.Dialog_RestoreConflictMessageFormat, fileName);
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        CancelButton.Focus();
    }

    private void RenameButton_Click(object sender, RoutedEventArgs e)
    {
        Result = new RestoreOptions(RestoreConflictAction.RenameNew);
        DialogResult = true;
    }

    private void ChooseButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = Strings.Dialog_ChooseDestination,
            OverwritePrompt = true
        };

        if (dialog.ShowDialog(this) == true)
        {
            Result = new RestoreOptions(RestoreConflictAction.ChooseDestination, dialog.FileName);
            DialogResult = true;
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Result = new RestoreOptions(RestoreConflictAction.Cancel);
        DialogResult = false;
    }
}
