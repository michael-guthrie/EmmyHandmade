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
    /// Interaction logic for BatchHistory.xaml
    /// </summary>
    public partial class BatchHistory : Window
    {
        ViewModels.BatchHistoryViewModel ViewModel = new ViewModels.BatchHistoryViewModel();

        public BatchHistory()
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
            var srcItem = (Data.Batch)src.DataContext;
            var w = new ModifyBatch();
            var vm = new EditBatchViewModel(srcItem.Id);
            w.DataContext = vm;
            vm.InitializeBackground();
            w.ShowDialog();
            w.DataContext = null;
            vm.Cleanup();
            vm = null;
        }
    }
}
