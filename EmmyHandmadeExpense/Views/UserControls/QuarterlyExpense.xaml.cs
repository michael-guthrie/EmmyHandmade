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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AssetManager.Views.UserControls
{
    /// <summary>
    /// Interaction logic for QuarterlyExpense.xaml
    /// </summary>
    public partial class QuarterlyExpense : UserControl
    {
        public QuarterlyExpense()
        {
            var vm = new ViewModels.QuarterlyExpenseViewModel();
            DataContext = vm;

            if (!vm.IsInDesignMode)
            {
                vm.InitializeBackground();
            }

            InitializeComponent();
        }
    }
}
