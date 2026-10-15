using System.Windows;
using AssetManager.ViewModels;

namespace AssetManager.Views;

public partial class ViewInventoryItemHistory : Window
{
    private readonly InventoryItemHistoryViewModel _viewModel;

    public ViewInventoryItemHistory(InventoryItemHistoryViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
    }

    /// <summary>Chooses which item's history to show. Called before the dialog is shown.</summary>
    public void LoadFor(int inventoryItemId)
    {
        _viewModel.InventoryItemId = inventoryItemId;
        _viewModel.InitializeBackground();
    }
}
