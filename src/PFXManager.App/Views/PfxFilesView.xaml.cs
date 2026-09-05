using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using PFXManager.App.ViewModels;

namespace PFXManager.App.Views;

public partial class PfxFilesView : UserControl
{
    public PfxFilesView()
    {
        InitializeComponent();
    }

    private void SelectionCheckBox_Toggled(object sender, RoutedEventArgs e)
    {
        (DataContext as PfxFilesViewModel)?.NotifySelectionChanged();
    }

    private void Row_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is DataGridRow row)
        {
            row.IsSelected = true;
        }
    }

    private void Row_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is DataGridRow { Item: CertificateRecordViewModel record } &&
            DataContext is PfxFilesViewModel viewModel)
        {
            viewModel.ShowInExplorerCommand.Execute(record);
        }
    }
}
