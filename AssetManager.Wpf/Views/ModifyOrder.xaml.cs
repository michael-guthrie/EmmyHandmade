using System.Windows;
using AssetManager.Infrastructure;
using AssetManager.ViewModels;

namespace AssetManager.Views;

public partial class ModifyOrder : Window
{
    private readonly OrderViewModel _viewModel;
    private readonly WindowManager _windows;

    public ModifyOrder(OrderViewModel viewModel, WindowManager windows)
    {
        _viewModel = viewModel;
        _windows = windows;

        DataContext = viewModel;
        InitializeComponent();

        viewModel.SaveCompleted += (_, _) => Close();
    }

    /// <summary>Opens the window on a new order.</summary>
    public void LoadForNew()
    {
        _viewModel.OrderId = null;
        _viewModel.InitializeBackground();
    }

    /// <summary>Opens the window on an existing order.</summary>
    public void LoadForEdit(int orderId)
    {
        _viewModel.OrderId = orderId;
        _viewModel.InitializeBackground();
    }

    /// <summary>
    /// The header money fields feed into the order total, which is a computed property on the
    /// view model rather than on the entity, so it needs a nudge when they change.
    /// </summary>
    private void MoneyField_LostFocus(object sender, RoutedEventArgs e) => _viewModel.RefreshTotal();

    private void NewItemButton_Click(object sender, RoutedEventArgs e)
    {
        // Opening Manage Items rather than a single-item dialog: the old CreateItem window
        // duplicated the same fields and validation that Manage Items already has, and the
        // lookups refresh automatically once it saves.
        _windows.ShowSingleton<ManageItems>();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}
