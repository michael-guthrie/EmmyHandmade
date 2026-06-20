namespace AssetManager.ViewModels
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class BatchHistoryViewModel : ExpenseViewModelBase
    {
        public IEnumerable<Data.Batch> Batches { get; private set; }

        public BatchHistoryViewModel()
        {
            MessengerInstance.Register<IEnumerable<int>>(this, Helpers.MessengerToken.Batch, batchIds => InitializeBackground());
        }

        protected override async Task InitializeAsync()
        {
            Batches = await Task.Run(() => DataContext.Batches.Include(nameof(Data.Batch.Product)).ToList());
            RaisePropertyChanged(nameof(Batches));
        }
    }
}
