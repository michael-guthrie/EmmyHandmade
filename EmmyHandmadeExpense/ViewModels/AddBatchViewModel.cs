namespace AssetManager.ViewModels
{
    using System;
    using System.Threading.Tasks;

    public class AddBatchViewModel : BatchViewModelBase
    {
        public override string Title => "Add Batch";

        protected override async Task InitializeAsync()
        {
            Batch = await Task.Run(() =>
                {
                    var b = DataContext.Batches.Create();
                    b.BatchDate = DateTime.Now;
                    DataContext.Batches.Add(b);
                    return b;
                });
        }
    }
}
