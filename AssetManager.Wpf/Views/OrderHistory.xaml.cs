using System.Windows;
using System.Windows.Controls;
using AssetManager.Infrastructure;
using AssetManager.ViewModels;

namespace AssetManager.Views;

public partial class OrderHistory : Window
{
    private readonly WindowManager _windows;

    public OrderHistory(OrderHistoryViewModel viewModel, WindowManager windows)
    {
        _windows = windows;
        DataContext = viewModel;
        InitializeComponent();
        viewModel.InitializeBackground();
    }

    private void ViewEditButton_Click(object sender, RoutedEventArgs e)
    {
        if (((Button)sender).DataContext is not OrderHistoryViewModel.OrderHistoryItem row) return;

        _windows.Show<ModifyOrder>(w => w.LoadForEdit(row.Id));
    }
}
