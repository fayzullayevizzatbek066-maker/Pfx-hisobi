using System.Linq;
using System.Windows.Controls;
using PFXManager.App.ViewModels;
using PFXManager.Core.Models;

namespace PFXManager.App.Views;

public partial class QuarantineView : UserControl
{
    public QuarantineView()
    {
        InitializeComponent();
    }

    private async void DeleteSelected_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is not QuarantineViewModel viewModel)
        {
            return;
        }

        var selected = Grid.SelectedItems.Cast<QuarantineItem>().ToList();
        await viewModel.PermanentDeleteAllSelectedCommand.ExecuteAsync(selected);
    }
}
