namespace AssetManager.Helpers
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading.Tasks;

    public class UnitLookup : GalaSoft.MvvmLight.ViewModelBase
    {
        public IEnumerable<KeyValuePair<int, string>> Units { get; private set; }
        public IEnumerable<KeyValuePair<int?, string>> NullableUnits { get; private set; }

        public UnitLookup()
        {
            if (IsInDesignMode)
            {
                Units = new ObservableCollection<KeyValuePair<int, string>>(new[]
                    {
                        new KeyValuePair<int, string>(1, "Design Value"),
                    });
            }
            else
            {
                LoadAsync();
            }
        }

        public async void LoadAsync()
        {
            var dbResults = await Task.Run(() =>
                {
                    using (var data = new Data.TrackingStore())
                    {
                        var qUnits = from u in data.UnitsOfMeasure
                                     select new { u.Id, u.Name };
                        var result = qUnits.OrderBy(u => u.Name).ToList();
                        data.Database.Connection.Close();
                        return result;
                    }
                });
            Units = dbResults.Select(i => new KeyValuePair<int, string>(i.Id, i.Name));
            RaisePropertyChanged(nameof(Units));
            NullableUnits = new[] { new KeyValuePair<int?, string>(null, "<Not Set>") }.Concat(
                    dbResults.Select(i => new KeyValuePair<int?, string>(i.Id, i.Name)));
            RaisePropertyChanged(nameof(NullableUnits));
        }
    }
}
