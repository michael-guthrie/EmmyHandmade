namespace AssetManager.Helpers
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class ItemLookup : GalaSoft.MvvmLight.ViewModelBase
    {
        public IEnumerable<KeyValuePair<int, string>> Items { get; private set; }

        public ItemLookup()
        {
            if (IsInDesignMode)
            {
                Items = new List<KeyValuePair<int, string>>(new[]
                    {
                        new KeyValuePair<int, string>(1, "Design Value"),
                    });
            }
            else
            {
                LoadAsync();
                MessengerInstance.Register<IEnumerable<int>>(this, MessengerToken.Item, itemIds => LoadAsync());
            }
        }

        public async void LoadAsync()
        {
            Items = await Task.Run(() =>
                {
                    using (var data = new Data.TrackingStore())
                    {
                        var qItems = from i in data.InventoryItems
                                     select new { i.Id, i.Name };
                        var result = qItems.OrderBy(i => i.Name).ToList().Select(i => new KeyValuePair<int, string>(i.Id, i.Name));
                        data.Database.Connection.Close();
                        return result;
                    }
                });
            RaisePropertyChanged(nameof(Items));
        }
    }
}
