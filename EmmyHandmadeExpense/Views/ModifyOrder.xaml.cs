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
    /// Interaction logic for AddOrder.xaml
    /// </summary>
    public partial class ModifyOrder : Window
    {
        public ModifyOrder()
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

        private void NewItemButton_Click(object sender, RoutedEventArgs e)
        {
            (new CreateItem()).ShowDialog();
        }
    }
}
