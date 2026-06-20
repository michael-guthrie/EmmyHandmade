namespace AssetManager.ViewModels
{
    using AssetManager.Data;
    using GalaSoft.MvvmLight;
    using GalaSoft.MvvmLight.Command;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading.Tasks;

    public abstract class BatchViewModelBase : ExpenseViewModelBase
    {
        public interface IBatchItemWrapperCost
        {
            int InventoryItemId { get; }
            decimal Quantity { get; }
            int UnitOfMeasureId { get; }
            decimal? Cost { get; }
            string CostDisplay { get; set; }
            ItemHistoryViewModel CostHistory { get; set; }
            IBatchItem WrappedItem { get; }
        }
        public class BatchItemWrapper<T> : ViewModelBase, IBatchItemWrapperCost where T : class, IBatchItem, new()
        {
            public TrackingStore DataContext { get; set; }
            public T Item { get; }
            public IBatchItem WrappedItem => Item;
            public int InventoryItemId
            {
                get => Item.InventoryItemId;
                set
                {
                    if (Item.InventoryItemId != value)
                    {
                        Item.InventoryItemId = value;
                        RaisePropertyChanged(nameof(InventoryItemId));

                        // Set units to the preferred for the item as default.
                        var item = DataContext.InventoryItems.SingleOrDefault(i => i.Id == value);
                        if (item != null)
                        {
                            UnitOfMeasureId = item.UnitOfMeasureId;
                        }
                    }
                }
            }
            public decimal Quantity
            {
                get => Item.Quantity;
                set
                {
                    if (Item.Quantity != value)
                    {
                        Item.Quantity = value;
                        RaisePropertyChanged(nameof(Quantity));
                        CalculateDerivedProperties();
                    }
                }
            }
            public int UnitOfMeasureId
            {
                get => Item.UnitOfMeasureId;
                set
                {
                    if (Item.UnitOfMeasureId != value)
                    {
                        Item.UnitOfMeasureId = value;
                        RaisePropertyChanged(nameof(UnitOfMeasureId));
                        CalculateDerivedProperties();
                    }
                }
            }
            public decimal? Percent
            {
                get => _Percent;
                set
                {
                    if (value != _Percent)
                    {
                        _Percent = value;
                        RaisePropertyChanged(nameof(Percent));
                    }
                }
            }
            private decimal? _Percent;
            public string CostDisplay
            {
                get => _CostDisplay;
                set
                {
                    if (value != _CostDisplay)
                    {
                        _CostDisplay = value;
                        RaisePropertyChanged(nameof(CostDisplay));
                    }
                }
            }
            private string _CostDisplay;
            public decimal? Cost => CostHistory?.Items.Sum(history => history.AdjustedQuantity * history.AdjustedUnitPrice);
            public ItemHistoryViewModel CostHistory
            {
                get => _CostHistory;
                set
                {
                    if (value != _CostHistory)
                    {
                        _CostHistory = value;
                        RaisePropertyChanged(nameof(CostHistory));
                        RaisePropertyChanged(nameof(Cost));
                    }
                }
            }
            private ItemHistoryViewModel _CostHistory;

            public BatchItemWrapper() : this(new T()) { }

            public BatchItemWrapper(T wrappedItem)
            {
                Item = wrappedItem;
            }

            private void CalculateDerivedProperties()
            {
                MessengerInstance.Send<IBatchItemWrapperCost>(this, DataContext);
            }
        }

        public abstract string Title { get; }

        protected Batch Batch
        {
            get => _Batch;
            set
            {
                if (value != _Batch)
                {
                    _Batch = value;
                    RaisePropertyChanged(nameof(Batch));
                    RaisePropertyChanged(nameof(BatchDate));
                    RaisePropertyChanged(nameof(Description));
                    RaisePropertyChanged(nameof(ProductId));
                    RaisePropertyChanged(nameof(UnitsProduced));
                }
            }
        }
        private Batch _Batch;

        public int? ProductId
        {
            get => _Batch?.ProductId;
            set
            {
                if (_Batch == null) return;
                if (_Batch.ProductId != value)
                {
                    _Batch.ProductId = value.GetValueOrDefault();
                    RaisePropertyChanged(nameof(ProductId));
                }
            }
        }

        public DateTime? BatchDate
        {
            get => _Batch?.BatchDate;
            set
            {
                if (_Batch == null) return;
                if (_Batch.BatchDate != value)
                {
                    _Batch.BatchDate = value.GetValueOrDefault();
                    RaisePropertyChanged(nameof(BatchDate));
                    Task.Run(() => UpdateItemCosts().Wait());
                }
            }
        }

        public string Description
        {
            get => _Batch?.Description;
            set
            {
                if (_Batch == null) return;
                if (_Batch.Description != value)
                {
                    _Batch.Description = value;
                    RaisePropertyChanged(nameof(Description));
                }
            }
        }

        public decimal? UnitsProduced
        {
            get => _Batch?.UnitsProduced;
            set
            {
                if (_Batch == null) return;
                if (_Batch.UnitsProduced != value)
                {
                    _Batch.UnitsProduced = value;
                    RaisePropertyChanged(nameof(UnitsProduced));
                    UpdateAggregateCosts(ItemsType.None);
                }
            }
        }

        public string UsageCostDisplay { get; private set; }
        public string LossCostDisplay { get; private set; }
        public string TotalCostDisplay { get; private set; }
        public string PerUnitCostDisplay { get; private set; }

        public ObservableCollection<BatchItemWrapper<BatchItem>> UsedItems { get; private set; }

        public ObservableCollection<BatchItemWrapper<BatchLossItem>> LossItems { get; private set; }

        public RelayCommand SaveCommand { get; }

        public event EventHandler SaveCompleted;

        protected BatchViewModelBase()
        {
            SaveCommand = new RelayCommand(OnSave, CanSave);
            UsedItems = new ObservableCollection<BatchItemWrapper<BatchItem>>();
            UsedItems.CollectionChanged += OnUsedItemsCollectionChanged;
            LossItems = new ObservableCollection<BatchItemWrapper<BatchLossItem>>();
            LossItems.CollectionChanged += OnLossItemsCollectionChanged;
            MessengerInstance.Register<IBatchItemWrapperCost>(this, DataContext, async listItem =>
                {
                    await CalculateItemCost(listItem);
                    if (listItem.WrappedItem is BatchItem)
                    {
                        await UpdateUsedItemPercentages();
                    }
                });
        }

        protected virtual void OnUsedItemsCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    foreach (var i in e.NewItems)
                    {
                        var w = (BatchItemWrapper<BatchItem>)i;
                        w.DataContext = DataContext;
                        w.PropertyChanged += (wSender, wEvt) => { if (wEvt.PropertyName == nameof(IBatchItemWrapperCost.CostDisplay)) { UpdateAggregateCosts(ItemsType.Used); } };
                        Batch.BatchItems.Add(w.Item);
                    }
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                    foreach (var i in e.OldItems)
                    {
                        var w = (BatchItemWrapper<BatchItem>)i;
                        w.PropertyChanged -= (wSender, wEvt) => { if (wEvt.PropertyName == nameof(IBatchItemWrapperCost.CostDisplay)) { UpdateAggregateCosts(ItemsType.Used); } };
                        DataContext.BatchItems.Remove(w.Item);
                        Batch.BatchItems.Remove(w.Item);
                        UpdateAggregateCosts(ItemsType.Used);
                    }
                    break;
            }
        }

        protected virtual void OnLossItemsCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    foreach (var i in e.NewItems)
                    {
                        var w = (BatchItemWrapper<BatchLossItem>)i;
                        w.DataContext = DataContext;
                        w.PropertyChanged += (wSender, wEvt) => { if (wEvt.PropertyName == nameof(IBatchItemWrapperCost.CostDisplay)) { UpdateAggregateCosts(ItemsType.Loss); } };
                        Batch.BatchLossItems.Add(w.Item);
                    }
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                    foreach (var i in e.OldItems)
                    {
                        var w = (BatchItemWrapper<BatchLossItem>)i;
                        w.PropertyChanged -= (wSender, wEvt) => { if (wEvt.PropertyName == nameof(IBatchItemWrapperCost.CostDisplay)) { UpdateAggregateCosts(ItemsType.Loss); } };
                        DataContext.BatchLossItems.Remove(w.Item);
                        Batch.BatchLossItems.Remove(w.Item);
                        UpdateAggregateCosts(ItemsType.Loss);
                    }
                    break;
            }
        }

        protected virtual async void OnSave()
        {
            try
            {
                await DataContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                MessengerInstance.Send<Exception>(ex);
                return;
            }
            MessengerInstance.Send(LossItems.Select(i => i.InventoryItemId).Concat(UsedItems.Select(i => i.InventoryItemId)), Helpers.MessengerToken.Item);
            SaveCompleted?.Invoke(this, EventArgs.Empty);
        }

        protected virtual bool CanSave() => true;

        protected async Task UpdateItemCosts()
        {
            foreach (var i in UsedItems)
            {
                await CalculateItemCost(i);
            }
            foreach (var i in LossItems)
            {
                await CalculateItemCost(i);
            }
        }

        private async Task CalculateItemCost(IBatchItemWrapperCost item)
        {
            if (!BatchDate.HasValue) { item.CostHistory = null; item.CostDisplay = null; return; }
            if (item.Quantity == 0M) { item.CostHistory = null; item.CostDisplay = "0.00"; return; }

            var result = await Task.Run<(ItemHistoryViewModel History, string CostDisplay)>(() =>
                {
                    try
                    {
                        // Load the item in question.
                        InventoryItem i = DataContext.InventoryItems.SingleOrDefault(ii => ii.Id == item.InventoryItemId);
                        if (i == null) return (null, "Select an item");

                        // Get the quantity of usage in the default units for the item.
                        decimal qtyForValue;
                        if (item.UnitOfMeasureId == i.UnitOfMeasureId)
                        {
                            qtyForValue = item.Quantity;
                        }
                        else
                        {
                            decimal conversionFactor;
                            var conversion = DataContext.UnitConversions.SingleOrDefault(c => c.UnitFromId == item.UnitOfMeasureId && c.UnitToId == i.UnitOfMeasureId);
                            if (conversion != null)
                            {
                                conversionFactor = conversion.Factor;
                            }
                            else
                            {
                                // Check if there is a conversion between item groups.
                                if (i.AlternateUnitOfMeasureId.HasValue)
                                {
                                    if (item.UnitOfMeasureId == i.AlternateUnitOfMeasureId)
                                    {
                                        conversionFactor = i.AlternateUnitOfMeasureFactor.Value;
                                    }
                                    else
                                    {
                                        conversion = DataContext.UnitConversions.Single(c => c.UnitFromId == item.UnitOfMeasureId && c.UnitToId == i.AlternateUnitOfMeasureId);
                                        conversionFactor = conversion.Factor * i.AlternateUnitOfMeasureFactor.Value;
                                    }
                                }
                                else
                                {
                                    return (null, "Conversion Error");
                                }
                            }
                            qtyForValue = item.Quantity * conversionFactor;
                        }

                        // Find all the previously used quantity for the item.
                        decimal usedItemQty = 0M;
                        foreach (var bi in DataContext.Batches.Where(b => b.BatchDate < BatchDate.Value).SelectMany(b => b.BatchItems).Where(bi => bi.InventoryItemId == item.InventoryItemId).ToList())
                        {
                            decimal adjustedQuantity;
                            if (bi.UnitOfMeasureId == i.UnitOfMeasureId)
                            {
                                adjustedQuantity = bi.Quantity;
                            }
                            else
                            {
                                decimal conversionFactor;
                                var conversion = DataContext.UnitConversions.SingleOrDefault(c => c.UnitFromId == bi.UnitOfMeasureId && c.UnitToId == i.UnitOfMeasureId);
                                if (conversion != null)
                                {
                                    conversionFactor = conversion.Factor;
                                }
                                else
                                {
                                    // Check if there is a conversion between item groups.
                                    if (i.AlternateUnitOfMeasureId.HasValue)
                                    {
                                        if (bi.UnitOfMeasureId == i.AlternateUnitOfMeasureId)
                                        {
                                            conversionFactor = i.AlternateUnitOfMeasureFactor.Value;
                                        }
                                        else
                                        {
                                            conversion = DataContext.UnitConversions.Single(c => c.UnitFromId == bi.UnitOfMeasureId && c.UnitToId == i.AlternateUnitOfMeasureId);
                                            conversionFactor = conversion.Factor * i.AlternateUnitOfMeasureFactor.Value;
                                        }
                                    }
                                    else
                                    {
                                        return (null, "Conversion Error");
                                    }
                                }
                                adjustedQuantity = bi.Quantity * conversionFactor;
                            }
                            usedItemQty += adjustedQuantity;
                        }
                        foreach (var bli in DataContext.Batches.Where(b => b.BatchDate < BatchDate.Value).SelectMany(b => b.BatchLossItems).Where(bi => bi.InventoryItemId == item.InventoryItemId).ToList())
                        {
                            decimal adjustedQuantity;
                            if (bli.UnitOfMeasureId == i.UnitOfMeasureId)
                            {
                                adjustedQuantity = bli.Quantity;
                            }
                            else
                            {
                                decimal conversionFactor;
                                var conversion = DataContext.UnitConversions.SingleOrDefault(c => c.UnitFromId == bli.UnitOfMeasureId && c.UnitToId == i.UnitOfMeasureId);
                                if (conversion != null)
                                {
                                    conversionFactor = conversion.Factor;
                                }
                                else
                                {
                                    // Check if there is a conversion between item groups.
                                    if (i.AlternateUnitOfMeasureId.HasValue)
                                    {
                                        if (bli.UnitOfMeasureId == i.AlternateUnitOfMeasureId)
                                        {
                                            conversionFactor = i.AlternateUnitOfMeasureFactor.Value;
                                        }
                                        else
                                        {
                                            conversion = DataContext.UnitConversions.Single(c => c.UnitFromId == bli.UnitOfMeasureId && c.UnitToId == i.AlternateUnitOfMeasureId);
                                            conversionFactor = conversion.Factor * i.AlternateUnitOfMeasureFactor.Value;
                                        }
                                    }
                                    else
                                    {
                                        return (null, "Conversion Error");
                                    }
                                }
                                adjustedQuantity = bli.Quantity * conversionFactor;
                            }
                            usedItemQty += adjustedQuantity;
                        }

                        // Now find the value of currently used items.
                        decimal qtyAvailable = usedItemQty * -1M;
                        decimal usedValue = 0M;
                        var historyItems = new List<HistoryItemViewModel>();

                        // Process items by first grouping the same item together, then add according to date.
                        foreach (var oi in DataContext.Orders.Where(o => o.DateReceived.HasValue).OrderBy(o => o.DateReceived).SelectMany(o => o.OrderItems).Where(oi => oi.InventoryItemId == i.Id).ToList())
                        {
                            // Quantity and unit price may need to be adjusted if the order used different units.
                            decimal adjustedQuantity, adjustedUnitPrice;
                            if (oi.UnitOfMeasureId == oi.InventoryItem.UnitOfMeasureId)
                            {
                                adjustedQuantity = oi.Quantity;
                                adjustedUnitPrice = oi.UnitPrice;
                            }
                            else
                            {
                                decimal conversionFactor;
                                var conversion = DataContext.UnitConversions.SingleOrDefault(c => c.UnitFromId == oi.UnitOfMeasureId && c.UnitToId == i.UnitOfMeasureId);
                                if (conversion != null)
                                {
                                    conversionFactor = conversion.Factor;
                                }
                                else
                                {
                                    // Check if there is a conversion between item groups.
                                    if (i.AlternateUnitOfMeasureId.HasValue)
                                    {
                                        if (oi.UnitOfMeasureId == i.AlternateUnitOfMeasureId)
                                        {
                                            conversionFactor = i.AlternateUnitOfMeasureFactor.Value;
                                        }
                                        else
                                        {
                                            conversion = DataContext.UnitConversions.Single(c => c.UnitFromId == oi.UnitOfMeasureId && c.UnitToId == i.AlternateUnitOfMeasureId);
                                            conversionFactor = conversion.Factor * i.AlternateUnitOfMeasureFactor.Value;
                                        }
                                    }
                                    else
                                    {
                                        return (null, "Conversion Error");
                                    }
                                }
                                adjustedQuantity = oi.Quantity * conversionFactor;
                                adjustedUnitPrice = oi.UnitPrice / conversionFactor;
                            }

                            // Increase the quantity according to the order and increase value if applicable.
                            qtyAvailable += adjustedQuantity;
                            if (qtyAvailable > 0M)
                            {
                                var adjustedUsedQty = Math.Min(Math.Min(qtyAvailable, adjustedQuantity), qtyForValue);
                                usedValue += adjustedUsedQty * adjustedUnitPrice;
                                historyItems.Add(new HistoryItemViewModel
                                {
                                    AdjustedQuantity = adjustedUsedQty,
                                    AdjustedUnitPrice = adjustedUnitPrice,
                                    Item = oi
                                });
                                qtyForValue -= adjustedQuantity;
                            }
                            if (qtyForValue <= 0M)
                            {
                                return (new ItemHistoryViewModel { Items = historyItems }, usedValue.ToString("N2"));
                            }
                        }
                        return (null, "Not found");
                    }
                    catch (Exception ex)
                    {
                        MessengerInstance.Send<Exception>(ex);
                        return (null, "Error, try again.");
                    }
                });
            item.CostHistory = result.History;
            item.CostDisplay = result.CostDisplay;
        }

        protected async Task UpdateUsedItemPercentages()
        {
            decimal? percentCalcTotal = await Task.Run(() =>
                {
                    if (!UsedItems.Any()) return null;
                    if (UsedItems.Select(i => i.UnitOfMeasureId).Distinct().Count() != 1) return null;

                    try
                    {
                        // Check if the batch is using oils, thus will calculate by items marked for oil percent calculation.
                        var oilIds = DataContext.InventoryItems.Where(i => i.IsPercentBatchOil).Select(i => i.Id).ToList();
                        if (UsedItems.Any(i => oilIds.Contains(i.InventoryItemId)))
                        {
                            return (decimal?)(UsedItems.Where(i => oilIds.Contains(i.InventoryItemId)).Sum(i => i.Quantity) / 100M);
                        }
                        else
                        {
                            // No oils; return as percent of total batch.
                            return (decimal?)(UsedItems.Sum(i => i.Quantity) / 100M);
                        }
                    }
                    catch
                    {
                        return null;
                    }
                });
            foreach (var i in UsedItems)
            {
                i.Percent = percentCalcTotal.GetValueOrDefault() > 0M ? i.Quantity / percentCalcTotal.Value : (decimal?)null;
            }
        }

        private class ItemsCost
        {
            public decimal TotalCost { get; set; }
            public bool IsComplete { get; set; }
        }

        private static ItemsCost FindItemsCost(IEnumerable<IBatchItemWrapperCost> items)
        {
            if (!items.Any()) return null;

            return new ItemsCost()
                {
                    IsComplete = items.All(i => i.Cost.HasValue),
                    TotalCost = items.Sum(i => i.Cost.GetValueOrDefault()),
                };
        }

        [Flags]
        protected enum ItemsType { None, Used = 1, Loss = 2, Both = 3 }

        protected void UpdateAggregateCosts(ItemsType iType)
        {
            ItemsCost used = FindItemsCost(UsedItems),
                      loss = FindItemsCost(LossItems);

            if (iType.HasFlag(ItemsType.Loss))
            {
                if (loss == null)
                {
                    LossCostDisplay = null;
                }
                else
                {
                    LossCostDisplay = loss.TotalCost.ToString("N2");
                    if (!loss.IsComplete)
                    {
                        LossCostDisplay += " (incomplete)";
                    }
                }
                RaisePropertyChanged(nameof(LossCostDisplay));
            }
            if (iType.HasFlag(ItemsType.Used))
            {
                if (used == null)
                {
                    UsageCostDisplay = null;
                }
                else
                {
                    UsageCostDisplay = used.TotalCost.ToString("N2");
                    if (!used.IsComplete)
                    {
                        UsageCostDisplay += " (incomplete)";
                    }
                }
                RaisePropertyChanged(nameof(UsageCostDisplay));
            }

            if ((loss == null) && (used == null))
            {
                Batch.Cost = null;
                TotalCostDisplay = null;
                PerUnitCostDisplay = null;
            }
            else
            {
                var totalCost = ((loss?.TotalCost).GetValueOrDefault() + (used?.TotalCost).GetValueOrDefault());
                Batch.Cost = totalCost;
                TotalCostDisplay = totalCost.ToString("N2");
                if ((loss?.IsComplete == false) || (used?.IsComplete == false))
                {
                    TotalCostDisplay += " (incomplete)";
                }
                decimal units = UnitsProduced.GetValueOrDefault();
                if (units != 0M)
                {
                    PerUnitCostDisplay = (totalCost / units).ToString("0.00##");
                    if ((loss?.IsComplete == false) || (used?.IsComplete == false))
                    {
                        PerUnitCostDisplay += " (incomplete)";
                    }
                }
                else
                {
                    PerUnitCostDisplay = null;
                }
            }
            RaisePropertyChanged(nameof(TotalCostDisplay));
            RaisePropertyChanged(nameof(PerUnitCostDisplay));
        }
    }
}
