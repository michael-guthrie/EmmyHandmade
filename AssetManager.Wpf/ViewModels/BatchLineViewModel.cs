using AssetManager.Core.Data.Entities;
using AssetManager.Core.Services;
using AssetManager.Infrastructure;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AssetManager.ViewModels;

/// <summary>
/// One line in the Used Items or Loss Items grid. Replaces the generic
/// <c>BatchViewModelBase.BatchItemWrapper&lt;T&gt;</c>.
///
/// <para>The generic parameter is gone because the two entity types share
/// <see cref="IBatchItem"/> and nothing here needed to know which was which — except when the
/// DataGrid creates a row from its blank line, which goes through the parameterless constructor
/// and is resolved by <see cref="Initialize"/>.</para>
/// </summary>
public sealed partial class BatchLineViewModel : ObservableObject
{
    private IBatchItem? _item;
    private LookupCache? _lookups;

    /// <summary>Stable identity used to match this line to its costing result.</summary>
    public int Key { get; private set; }

    public IBatchItem Item =>
        _item ?? throw new InvalidOperationException("The line has not been initialised yet.");

    /// <summary>Raised when something that affects costing changes.</summary>
    public event EventHandler? LineChanged;

    /// <summary>Used by the DataGrid's new-row placeholder.</summary>
    public BatchLineViewModel() { }

    public BatchLineViewModel(int key, IBatchItem item, LookupCache lookups)
    {
        Key = key;
        _item = item;
        _lookups = lookups;
    }

    /// <summary>Completes a line the grid constructed, creating the entity of the right type.</summary>
    internal void Initialize(int key, LookupCache lookups, bool isLoss)
    {
        Key = key;
        _lookups ??= lookups;
        _item ??= isLoss ? new BatchLossItem() : new BatchItem();

        // The grid's blank row defaults to ounces by weight until an item is chosen.
        if (_item.UnitOfMeasureId == 0) _item.UnitOfMeasureId = 102;
    }

    public int InventoryItemId
    {
        get => _item?.InventoryItemId ?? 0;
        set
        {
            if (_item is null || _item.InventoryItemId == value) return;
            _item.InventoryItemId = value;
            OnPropertyChanged();

            // Picking an item switches the line to that item's own tracking unit.
            var defaultUnit = _lookups?.DefaultUnitIdForItem(value);
            if (defaultUnit is { } unitId) UnitOfMeasureId = unitId;

            OnPropertyChanged(nameof(DefaultUnitName));
            LineChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public decimal Quantity
    {
        get => _item?.Quantity ?? 0M;
        set
        {
            if (_item is null || _item.Quantity == value) return;
            _item.Quantity = value;
            OnPropertyChanged();
            LineChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public int UnitOfMeasureId
    {
        get => _item?.UnitOfMeasureId ?? 0;
        set
        {
            if (_item is null || _item.UnitOfMeasureId == value) return;
            _item.UnitOfMeasureId = value;
            OnPropertyChanged();
            LineChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>The item's own tracking unit, shown greyed beside the chosen unit.</summary>
    public string DefaultUnitName => _lookups?.ItemUnitName(InventoryItemId) ?? "N/A";

    [ObservableProperty]
    private decimal? _percent;

    public BatchLineCost? Cost { get; private set; }

    /// <summary>What the grid's Cost column shows.</summary>
    public string? CostDisplay => Cost?.Display;

    /// <summary>The purchase lots this line was costed against, for the cost-detail popup.</summary>
    public IReadOnlyList<CostHistoryEntry> CostHistory => Cost?.History ?? [];

    public bool HasCostHistory => CostHistory.Count > 0;

    internal void ApplyCost(BatchLineCost? cost)
    {
        Cost = cost;
        OnPropertyChanged(nameof(Cost));
        OnPropertyChanged(nameof(CostDisplay));
        OnPropertyChanged(nameof(CostHistory));
        OnPropertyChanged(nameof(HasCostHistory));
    }
}
