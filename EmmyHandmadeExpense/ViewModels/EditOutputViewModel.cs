namespace AssetManager.ViewModels
{
    using System.Linq;
    using System.Threading.Tasks;

    public class EditOutputViewModel : OutputViewModelBase
    {
        public int OutRecordId { get; }

        public EditOutputViewModel(int outRecordId) { OutRecordId = outRecordId; }

        protected override async Task InitializeAsync()
        {
            OutRecord = await Task.Run(() => DataContext
                .OutRecords
                .Include(nameof(OutRecord.Materials))
                .Include(nameof(OutRecord.Products))
                .Single(or => or.Id == OutRecordId)
            );
        }
    }
}
