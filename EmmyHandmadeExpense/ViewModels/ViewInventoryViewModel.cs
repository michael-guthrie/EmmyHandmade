namespace AssetManager.ViewModels
{
    using GalaSoft.MvvmLight.Command;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using System.Threading.Tasks;

    public class ViewInventoryViewModel : ExpenseViewModelBase
    {
        public class InventoryGridItem
        {
            public int Id { get; set; }

            public string Name { get; set; }

            public decimal? Quantity { get; set; }

            [Display(Name = "Units")]
            public string UnitOfMeasure { get; set; }

            public decimal? Value { get; set; }
        }

        public IEnumerable<InventoryGridItem> Items
        {
            get => _Items;
            private set
            {
                if (value != _Items)
                {
                    _Items = value;
                    RaisePropertyChanged(nameof(Items));
                }
            }
        }
        private IEnumerable<InventoryGridItem> _Items;

        public DateTime InventoryAsOfDate
        {
            get => _InventoryAsOfDate;
            set
            {
                if (value != _InventoryAsOfDate)
                {
                    _InventoryAsOfDate = value;
                    RaisePropertyChanged(nameof(InventoryAsOfDate));
                }
            }
        }
        private DateTime _InventoryAsOfDate;

        public decimal TotalValue
        {
            get => _TotalValue;
            set
            {
                if (value != _TotalValue)
                {
                    _TotalValue = value;
                    RaisePropertyChanged(nameof(TotalValue));
                }
            }
        }
        private decimal _TotalValue;

        public RelayCommand CalculateAtDate { get; private set; }

        public RelayCommand PrintCommand { get; private set; }

        public event EventHandler Printing;

        public ViewInventoryViewModel()
        {
            _InventoryAsOfDate = DateTime.Today;
            CalculateAtDate = new RelayCommand(() => base.InitializeBackground());
            PrintCommand = new RelayCommand(OnPrint);
            MessengerInstance.Register<IEnumerable<int>>(this, Helpers.MessengerToken.Item, i => base.InitializeBackground());
        }

        protected override async Task InitializeAsync()
        {
            Items = await Task.Run(() => LoadInventory());
            TotalValue = Items.Sum(i => i.Value ?? 0M);
        }

        private List<InventoryGridItem> LoadInventory()
        {
            try
            {
                var results = new List<InventoryGridItem>();
                foreach (var i in DataContext.InventoryItems.OrderBy(i => i.Name).ToList())
                {
                    var gridItem = LoadGridItemFromDataItem(i);
                    results.Add(gridItem);
                }
                return results;
            }
            catch (Exception ex)
            {
                MessengerInstance.Send<Exception>(ex);
                return null;
            }
        }

        private InventoryGridItem LoadGridItemFromDataItem(Data.InventoryItem i)
        {
            InventoryGridItem currentItem = new InventoryGridItem()
                {
                    Id = i.Id,
                    Name = i.Name,
                };

            decimal quantity, value;
            if (Helpers.InventoryCalculationHelper.TryGetTotalAtDate(i, DataContext, InventoryAsOfDate, out quantity, out value))
            {
                currentItem.Quantity = quantity;
                currentItem.UnitOfMeasure = i.UnitOfMeasure.Name;
                currentItem.Value = value;
            }
            else
            {
                currentItem.UnitOfMeasure = "Conversion Error";
            }
            return currentItem;
        }

        private void OnPrint()
        {
            Printing?.Invoke(this, EventArgs.Empty);
        }
    }
}
