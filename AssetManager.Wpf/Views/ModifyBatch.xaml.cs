using System.Windows;
using System.Windows.Controls;
using AssetManager.Infrastructure;
using AssetManager.ViewModels;

namespace AssetManager.Views;

public partial class ModifyBatch : Window
{
    private readonly BatchViewModel _viewModel;
    private readonly WindowManager _windows;

    public ModifyBatch(BatchViewModel viewModel, WindowManager windows)
    {
        _viewModel = viewModel;
        _windows = windows;

        DataContext = viewModel;
        InitializeComponent();

        viewModel.SaveCompleted += (_, _) => Close();
    }

    public void LoadForNew()
    {
        _viewModel.BatchId = null;
        _viewModel.InitializeBackground();
    }

    public void LoadForEdit(int batchId)
    {
        _viewModel.BatchId = batchId;
        _viewModel.InitializeBackground();
    }

    private async void NewProductButton_Click(object sender, RoutedEventArgs e)
    {
        AddProduct? dialog = null;
        var created = _windows.ShowDialog<AddProduct>(w => dialog = w, this);

        if (created == true && dialog is not null)
        {
            await _viewModel.SelectNewProductAsync(dialog.CreatedProductId);
        }
    }

    /// <summary>
    /// Toggles the row-details panel, which holds the cost breakdown for that line. The old
    /// version did the same thing; the data behind it now comes from the shared costing service.
    /// </summary>
    private void ShowCostDetail_Click(object sender, RoutedEventArgs e)
    {
        var row = FindParent<DataGridRow>((DependencyObject)sender);
        if (row is null) return;

        row.DetailsVisibility = row.DetailsVisibility == Visibility.Visible
            ? Visibility.Collapsed
            : Visibility.Visible;
    }

    private static T? FindParent<T>(DependencyObject child) where T : DependencyObject
    {
        while (true)
        {
            var parent = System.Windows.Media.VisualTreeHelper.GetParent(child);
            if (parent is null) return null;
            if (parent is T match) return match;
            child = parent;
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}
