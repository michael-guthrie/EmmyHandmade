namespace AssetManager.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class InventoryItemHistoryViewModel : ExpenseViewModelBase
    {
        public int InventoryItemId { get; }

        public string InventoryItemName { get; private set; }

        public IEnumerable<InventoryItemHistoryEntry> ChangeEntries { get; private set; }

        public InventoryItemHistoryViewModel(int inventoryItemId)
        {
            InventoryItemId = inventoryItemId;
        }

        protected override async Task InitializeAsync()
        {
            var item = await Task.Run(() => DataContext.InventoryItems.Single(i => i.Id == InventoryItemId));
            InventoryItemName = item.Name;

            var conversions = await Task.Run(() => DataContext.UnitConversions.Where(c => c.UnitToId == item.UnitOfMeasureId).ToList());
            var entryHelper = new InventoryItemHistoryEntryHelper(item.UnitOfMeasureId, conversions);

            var itemsIn = await Task.Run(() => DataContext.OrderItems.Where(i => i.InventoryItemId == InventoryItemId && i.Order.DateReceived != null)
                .Select(i => new { i.Quantity, i.Order.DateReceived, i.UnitOfMeasureId, UnitOfMeasureName = i.UnitOfMeasure.Name }).ToList()
                .Select(i => entryHelper.CreateInput(i.Quantity, i.DateReceived.Value, i.UnitOfMeasureId, i.UnitOfMeasureName)));
            var itemsUsed = await Task.Run(() => DataContext.BatchItems.Where(i => i.InventoryItemId == InventoryItemId)
                .Select(i => new { i.Quantity, i.Batch.BatchDate, i.UnitOfMeasureId, UnitOfMeasureName = i.UnitOfMeasure.Name }).ToList()
                .Select(i => entryHelper.CreateOutput(i.Quantity, i.BatchDate, i.UnitOfMeasureId, i.UnitOfMeasureName)));
            var itemsLost = await Task.Run(() => DataContext.BatchLossItems.Where(i => i.InventoryItemId == InventoryItemId)
                .Select(i => new { i.Quantity, i.Batch.BatchDate, i.UnitOfMeasureId, UnitOfMeasureName = i.UnitOfMeasure.Name }).ToList()
                .Select(i => entryHelper.CreateOutput(i.Quantity, i.BatchDate, i.UnitOfMeasureId, i.UnitOfMeasureName)));
            var itemsOut = await Task.Run(() => DataContext.OutRecordMaterials.Where(i => i.InventoryItemId == InventoryItemId)
                .Select(i => new { i.Quantity, OutDate = i.OutRecord.Date, i.InventoryItem.UnitOfMeasureId, UnitOfMeasureName = i.InventoryItem.UnitOfMeasure.Name }).ToList()
                .Select(i => entryHelper.CreateOutput(i.Quantity, i.OutDate, i.UnitOfMeasureId, i.UnitOfMeasureName)));

            ChangeEntries = itemsIn.Concat(itemsUsed).Concat(itemsLost).Concat(itemsOut).OrderBy(e => e.ChangeDate).ToList();

            decimal qtyToDate = 0M;
            foreach (var entry in ChangeEntries)
            {
                if (!entry.QuantityChange.HasValue) break;

                qtyToDate += entry.QuantityChange.Value;
                entry.QuantityToDate = qtyToDate;
            }

            RaisePropertyChanged(nameof(InventoryItemName));
            RaisePropertyChanged(nameof(ChangeEntries));
        }

        private sealed class InventoryItemHistoryEntryHelper
        {
            private readonly int TargetUoMId;
            private readonly IEnumerable<Data.UnitConversion> ApplicableConversions;

            public InventoryItemHistoryEntryHelper(int targetUoMId, IEnumerable<Data.UnitConversion> applicableConversions)
            {
                TargetUoMId = targetUoMId;
                ApplicableConversions = applicableConversions;
            }

            public InventoryItemHistoryEntry CreateInput(decimal quantity, DateTime inputDate, int unitOfMeasureId, string unitOfMeasureName)
            {
                if (unitOfMeasureId == TargetUoMId)
                {
                    return new InventoryItemHistoryEntry(quantity, inputDate, unitOfMeasureName);
                }
                else
                {
                    decimal? conversionFactor = ApplicableConversions.SingleOrDefault(c => c.UnitFromId == unitOfMeasureId)?.Factor;
                    return new InventoryItemHistoryEntry(quantity * conversionFactor, inputDate, unitOfMeasureName);
                }
            }

            public InventoryItemHistoryEntry CreateOutput(decimal quantity, DateTime outputDate, int unitOfMeasureId, string unitOfMeasureName)
            {
                return CreateInput(-1M * quantity, outputDate, unitOfMeasureId, unitOfMeasureName);
            }
        }
    }

    public class InventoryItemHistoryEntry : GalaSoft.MvvmLight.ViewModelBase
    {
        public decimal? QuantityChange { get; }
        public decimal? QuantityToDate
        {
            get => _QuantityToDate;
            set
            {
                if (value != _QuantityToDate)
                {
                    _QuantityToDate = value;
                    RaisePropertyChanged(nameof(QuantityToDate));
                }
            }
        }
        private decimal? _QuantityToDate;
        public DateTime ChangeDate { get; }
        private string UnitOfMeasureName { get; }

        public InventoryItemHistoryEntry(decimal? quantityChange, DateTime changeDate, string unitOfMeasureName)
        {
            QuantityChange = quantityChange;
            ChangeDate = changeDate;
            UnitOfMeasureName = unitOfMeasureName;
        }
    }
}
