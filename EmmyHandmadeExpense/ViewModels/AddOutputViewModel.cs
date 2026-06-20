namespace AssetManager.ViewModels
{
    using System;
    using System.Threading.Tasks;

    public class AddOutputViewModel : OutputViewModelBase
    {
        protected override async Task InitializeAsync()
        {
            OutRecord = new Data.OutRecord() { Date = DateTime.Today };
            DataContext.OutRecords.Add(OutRecord);
            await Task.FromResult(OutRecord);
        }
    }
}
