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
    /// Interaction logic for AddProduct.xaml
    /// </summary>
    public partial class AddProduct : Window
    {
        public int ProductId { get; private set; }

        public AddProduct()
        {
            InitializeComponent();

            if (!GalaSoft.MvvmLight.ViewModelBase.IsInDesignModeStatic)
            {
                this.Loaded += (sender, e) => ProductName.Focus();
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            using (var db = new Data.TrackingStore())
            {
                // Check for unique name.
                if (db.BatchProducts.Any(p => p.Name == ProductName.Text))
                {
                    ErrorBlock.Text = "Name already exists.";
                    return;
                }
                var newProductName = new Data.BatchProduct() { Name = ProductName.Text };
                db.BatchProducts.Add(newProductName);
                db.SaveChanges();
                ProductId = newProductName.Id;
            }
            GalaSoft.MvvmLight.Messaging.Messenger.Default.Send<IEnumerable<int>>(new[] { ProductId }, Helpers.MessengerToken.Product);
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
