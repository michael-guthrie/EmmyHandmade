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
    /// Interaction logic for ManageMiscExpenses.xaml
    /// </summary>
    public partial class ManageMiscExpenses : Window
    {
        ViewModels.MiscExpenseViewModel ViewModel = new ViewModels.MiscExpenseViewModel();

        public ManageMiscExpenses()
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
                ViewModel.ValidationFailed += ViewModel_ValidationFailed;
            }
        }

        private void ViewModel_ValidationFailed(object sender, Helpers.ValidationEventArgs e)
        {
            MessageBox.Show(e.ErrorMessage);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.HasChanges)
            {
                var confirm = MessageBox.Show("You will lose outstanding changes if you close.  Continue?", "Change Warning", MessageBoxButton.YesNo);
                if (confirm != MessageBoxResult.Yes) return;
            }
            Close();
        }
    }
}
