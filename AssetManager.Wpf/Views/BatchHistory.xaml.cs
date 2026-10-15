using System.Windows;
using System.Windows.Controls;
using AssetManager.Core.Data.Entities;
using AssetManager.Infrastructure;
using AssetManager.ViewModels;

namespace AssetManager.Views;

public partial class BatchHistory : Window
{
    private readonly WindowManager _windows;

    public BatchHistory(BatchHistoryViewModel viewModel, WindowManager windows)
    {
        _windows = windows;
        DataContext = viewModel;
        InitializeComponent();
        viewModel.InitializeBackground();
    }

    private void ViewEditButton_Click(object sender, RoutedEventArgs e)
    {
        if (((Button)sender).DataContext is not Batch batch) return;

        _windows.Show<ModifyBatch>(w => w.LoadForEdit(batch.Id));
    }
}
