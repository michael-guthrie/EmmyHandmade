using System.Windows;
using System.Windows.Controls;
using AssetManager.Core.Data.Entities;
using AssetManager.Infrastructure;
using AssetManager.ViewModels;

namespace AssetManager.Views;

public partial class OutputHistory : Window
{
    private readonly WindowManager _windows;

    public OutputHistory(OutputHistoryViewModel viewModel, WindowManager windows)
    {
        _windows = windows;
        DataContext = viewModel;
        InitializeComponent();
        viewModel.InitializeBackground();
    }

    private void ViewEditButton_Click(object sender, RoutedEventArgs e)
    {
        if (((Button)sender).DataContext is not OutRecord record) return;

        _windows.Show<ModifyOutput>(w => w.LoadForEdit(record.Id));
    }
}
