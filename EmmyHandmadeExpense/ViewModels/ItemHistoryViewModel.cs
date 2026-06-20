namespace AssetManager.ViewModels
{
    using AssetManager.Data;
    using GalaSoft.MvvmLight;
    using System.Collections.Generic;

    public class ItemHistoryViewModel : ViewModelBase
    {
        public IEnumerable<HistoryItemViewModel> Items
        {
            get => _Items;
            set
            {
                _Items = value;
                RaisePropertyChanged(nameof(Items));
            }
        }
        private IEnumerable<HistoryItemViewModel> _Items;
    }

    public class HistoryItemViewModel : ViewModelBase
    {
        public decimal AdjustedQuantity
        {
            get => _AdjustedQuantity;
            set
            {
                if (value != _AdjustedQuantity)
                {
                    _AdjustedQuantity = value;
                    RaisePropertyChanged(nameof(AdjustedQuantity));
                }
            }
        }
        private decimal _AdjustedQuantity;
        public decimal AdjustedUnitPrice
        {
            get => _AdjustedUnitPrice;
            set
            {
                if (value != _AdjustedUnitPrice)
                {
                    _AdjustedUnitPrice = value;
                    RaisePropertyChanged(nameof(AdjustedUnitPrice));
                }
            }
        }
        private decimal _AdjustedUnitPrice;
        public OrderItem Item
        {
            get => _Item;
            set
            {
                if (value != _Item)
                {
                    _Item = value;
                    RaisePropertyChanged(nameof(Item));
                }
            }
        }
        private OrderItem _Item;
    }
}
