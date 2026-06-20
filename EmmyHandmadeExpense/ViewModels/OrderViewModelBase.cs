namespace AssetManager.ViewModels
{
    using AssetManager.Data;
    using GalaSoft.MvvmLight;
    using GalaSoft.MvvmLight.Command;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;

    public abstract class OrderViewModelBase: ExpenseViewModelBase
    {
        public class OrderItemWrapper : ViewModelBase
        {
            public Data.TrackingStore DataContext { get; set; }
            public OrderItem Item { get; }
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
                            OrderUOM = item.UnitOfMeasureId;
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
                        RaisePropertyChanged(nameof(Cost));
                    }
                }
            }
            public int OrderUOM
            {
                get => Item.UnitOfMeasureId;
                set
                {
                    if (Item.UnitOfMeasureId != value)
                    {
                        Item.UnitOfMeasureId = value;
                        RaisePropertyChanged(nameof(OrderUOM));
                    }
                }
            }
            public decimal UnitPrice
            {
                get => Item.UnitPrice;
                set
                {
                    if (Item.UnitPrice != value)
                    {
                        Item.UnitPrice = value;
                        RaisePropertyChanged(nameof(UnitPrice));
                        RaisePropertyChanged(nameof(Cost));
                    }
                }
            }
            public decimal Cost
            {
                get => Quantity * UnitPrice;
                set
                {
                    if (Cost != value)
                    {
                        Item.UnitPrice = (Quantity != 0M) ? (value / Quantity) : 0M;
                        RaisePropertyChanged(nameof(UnitPrice));
                        RaisePropertyChanged(nameof(Cost));
                    }
                }
            }

            public OrderItemWrapper() : this(new OrderItem()) { }

            public OrderItemWrapper(OrderItem wrappedItem)
            {
                Item = wrappedItem;
            }
        }

        public abstract string Title { get; }

        public Order Order
        {
            get => _Order;
            protected set
            {
                if (value != _Order)
                {
                    _Order = value;
                    RaisePropertyChanged(nameof(Order));
                    RaisePropertyChanged(nameof(DatePlaced));
                    RaisePropertyChanged(nameof(DateReceived));
                    RaisePropertyChanged(nameof(Link));
                    RaisePropertyChanged(nameof(OrderNumber));
                    RaisePropertyChanged(nameof(Shipping));
                    RaisePropertyChanged(nameof(Source));
                    RaisePropertyChanged(nameof(Tax));
                    RaisePropertyChanged(nameof(Discount));
                    RaisePropertyChanged(nameof(TotalCost));
                }
            }
        }
        private Order _Order;

        public DateTime? DatePlaced
        {
            get => _Order?.DatePlaced;
            set
            {
                if (_Order == null) return;
                if (_Order.DatePlaced != value)
                {
                    _Order.DatePlaced = value.GetValueOrDefault();
                    RaisePropertyChanged(nameof(DatePlaced));
                }
            }
        }

        public DateTime? DateReceived
        {
            get => _Order?.DateReceived;
            set
            {
                if (_Order == null) return;
                if (_Order.DateReceived != value)
                {
                    _Order.DateReceived = value;
                    RaisePropertyChanged(nameof(DateReceived));
                }
            }
        }

        public string Link
        {
            get => _Order?.Link;
            set
            {
                if (_Order == null) return;
                if (_Order.Link != value)
                {
                    _Order.Link = value;
                    RaisePropertyChanged(nameof(Link));
                }
            }
        }

        public string OrderNumber
        {
            get => _Order?.OrderNumber;
            set
            {
                if (_Order == null) return;
                if (_Order.OrderNumber != value)
                {
                    _Order.OrderNumber = value;
                    RaisePropertyChanged(nameof(OrderNumber));
                }
            }
        }

        public decimal? Shipping
        {
            get => _Order?.Shipping;
            set
            {
                if (_Order == null) return;
                if (_Order.Shipping != value)
                {
                    _Order.Shipping = value;
                    RaisePropertyChanged(nameof(Shipping));
                    RaisePropertyChanged(nameof(TotalCost));
                }
            }
        }

        public string Source
        {
            get => _Order?.Source;
            set
            {
                if (_Order == null) return;
                if (_Order.Source != value)
                {
                    _Order.Source = value;
                    RaisePropertyChanged(nameof(Source));
                }
            }
        }

        public decimal? Tax
        {
            get => _Order?.Tax;
            set
            {
                if (_Order == null) return;
                if (_Order.Tax != value)
                {
                    _Order.Tax = value;
                    RaisePropertyChanged(nameof(Tax));
                    RaisePropertyChanged(nameof(TotalCost));
                }
            }
        }

        public decimal? Discount
        {
            get => _Order?.Discount;
            set
            {
                if (_Order == null) return;
                if (_Order.Discount != value)
                {
                    _Order.Discount = value;
                    RaisePropertyChanged(nameof(Discount));
                    RaisePropertyChanged(nameof(TotalCost));
                }
            }
        }

        public decimal? TotalCost
        {
            get
            {
                return (Items.Any() || Shipping.HasValue || Tax.HasValue || Discount.HasValue)
                    ? Items.Sum(i => (decimal?)i.Cost).GetValueOrDefault() + Shipping.GetValueOrDefault() + Tax.GetValueOrDefault() - Discount.GetValueOrDefault()
                    : (decimal?)null;
            }
        }

        public ObservableCollection<OrderItemWrapper> Items { get; }

        public RelayCommand SaveCommand { get; }

        public event EventHandler SaveCompleted;

        protected OrderViewModelBase()
        {
            SaveCommand = new RelayCommand(OnSave, CanSave);
            Items = new ObservableCollection<OrderItemWrapper>();
            Items.CollectionChanged += OnItemsCollectionChanged;
        }

        protected virtual void OnItemsCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    foreach (var i in e.NewItems)
                    {
                        var w = (OrderItemWrapper)i;
                        w.DataContext = DataContext;
                        w.PropertyChanged += (wSender, wEvt) => { if (wEvt.PropertyName == nameof(OrderItemWrapper.Cost)) { RaisePropertyChanged(nameof(TotalCost)); } };
                        Order.OrderItems.Add(w.Item);
                    }
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                    foreach (var i in e.OldItems)
                    {
                        var w = (OrderItemWrapper)i;
                        w.PropertyChanged -= (wSender, wEvt) => { if (wEvt.PropertyName == nameof(OrderItemWrapper.Cost)) { RaisePropertyChanged(nameof(TotalCost)); } };
                        DataContext.OrderItems.Remove(w.Item);
                        Order.OrderItems.Remove(w.Item);
                        RaisePropertyChanged(nameof(TotalCost));
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
            MessengerInstance.Send<IEnumerable<DateTime>>(new[] { Order.DatePlaced }, Helpers.MessengerToken.Expense);
            MessengerInstance.Send<IEnumerable<int>>(Items.Select(i => i.InventoryItemId), Helpers.MessengerToken.Item);
            SaveCompleted?.Invoke(this, EventArgs.Empty);
        }

        protected virtual bool CanSave() => true;
    }
}
