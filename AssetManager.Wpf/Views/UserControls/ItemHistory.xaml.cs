using System.Windows.Controls;

namespace AssetManager.Views.UserControls;

/// <summary>
/// Cost-detail grid shown as a batch row's details. Its DataContext is the BatchLineViewModel,
/// whose CostHistory comes straight from the shared costing service.
/// </summary>
public partial class ItemHistory : UserControl
{
    public ItemHistory() => InitializeComponent();
}
