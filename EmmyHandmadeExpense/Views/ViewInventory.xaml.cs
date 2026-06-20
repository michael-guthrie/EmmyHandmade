using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace AssetManager.Views
{
    /// <summary>
    /// Interaction logic for ViewInventory.xaml
    /// </summary>
    public partial class ViewInventory : Window
    {
        readonly ViewModels.ViewInventoryViewModel ViewModel;

        public ViewInventory()
        {
            ViewModel = new ViewModels.ViewInventoryViewModel();
            ViewModel.Printing += ViewModel_Printing;
            DataContext = ViewModel;

            InitializeComponent();

            if (!ViewModel.IsInDesignMode)
            {
                ViewModel.InitializeBackground();
                Closed += (sender, e) =>
                    {
                        ViewModel.Cleanup();
                    };
            }
        }

        private void ViewModel_Printing(object sender, EventArgs e)
        {
            PrintDialog dlgPrint = new PrintDialog()
            {
                CurrentPageEnabled = false,
                SelectedPagesEnabled = false,
                UserPageRangeEnabled = false,
            };
            if (dlgPrint.ShowDialog() == true)
            {
                //dlgPrint.PrintVisual(InventoryGrid, "Inventory");
                var pCap = dlgPrint.PrintQueue.GetPrintCapabilities(dlgPrint.PrintTicket);
                var paperSize = new Size(dlgPrint.PrintableAreaWidth, dlgPrint.PrintableAreaHeight);
                var printArea = new Rect(pCap.PageImageableArea.OriginWidth,
                                         pCap.PageImageableArea.OriginHeight,
                                         pCap.PageImageableArea.ExtentWidth,
                                         pCap.PageImageableArea.ExtentHeight);
                var doc = Helpers.InventoryPaginator.Create(ViewModel, paperSize, printArea);
                dlgPrint.PrintDocument(doc, $"Inventory_{ViewModel.InventoryAsOfDate:yyyy_MM_dd}");
            }
        }

        private void ViewHistoryButton_Click(object sender, RoutedEventArgs e)
        {
            var src = (Button)sender;
            var dataSrc = (ViewModels.ViewInventoryViewModel.InventoryGridItem)src.DataContext;
            var itemId = dataSrc.Id;
            var historyWindow = new ViewInventoryItemHistory(itemId);
            historyWindow.ShowDialog();
        }
    }
}
