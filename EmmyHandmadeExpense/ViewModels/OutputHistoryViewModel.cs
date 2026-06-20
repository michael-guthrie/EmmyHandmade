namespace AssetManager.ViewModels
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class OutputHistoryViewModel : ExpenseViewModelBase
    {
        public IEnumerable<Data.OutRecord> Records { get; set; }

        protected override async Task InitializeAsync()
        {
            Records = await Task.Run(() => DataContext.OutRecords.ToList());
            RaisePropertyChanged(nameof(Records));
        }
    }
}
