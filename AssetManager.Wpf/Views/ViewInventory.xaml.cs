using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AssetManager.Core.Services;
using AssetManager.Helpers;
using AssetManager.Infrastructure;
using AssetManager.ViewModels;

namespace AssetManager.Views;

public partial class ViewInventory : Window
{
    private readonly ViewInventoryViewModel _viewModel;
    private readonly WindowManager _windows;

    public ViewInventory(ViewInventoryViewModel viewModel, WindowManager windows)
    {
        _viewModel = viewModel;
        _windows = windows;

        DataContext = viewModel;
        InitializeComponent();

        viewModel.Printing += OnPrinting;
        viewModel.InitializeBackground();
    }

    private void OnPrinting(object? sender, EventArgs e)
    {
        var printDialog = new PrintDialog
        {
            CurrentPageEnabled = false,
            SelectedPagesEnabled = false,
            UserPageRangeEnabled = false,
        };

        if (printDialog.ShowDialog() != true) return;

        var capabilities = printDialog.PrintQueue.GetPrintCapabilities(printDialog.PrintTicket);
        var paperSize = new Size(printDialog.PrintableAreaWidth, printDialog.PrintableAreaHeight);

        var imageableArea = capabilities.PageImageableArea;
        var printArea = imageableArea is null
            ? new Rect(paperSize)
            : new Rect(imageableArea.OriginWidth, imageableArea.OriginHeight,
                       imageableArea.ExtentWidth, imageableArea.ExtentHeight);

        // Text measurement needs the DPI scale of the screen this window is on.
        var pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;

        var document = InventoryPaginator.Create(
            _viewModel.Items, _viewModel.InventoryAsOfDate, paperSize, printArea, pixelsPerDip);

        if (document.PageCount == 0)
        {
            MessageBox.Show(this, "There is nothing to print.", "Print",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        printDialog.PrintDocument(document, $"Inventory_{_viewModel.InventoryAsOfDate:yyyy_MM_dd}");
    }

    private void ViewHistoryButton_Click(object sender, RoutedEventArgs e)
    {
        if (((Button)sender).DataContext is not InventoryTotal row) return;

        _windows.ShowDialog<ViewInventoryItemHistory>(w => w.LoadFor(row.Item.Id), this);
    }
}
