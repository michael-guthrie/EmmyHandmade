namespace AssetManager.ViewModels
{
    using AssetManager.Data;
    using System.Linq;
    using System.Threading.Tasks;

    public class EditBatchViewModel : BatchViewModelBase
    {
        public override string Title => "View / Edit Batch";

        public int BatchId { get; }

        public EditBatchViewModel(int batchId)
        {
            BatchId = batchId;
        }

        protected override async Task InitializeAsync()
        {
            var asyncLoadResult = await Task.Run(() =>
                {
                    var matchingBatch = DataContext.Batches.Single(b => b.Id == BatchId);
                    var usedItems = matchingBatch.BatchItems.ToList().Select(oi => new BatchItemWrapper<BatchItem>(oi) { DataContext = DataContext });
                    var lossItems = matchingBatch.BatchLossItems.ToList().Select(oi => new BatchItemWrapper<BatchLossItem>(oi) { DataContext = DataContext });
                    decimal totalQuantity = matchingBatch.BatchItems.Sum(i => i.Quantity) / 100M;
                    return new { Batch = matchingBatch, UsedItems = usedItems, LossItems = lossItems, TotalQuantity = totalQuantity };
                });
            Batch = asyncLoadResult.Batch;
            UsedItems.CollectionChanged -= OnUsedItemsCollectionChanged;
            foreach (var bi in asyncLoadResult.UsedItems)
            {
                UsedItems.Add(bi);
            }
            UsedItems.CollectionChanged += OnUsedItemsCollectionChanged;
            LossItems.CollectionChanged -= OnLossItemsCollectionChanged;
            foreach (var li in asyncLoadResult.LossItems)
            {
                LossItems.Add(li);
            }
            LossItems.CollectionChanged += OnLossItemsCollectionChanged;
            await UpdateItemCosts();
            await UpdateUsedItemPercentages();
            UpdateAggregateCosts(ItemsType.Both);
        }
    }
}
