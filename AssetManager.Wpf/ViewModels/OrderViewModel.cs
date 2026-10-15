using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using AssetManager.Core.Data;
using AssetManager.Core.Data.Entities;
using AssetManager.Infrastructure;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore;

namespace AssetManager.ViewModels;

/// <summary>
/// Add or edit an order. Replaces the <c>OrderViewModelBase</c> / <c>AddOrderViewModel</c> /
/// <c>EditOrderViewModel</c> trio: the only thing that differed between add and edit was which
/// record got loaded, so that is now a property rather than a subclass.
/// </summary>
public sealed partial class OrderViewModel : ExpenseViewModelBase
{
    private readonly LookupCache _lookups;

    /// <summary>Null for a new order; set before initialising to edit an existing one.</summary>
    public int? OrderId { get; set; }

    public string Title => OrderId is null ? "Add Order" : "View / Edit Order";

    public LookupCache Lookups => _lookups;

    public Order Order { get; private set; } = new();

    public ObservableCollection<OrderItemWrapper> Items { get; } = [];

    public event EventHandler? SaveCompleted;

    public OrderViewModel(IDbContextFactory<TrackingStore> dbFactory, LookupCache lookups)
        : base(dbFactory)
    {
        _lookups = lookups;
        Items.CollectionChanged += OnItemsCollectionChanged;
    }

    protected override async Task LoadAsync(CancellationToken ct)
    {
        if (OrderId is null)
        {
            Order = new Order { DatePlaced = DateTime.Now };
            DataContext.Orders.Add(Order);
        }
        else
        {
            Order = await DataContext.Orders
                .Include(o => o.OrderItems)
                .SingleAsync(o => o.Id == OrderId, ct);

            Items.CollectionChanged -= OnItemsCollectionChanged;
            foreach (var orderItem in Order.OrderItems.ToList())
            {
                Items.Add(CreateWrapper(orderItem));
            }
            Items.CollectionChanged += OnItemsCollectionChanged;
        }

        OnPropertyChanged(nameof(Order));
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(TotalCost));
    }

    private OrderItemWrapper CreateWrapper(OrderItem item)
    {
        var wrapper = new OrderItemWrapper(item, _lookups);
        wrapper.PropertyChanged += OnWrapperPropertyChanged;
        return wrapper;
    }

    private void OnWrapperPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(OrderItemWrapper.Cost)) OnPropertyChanged(nameof(TotalCost));
    }

    private void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems is not null)
        {
            foreach (OrderItemWrapper wrapper in e.NewItems)
            {
                wrapper.Attach(_lookups);
                wrapper.PropertyChanged += OnWrapperPropertyChanged;
                Order.OrderItems.Add(wrapper.Item);
            }
        }

        if (e.OldItems is not null)
        {
            foreach (OrderItemWrapper wrapper in e.OldItems)
            {
                wrapper.PropertyChanged -= OnWrapperPropertyChanged;
                DataContext.OrderItems.Remove(wrapper.Item);
                Order.OrderItems.Remove(wrapper.Item);
            }
        }

        OnPropertyChanged(nameof(TotalCost));
    }

    /// <summary>
    /// Null when there is nothing at all to total, so a brand new order shows a blank rather
    /// than 0.00 — the behaviour of the original.
    /// </summary>
    public decimal? TotalCost =>
        (Items.Count > 0 || Order.Shipping.HasValue || Order.Tax.HasValue || Order.Discount.HasValue)
            ? Items.Sum(i => i.Cost) + (Order.Shipping ?? 0M) + (Order.Tax ?? 0M) - (Order.Discount ?? 0M)
            : null;

    /// <summary>Called by the view when a header money field changes, to refresh the total.</summary>
    public void RefreshTotal() => OnPropertyChanged(nameof(TotalCost));

    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            await DataContext.SaveChangesAsync();

            WeakReferenceMessenger.Default.Send(new Messages.ExpensesChanged([Order.DatePlaced]));
            WeakReferenceMessenger.Default.Send(new Messages.OrdersChanged([Order.Id]));
            WeakReferenceMessenger.Default.Send(
                new Messages.ItemsChanged(Items.Select(i => i.InventoryItemId).Distinct().ToList()));

            SaveCompleted?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            ReportError(ex);
        }
    }

    /// <summary>
    /// Grid-row wrapper around an <see cref="OrderItem"/>. Kept from the original because the
    /// grid needs two behaviours the entity does not have: picking an item defaults the units,
    /// and editing the extended cost back-solves the unit price.
    /// </summary>
    public sealed partial class OrderItemWrapper : ObservableObject
    {
        private LookupCache? _lookups;

        public OrderItem Item { get; }

        public OrderItemWrapper() : this(new OrderItem(), null) { }

        public OrderItemWrapper(OrderItem item, LookupCache? lookups)
        {
            Item = item;
            _lookups = lookups;
        }

        internal void Attach(LookupCache lookups) => _lookups ??= lookups;

        public int InventoryItemId
        {
            get => Item.InventoryItemId;
            set
            {
                if (Item.InventoryItemId == value) return;
                Item.InventoryItemId = value;
                OnPropertyChanged();

                // Default the line to the item's own tracking unit.
                var defaultUnit = _lookups?.DefaultUnitIdForItem(value);
                if (defaultUnit is { } unitId) OrderUOM = unitId;

                OnPropertyChanged(nameof(DefaultUnitName));
            }
        }

        public decimal Quantity
        {
            get => Item.Quantity;
            set
            {
                if (Item.Quantity == value) return;
                Item.Quantity = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Cost));
            }
        }

        public int OrderUOM
        {
            get => Item.UnitOfMeasureId;
            set
            {
                if (Item.UnitOfMeasureId == value) return;
                Item.UnitOfMeasureId = value;
                OnPropertyChanged();
            }
        }

        public decimal UnitPrice
        {
            get => Item.UnitPrice;
            set
            {
                if (Item.UnitPrice == value) return;
                Item.UnitPrice = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Cost));
            }
        }

        /// <summary>Extended cost. Setting it back-solves the unit price.</summary>
        public decimal Cost
        {
            get => Quantity * UnitPrice;
            set
            {
                if (Cost == value) return;
                Item.UnitPrice = Quantity != 0M ? value / Quantity : 0M;
                OnPropertyChanged(nameof(UnitPrice));
                OnPropertyChanged(nameof(Cost));
            }
        }

        public string DefaultUnitName => _lookups?.ItemUnitName(InventoryItemId) ?? "N/A";
    }
}
