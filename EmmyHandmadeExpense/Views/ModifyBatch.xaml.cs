using AssetManager.Helpers;
using System.Windows;
using System.Windows.Controls;

namespace AssetManager.Views
{
    /// <summary>
    /// Interaction logic for AddOrder.xaml
    /// </summary>
    public partial class ModifyBatch : Window
    {
        public ModifyBatch()
        {
            InitializeComponent();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            if ((DataContext as ViewModels.ExpenseViewModelBase)?.HasChanges == true)
            {
                var confirm = MessageBox.Show("You will lose outstanding changes if you close.  Continue?", "Change Warning", MessageBoxButton.YesNo);
                if (confirm != MessageBoxResult.Yes) return;
            }
            Close();
        }

        private void NewProductButton_Click(object sender, RoutedEventArgs e)
        {
            var w = new AddProduct();
            var result = w.ShowDialog();
            if ((result == true) && (w.ProductId > 0))
            {
                ((ViewModels.BatchViewModelBase)DataContext).ProductId = w.ProductId;
            }
        }

        private void ShowCostDetail_Click(object sender, RoutedEventArgs e)
        {
            var row = ((Button) sender).GetParentOfType<DataGridRow>();
            row.DetailsVisibility = row.DetailsVisibility == Visibility.Visible
                ? Visibility.Collapsed
                : Visibility.Visible;
        }
    }
}
