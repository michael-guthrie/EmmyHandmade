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
    /// Interaction logic for CreateItem.xaml
    /// </summary>
    public partial class CreateItem : Window
    {
        public CreateItem()
        {
            DataContext = new Data.InventoryItem();

            InitializeComponent();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var ii = (Data.InventoryItem)DataContext;
                using (var db = new Data.TrackingStore())
                {
                    db.InventoryItems.Add(ii);
                    db.SaveChanges();
                }
                GalaSoft.MvvmLight.Messaging.Messenger.Default.Send<IEnumerable<int>>(new[] { ii.Id }, Helpers.MessengerToken.Item);
                Close();
            }
            catch (Exception ex)
            {
                Exception exInnermost = ex;
                while (exInnermost.InnerException != null) { exInnermost = exInnermost.InnerException; }
                ErrorLabel.Text = exInnermost.Message;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
