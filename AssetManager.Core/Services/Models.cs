using AssetManager.Core.Data.Entities;

namespace AssetManager.Core.Services;

/// <summary>Stock level and value of one raw material at a point in time.</summary>
/// <param name="Item">The item measured.</param>
/// <param name="IsComputable">
/// False when some transaction used a unit with no conversion route to the item's tracking unit.
/// Quantity and Value are meaningless in that case -- the WPF grids showed "N/A".
/// </param>
/// <param name="Quantity">On-hand quantity in the item's tracking unit. May be negative if usage outran recorded purchases.</param>
/// <param name="Value">Value of the on-hand quantity, costed against the most recent purchases.</param>
public record InventoryTotal(InventoryItem Item, bool IsComputable, decimal Quantity, decimal Value)
{
    public decimal? UnitValue => Quantity == 0M ? null : Value / Quantity;
}

/// <summary>Produced, sold and remaining position for one finished product.</summary>
public record ProductTotal(
    BatchProduct Product,
    decimal TotalProduced,
    decimal TotalOut,
    decimal RemainingValue)
{
    public decimal Remaining => TotalProduced - TotalOut;

    public decimal? UnitValue => Remaining == 0M ? null : RemainingValue / Remaining;
}

/// <summary>
/// One purchase lot that a consumption was costed against, and how much of it was drawn down.
/// Mirrors the WPF <c>HistoryItemViewModel</c>; this is what the cost-history popup displayed.
/// </summary>
public record CostHistoryEntry(
    OrderItem OrderItem,
    decimal AdjustedQuantity,
    decimal AdjustedUnitPrice)
{
    public decimal ExtendedCost => AdjustedQuantity * AdjustedUnitPrice;
}

/// <summary>Outcome of costing one batch line against the purchase history.</summary>
public enum CostStatus
{
    /// <summary>Fully costed against purchase lots.</summary>
    Costed,

    /// <summary>Quantity was zero, so the cost is zero.</summary>
    Zero,

    /// <summary>No inventory item selected yet.</summary>
    NoItemSelected,

    /// <summary>No conversion route between the line's unit and the item's tracking unit.</summary>
    ConversionError,

    /// <summary>Purchases on or before the batch date did not cover the quantity used.</summary>
    NotFound,

    /// <summary>No batch date set yet, so there is nothing to cost against.</summary>
    NoBatchDate,
}

/// <summary>Cost of one batch line, plus the lots it was drawn from.</summary>
public record BatchLineCost(
    CostStatus Status,
    decimal? Cost,
    IReadOnlyList<CostHistoryEntry> History)
{
    public static BatchLineCost None(CostStatus status) => new(status, null, Array.Empty<CostHistoryEntry>());

    public static BatchLineCost ZeroCost() => new(CostStatus.Zero, 0M, Array.Empty<CostHistoryEntry>());

    /// <summary>What the WPF grid put in its cost column. Null renders as a blank cell.</summary>
    public string? Display => Status switch
    {
        CostStatus.Costed => Cost.GetValueOrDefault().ToString("N2"),
        CostStatus.Zero => "0.00",
        CostStatus.NoItemSelected => "Select an item",
        CostStatus.ConversionError => "Conversion Error",
        CostStatus.NoBatchDate => null,
        _ => "Not found",
    };
}

/// <summary>Rolled-up cost of a whole batch.</summary>
/// <param name="IsComplete">False if any line could not be costed; the WPF UI appended "(incomplete)".</param>
public record BatchCostSummary(
    decimal UsageCost,
    bool UsageComplete,
    bool HasUsageItems,
    decimal LossCost,
    bool LossComplete,
    bool HasLossItems,
    decimal? UnitsProduced)
{
    public bool IsComplete => (!HasUsageItems || UsageComplete) && (!HasLossItems || LossComplete);

    public decimal? TotalCost =>
        (!HasUsageItems && !HasLossItems) ? null : UsageCost + LossCost;

    public decimal? PerUnitCost
    {
        get
        {
            var total = TotalCost;
            var units = UnitsProduced.GetValueOrDefault();
            return (total is null || units == 0M) ? null : total.Value / units;
        }
    }

    private string Suffix => IsComplete ? string.Empty : " (incomplete)";

    public string? UsageCostDisplay => HasUsageItems ? UsageCost.ToString("N2") + (UsageComplete ? "" : " (incomplete)") : null;
    public string? LossCostDisplay => HasLossItems ? LossCost.ToString("N2") + (LossComplete ? "" : " (incomplete)") : null;
    public string? TotalCostDisplay => TotalCost is null ? null : TotalCost.Value.ToString("N2") + Suffix;
    public string? PerUnitCostDisplay => PerUnitCost is null ? null : PerUnitCost.Value.ToString("0.00##") + Suffix;
}

/// <summary>Expense and sales totals for one calendar quarter.</summary>
public record QuarterSummary(int Year, int Quarter, decimal ExpenseTotal, decimal SalesTotal)
{
    public decimal Net => SalesTotal - ExpenseTotal;

    public DateTime Start => new(Year, (Quarter * 3) - 2, 1);

    public DateTime End => new DateTime(Year, Quarter * 3, 1).AddMonths(1).AddMilliseconds(-1.0);
}
