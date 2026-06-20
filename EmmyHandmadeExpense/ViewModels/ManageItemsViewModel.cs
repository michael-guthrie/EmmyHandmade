namespace AssetManager.ViewModels
{
    using AssetManager.Data;
    using GalaSoft.MvvmLight.Command;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading.Tasks;

    public class ManageItemsViewModel : ExpenseViewModelBase
    {
        public ObservableCollection<InventoryItem> Items { get; private set; }

        public RelayCommand SaveCommand { get; private set; }

        private bool CanSave
        {
            get => _CanSave;
            set
            {
                if (value != _CanSave)
                {
                    _CanSave = value;
                    SaveCommand.RaiseCanExecuteChanged();
                }
            }
        }
        private bool _CanSave = true;

        public event EventHandler SaveStarted;
        public event EventHandler SaveCompleted;
        public event EventHandler<Helpers.ValidationEventArgs> ValidationFailed;

        public ManageItemsViewModel()
        {
            SaveCommand = new RelayCommand(OnSave, () => CanSave);
        }

        protected override async Task InitializeAsync()
        {
            Items = new ObservableCollection<InventoryItem>(
                await Task.Run(() => DataContext.InventoryItems.ToList()));
            Items.CollectionChanged += Items_CollectionChanged;
            RaisePropertyChanged(nameof(Items));
        }

        private void Items_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    foreach (var i in e.NewItems)
                    {
                        var invItem = (InventoryItem)i;
                        if (invItem.UnitOfMeasureId == 0)
                        {
                            // Default to ounces weight.
                            invItem.UnitOfMeasureId = 102;
                        }
                        DataContext.InventoryItems.Add(invItem);
                    }
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                    foreach (var i in e.OldItems)
                    {
                        DataContext.InventoryItems.Remove((InventoryItem)i);
                    }
                    break;
            }
        }

        private async void OnSave()
        {
            CanSave = false;
            SaveStarted?.Invoke(this, EventArgs.Empty);

            try
            {
                // Check that all items are valid.
                string valErrors = await Task.Run(() =>
                    {
                        var validUnits = DataContext.UnitsOfMeasure.Select(u => u.Id).ToList();
                        var sb = new System.Text.StringBuilder();
                        foreach (var i in Items)
                        {
                            if (!validUnits.Contains(i.UnitOfMeasureId))
                            {
                                sb.AppendLine($"You need to select valid Units for {i.Name}.");
                            }
                            if ((i.AlternateUnitOfMeasureFactor.HasValue && !i.AlternateUnitOfMeasureId.HasValue) ||
                                (!i.AlternateUnitOfMeasureFactor.HasValue && i.AlternateUnitOfMeasureId.HasValue))
                            {
                                sb.AppendLine(
                                    $"For alternate unit conversion of {i.Name} you must provide both Alternate Units and Converstion Factor.");
                            }
                        }
                        if (Items.Any(i => string.IsNullOrEmpty(i.Name)))
                        {
                            sb.AppendLine("You have one or more items missing a name.");
                        }
                        if (Items.Count != Items.Select(i => i.Name).Distinct().Count())
                        {
                            sb.AppendLine("You have one or more items with a duplicate name.");
                        }
                        return sb.ToString();
                    });
                if (valErrors.Length > 0)
                {
                    ValidationFailed?.Invoke(this, new Helpers.ValidationEventArgs(valErrors));
                    return;
                }

                try
                {
                    await DataContext.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    MessengerInstance.Send<Exception>(ex);
                    return;
                }
                MessengerInstance.Send<IEnumerable<int>>(Items.Select(i => i.Id), Helpers.MessengerToken.Item);
                SaveCompleted?.Invoke(this, EventArgs.Empty);
            }
            finally
            {
                CanSave = true;
            }
        }
    }
}
