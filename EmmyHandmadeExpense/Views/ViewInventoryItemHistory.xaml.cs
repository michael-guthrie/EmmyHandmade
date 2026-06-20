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
    /// Interaction logic for ViewInventoryItemHistory.xaml
    /// </summary>
    public partial class ViewInventoryItemHistory : Window
    {
        public ViewInventoryItemHistory(int inventoryItemId)
        {
            InitializeComponent();

            var viewModel = new ViewModels.InventoryItemHistoryViewModel(inventoryItemId);
            this.DataContext = viewModel;

            if (!viewModel.IsInDesignMode)
            {
                viewModel.InitializeBackground();
                Closed += (sender, e) =>
                    {
                        viewModel.Cleanup();
                    };
            }
        }
    }
}
