using System.Windows.Controls;

namespace AssetManager.Views.UserControls;

/// <summary>
/// Quarterly totals panel. The DataContext is assigned by MainWindow from the container, rather
/// than the control constructing its own view model as it did before.
/// </summary>
public partial class QuarterlyExpense : UserControl
{
    public QuarterlyExpense() => InitializeComponent();
}
