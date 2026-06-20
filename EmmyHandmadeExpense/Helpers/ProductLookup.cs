namespace AssetManager.Helpers
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class ProductLookup : GalaSoft.MvvmLight.ViewModelBase
    {
        public IEnumerable<KeyValuePair<int, string>> Products { get; private set; }

        public ProductLookup()
        {
            if (IsInDesignMode)
            {
                Products = new List<KeyValuePair<int, string>>(new[]
                    {
                        new KeyValuePair<int, string>(1, "Design Value"),
                    });
            }
            else
            {
                LoadAsync();
                MessengerInstance.Register<IEnumerable<int>>(this, MessengerToken.Product, itemIds => LoadAsync());
            }
        }

        public async void LoadAsync()
        {
            Products = await Task.Run(() =>
                {
                    using (var data = new Data.TrackingStore())
                    {
                        var qProducts = from p in data.BatchProducts
                                     select new { p.Id, p.Name };
                        var result = qProducts.OrderBy(p => p.Name).ToList().Select(p => new KeyValuePair<int, string>(p.Id, p.Name));
                        data.Database.Connection.Close();
                        return result;
                    }
                });
            RaisePropertyChanged(nameof(Products));
        }
    }
}
