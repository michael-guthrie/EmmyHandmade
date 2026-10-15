using AssetManager.Core.Data;
using AssetManager.Core.Data.Entities;
using AssetManager.Infrastructure;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore;

namespace AssetManager.ViewModels;

/// <summary>The order list.</summary>
public sealed partial class OrderHistoryViewModel : ExpenseViewModelBase
{
    [ObservableProperty]
    private IReadOnlyList<OrderHistoryItem> _items = [];

    public OrderHistoryViewModel(IDbContextFactory<TrackingStore> dbFactory) : base(dbFactory)
    {
        WeakReferenceMessenger.Default.Register<Messages.OrdersChanged>(this, (_, _) => InitializeBackground());
    }

    protected override async Task LoadAsync(CancellationToken ct)
    {
        Items = await DataContext.Orders.AsNoTracking()
            .OrderByDescending(o => o.DatePlaced)
            .Select(o => new OrderHistoryItem(
                o.Id,
                o.Source,
                o.DatePlaced,
                o.DateReceived,
                o.OrderItems.Count,
                (o.Shipping ?? 0M) + (o.Tax ?? 0M)
                    + (o.OrderItems.Sum(oi => (decimal?)(oi.Quantity * oi.UnitPrice)) ?? 0M)
                    - (o.Discount ?? 0M)))
            .ToListAsync(ct);
    }

    public sealed record OrderHistoryItem(
        int Id, string? Source, DateTime OrderDate, DateTime? ReceiptDate, int OrderedItems, decimal TotalCost);
}

/// <summary>The batch list.</summary>
public sealed partial class BatchHistoryViewModel : ExpenseViewModelBase
{
    [ObservableProperty]
    private IReadOnlyList<Batch> _batches = [];

    public BatchHistoryViewModel(IDbContextFactory<TrackingStore> dbFactory) : base(dbFactory)
    {
        WeakReferenceMessenger.Default.Register<Messages.BatchesChanged>(this, (_, _) => InitializeBackground());
    }

    protected override async Task LoadAsync(CancellationToken ct)
    {
        Batches = await DataContext.Batches.AsNoTracking()
            .Include(b => b.Product)
            .OrderByDescending(b => b.BatchDate)
            .ToListAsync(ct);
    }
}

/// <summary>The output-record list.</summary>
public sealed partial class OutputHistoryViewModel : ExpenseViewModelBase
{
    [ObservableProperty]
    private IReadOnlyList<OutRecord> _records = [];

    public OutputHistoryViewModel(IDbContextFactory<TrackingStore> dbFactory) : base(dbFactory)
    {
        WeakReferenceMessenger.Default.Register<Messages.ExpensesChanged>(this, (_, _) => InitializeBackground());
    }

    protected override async Task LoadAsync(CancellationToken ct)
    {
        Records = await DataContext.OutRecords.AsNoTracking()
            .OrderByDescending(o => o.Date)
            .ToListAsync(ct);
    }
}

/// <summary>
/// Every movement of one inventory item, running to a balance. Ported as-is; the running total
/// stops at the first entry that could not be converted, as in the original.
/// </summary>
public sealed partial class InventoryItemHistoryViewModel : ExpenseViewModelBase
{
    /// <summary>Set before initialising.</summary>
    public int InventoryItemId { get; set; }

    [ObservableProperty]
    private string _inventoryItemName = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<InventoryItemHistoryEntry> _changeEntries = [];

    public InventoryItemHistoryViewModel(IDbContextFactory<TrackingStore> dbFactory) : base(dbFactory) { }

    protected override async Task LoadAsync(CancellationToken ct)
    {
        var item = await DataContext.InventoryItems.AsNoTracking()
            .SingleAsync(i => i.Id == InventoryItemId, ct);

        InventoryItemName = item.Name;

        var conversions = await DataContext.UnitConversions.AsNoTracking()
            .Where(c => c.UnitToId == item.UnitOfMeasureId)
            .ToListAsync(ct);

        var factors = conversions.ToDictionary(c => c.UnitFromId, c => c.Factor);

        decimal? Convert(decimal quantity, int unitId) =>
            unitId == item.UnitOfMeasureId
                ? quantity
                : factors.TryGetValue(unitId, out var factor) ? quantity * factor : null;

        var received = await DataContext.OrderItems.AsNoTracking()
            .Where(i => i.InventoryItemId == InventoryItemId && i.Order!.DateReceived != null)
            .Select(i => new { i.Quantity, Date = i.Order!.DateReceived!.Value, i.UnitOfMeasureId, UnitName = i.UnitOfMeasure!.Name })
            .ToListAsync(ct);

        var used = await DataContext.BatchItems.AsNoTracking()
            .Where(i => i.InventoryItemId == InventoryItemId)
            .Select(i => new { i.Quantity, Date = i.Batch!.BatchDate, i.UnitOfMeasureId, UnitName = i.UnitOfMeasure!.Name })
            .ToListAsync(ct);

        var lost = await DataContext.BatchLossItems.AsNoTracking()
            .Where(i => i.InventoryItemId == InventoryItemId)
            .Select(i => new { i.Quantity, Date = i.Batch!.BatchDate, i.UnitOfMeasureId, UnitName = i.UnitOfMeasure!.Name })
            .ToListAsync(ct);

        var output = await DataContext.OutRecordMaterials.AsNoTracking()
            .Where(i => i.InventoryItemId == InventoryItemId)
            .Select(i => new
            {
                i.Quantity,
                Date = i.OutRecord!.Date,
                UnitOfMeasureId = i.InventoryItem!.UnitOfMeasureId,
                UnitName = i.InventoryItem!.UnitOfMeasure!.Name,
            })
            .ToListAsync(ct);

        var entries = received
            .Select(r => new InventoryItemHistoryEntry(Convert(r.Quantity, r.UnitOfMeasureId), r.Date, r.UnitName))
            .Concat(used.Select(r => new InventoryItemHistoryEntry(Convert(-1M * r.Quantity, r.UnitOfMeasureId), r.Date, r.UnitName)))
            .Concat(lost.Select(r => new InventoryItemHistoryEntry(Convert(-1M * r.Quantity, r.UnitOfMeasureId), r.Date, r.UnitName)))
            .Concat(output.Select(r => new InventoryItemHistoryEntry(Convert(-1M * r.Quantity, r.UnitOfMeasureId), r.Date, r.UnitName)))
            .OrderBy(e => e.ChangeDate)
            .ToList();

        decimal runningTotal = 0M;
        foreach (var entry in entries)
        {
            if (entry.QuantityChange is not { } change) break;
            runningTotal += change;
            entry.QuantityToDate = runningTotal;
        }

        ChangeEntries = entries;
    }
}

/// <summary>One movement in an item's history.</summary>
public sealed partial class InventoryItemHistoryEntry : ObservableObject
{
    public decimal? QuantityChange { get; }

    public DateTime ChangeDate { get; }

    public string UnitOfMeasureName { get; }

    [ObservableProperty]
    private decimal? _quantityToDate;

    public InventoryItemHistoryEntry(decimal? quantityChange, DateTime changeDate, string unitOfMeasureName)
    {
        QuantityChange = quantityChange;
        ChangeDate = changeDate;
        UnitOfMeasureName = unitOfMeasureName;
    }
}
