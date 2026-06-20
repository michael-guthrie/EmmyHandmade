namespace AssetManager.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows.Data;

    public class ItemUnitConverter : IValueConverter
    {
        private Dictionary<int, string> UnitLookups;

        public ItemUnitConverter()
        {
            if (!GalaSoft.MvvmLight.ViewModelBase.IsInDesignModeStatic)
            {
                LoadAsync();
                GalaSoft.MvvmLight.Messaging.Messenger.Default.Register<IEnumerable<int>>(this, MessengerToken.Item, itemIds => LoadAsync());
            }
        }

        public void LoadAsync()
        {
            Task.Run(() =>
                {
                    using (var data = new Data.TrackingStore())
                    {
                        var qResults = from i in data.InventoryItems
                                       select new { i.Id, i.UnitOfMeasure.Name };
                        UnitLookups = qResults.ToList().ToDictionary(i => i.Id, i => i.Name);
                        data.Database.Connection.Close();
                    }
                });
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (UnitLookups != null)
            {
                int itemId = (int)value;
                if (UnitLookups.ContainsKey(itemId)) return UnitLookups[itemId];
            }
            return "N/A";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
