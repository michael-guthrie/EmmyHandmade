using AssetManager.ViewModels;
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
    /// Interaction logic for OrderHistory.xaml
    /// </summary>
    public partial class OrderHistory : Window
    {
        ViewModels.OrderHistoryViewModel ViewModel = new ViewModels.OrderHistoryViewModel();

        public OrderHistory()
        {
            DataContext = ViewModel;

            InitializeComponent();

            if (!ViewModel.IsInDesignMode)
            {
                ViewModel.InitializeBackground();
                Closed += (sender, e) =>
                {
                    ViewModel.Cleanup();
                    ViewModel = null;
                };
            }
        }

        private void ViewEditButton_Click(object sender, RoutedEventArgs e)
        {
            var src = (Button)sender;
            var srcItem = (ViewModels.OrderHistoryViewModel.OrderHistoryItem)src.DataContext;
            var w = new ModifyOrder();
            var vm = new EditOrderViewModel(srcItem.Id);
            w.DataContext = vm;
            vm.InitializeBackground();
            w.ShowDialog();
            w.DataContext = null;
            vm.Cleanup();
            vm = null;
        }
    }
}
