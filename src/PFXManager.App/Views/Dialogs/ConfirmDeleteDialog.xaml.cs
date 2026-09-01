using System.Windows;
using PFXManager.App.Resources;

namespace PFXManager.App.Views.Dialogs;

public partial class ConfirmDeleteDialog : Window
{
    public string Message { get; }
    public string CancelText => Strings.Dialog_Cancel;
    public string DeleteText => Strings.Dialog_DeletePermanently;

    public bool Confirmed { get; private set; }

    public ConfirmDeleteDialog(int count)
    {
        InitializeComponent();
        Title = Strings.Dialog_PermanentDeleteTitle;
        Message = string.Format(Strings.Dialog_PermanentDeleteMessageFormat, count);
        DataContext = this;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        CancelButton.Focus();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Confirmed = false;
        DialogResult = false;
    }

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        Confirmed = true;
        DialogResult = true;
    }
}
