using System.Windows;
using System.Windows.Input;
using PFXManager.App.Resources;

namespace PFXManager.App.Views.Dialogs;

public partial class PasswordPromptDialog : Window
{
    /// <summary>
    /// Never exposed as a bindable property: read directly from the PasswordBox only when the
    /// user submits, and never stored anywhere beyond this local variable's lifetime.
    /// </summary>
    public string? EnteredPassword { get; private set; }

    public PasswordPromptDialog(string fileName)
    {
        InitializeComponent();
        Title = Strings.Dialog_PasswordTitle;
        PromptText.Text = string.Format(Strings.Dialog_PasswordPromptFormat, fileName);
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        PasswordInput.Focus();
    }

    private void PasswordInput_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            Submit();
        }
    }

    private void OkButton_Click(object sender, RoutedEventArgs e) => Submit();

    private void Submit()
    {
        EnteredPassword = PasswordInput.Password;
        DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        EnteredPassword = null;
        DialogResult = false;
    }
}
