using AssetManager.Core.Data;
using AssetManager.Core.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AssetManager.Core.Services;

/// <summary>
/// Port of the costing logic in the WPF <c>BatchViewModelBase</c>: what a batch line costs, what
/// share of the batch each ingredient is, and the rolled-up batch totals.
///
/// Costing a line walks the purchase lots for that material oldest-first. Lots are first absorbed
/// by everything consumed before this batch; once the running availability turns positive, the
/// remaining lots pay for this line at their own unit price. That makes a batch cost what the
/// material actually cost when it was bought, not an average.
/// </summary>
public sealed class BatchCostingService
{
    private readonly IDbContextFactory<TrackingStore> _dbFactory;

    public BatchCostingService(IDbContextFactory<TrackingStore> dbFactory) => _dbFactory = dbFactory;

    /// <summary>Costs one batch line as of <paramref name="batchDate"/>.</summary>
    public async Task<BatchLineCost> CalculateLineCostAsync(
        int inventoryItemId,
        decimal quantity,
        int unitOfMeasureId,
        DateTime? batchDate,
        CancellationToken ct = default)
    {
        if (batchDate is null) return BatchLineCost.None(CostStatus.NoBatchDate);
        if (quantity == 0M) return BatchLineCost.ZeroCost();

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var converter = await UnitConversionService.CreateAsync(db, ct);

        var item = await db.InventoryItems.AsNoTracking()
            .SingleOrDefaultAsync(i => i.Id == inventoryItemId, ct);
        if (item is null) return BatchLineCost.None(CostStatus.NoItemSelected);

        var context = await LoadContextAsync(db, item, batchDate.Value, ct);

        return CalculateLineCost(item, quantity, unitOfMeasureId, converter, context);
    }

    /// <summary>
    /// Costs several batch lines against one snapshot of the data. Use this when costing a whole
    /// batch: the WPF app re-queried the entire purchase history once per line.
    /// </summary>
    public async Task<IReadOnlyDictionary<int, BatchLineCost>> CalculateLineCostsAsync(
        IReadOnlyList<BatchLineInput> lines,
        DateTime? batchDate,
        CancellationToken ct = default)
    {
        var results = new Dictionary<int, BatchLineCost>();
        if (lines.Count == 0) return results;

        if (batchDate is null)
        {
            foreach (var line in lines) results[line.Key] = BatchLineCost.None(CostStatus.NoBatchDate);
            return results;
        }

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var converter = await UnitConversionService.CreateAsync(db, ct);

        var itemIds = lines.Select(l => l.InventoryItemId).Distinct().ToList();
        var items = await db.InventoryItems.AsNoTracking()
            .Where(i => itemIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, ct);

        var contexts = new Dictionary<int, ItemCostContext>();

        foreach (var line in lines)
        {
            if (line.Quantity == 0M) { results[line.Key] = BatchLineCost.ZeroCost(); continue; }

            if (!items.TryGetValue(line.InventoryItemId, out var item))
            {
                results[line.Key] = BatchLineCost.None(CostStatus.NoItemSelected);
                continue;
            }

            if (!contexts.TryGetValue(item.Id, out var context))
            {
                context = await LoadContextAsync(db, item, batchDate.Value, ct);
                contexts[item.Id] = context;
            }

            results[line.Key] = CalculateLineCost(item, line.Quantity, line.UnitOfMeasureId, converter, context);
        }

        return results;
    }

    private static async Task<ItemCostContext> LoadContextAsync(
        TrackingStore db, InventoryItem item, DateTime batchDate, CancellationToken ct)
    {
        // Prior consumption uses a strict "<" on batch date, matching the WPF original: batches on
        // the same day as the one being costed do not consume stock ahead of it.
        var priorUsage = await db.BatchItems.AsNoTracking()
            .Where(bi => bi.InventoryItemId == item.Id && bi.Batch!.BatchDate < batchDate)
            .Select(bi => new ConsumptionRow(bi.Quantity, bi.UnitOfMeasureId))
            .ToListAsync(ct);

        var priorLoss = await db.BatchLossItems.AsNoTracking()
            .Where(bli => bli.InventoryItemId == item.Id && bli.Batch!.BatchDate < batchDate)
            .Select(bli => new ConsumptionRow(bli.Quantity, bli.UnitOfMeasureId))
            .ToListAsync(ct);

        // The WPF original considered every received order here, not only those received on or
        // before the batch date. Preserved as-is.
        //
        // Id is the tiebreaker after date: several orders can share a received date, and without a
        // total order the lots would be drawn down in whatever sequence the database happened to
        // return, so the same batch could cost differently between runs.
        var orderItems = await db.OrderItems.AsNoTracking()
            .Include(oi => oi.Order)
            .Include(oi => oi.UnitOfMeasure)
            .Where(oi => oi.InventoryItemId == item.Id && oi.Order!.DateReceived != null)
            .OrderBy(oi => oi.Order!.DateReceived)
            .ThenBy(oi => oi.Id)
            .ToListAsync(ct);

        var lots = orderItems
            .Select(oi => new LotRow(
                oi.Id, oi.Quantity, oi.UnitPrice, oi.UnitOfMeasureId, oi.Order!.DateReceived))
            .ToList();

        return new ItemCostContext(
            priorUsage.Concat(priorLoss).ToList(),
            lots,
            orderItems.ToDictionary(oi => oi.Id));
    }

    private static BatchLineCost CalculateLineCost(
        InventoryItem item,
        decimal quantity,
        int unitOfMeasureId,
        UnitConversionService converter,
        ItemCostContext context)
    {
        // Quantity being costed, expressed in the item's tracking unit.
        if (!converter.TryConvertQuantity(item, unitOfMeasureId, quantity, out var qtyForValue))
        {
            return BatchLineCost.None(CostStatus.ConversionError);
        }

        decimal usedItemQty = 0M;
        foreach (var row in context.PriorConsumption)
        {
            if (!converter.TryConvertQuantity(item, row.UnitOfMeasureId, row.Quantity, out var adjusted))
            {
                return BatchLineCost.None(CostStatus.ConversionError);
            }
            usedItemQty += adjusted;
        }

        decimal qtyAvailable = usedItemQty * -1M;
        decimal usedValue = 0M;
        var history = new List<CostHistoryEntry>();

        foreach (var lot in context.Lots)
        {
            if (!converter.TryGetFactor(item, lot.UnitOfMeasureId, out var factor) || factor == 0M)
            {
                return BatchLineCost.None(CostStatus.ConversionError);
            }

            var adjustedQuantity = lot.Quantity * factor;
            var adjustedUnitPrice = lot.UnitPrice / factor;

            qtyAvailable += adjustedQuantity;
            if (qtyAvailable > 0M)
            {
                var adjustedUsedQty = Math.Min(Math.Min(qtyAvailable, adjustedQuantity), qtyForValue);
                usedValue += adjustedUsedQty * adjustedUnitPrice;

                if (context.OrderItems.TryGetValue(lot.OrderItemId, out var orderItem))
                {
                    history.Add(new CostHistoryEntry(orderItem, adjustedUsedQty, adjustedUnitPrice));
                }

                // NOTE: the WPF original decrements by the whole lot quantity here, not by the
                // amount actually drawn (adjustedUsedQty). Where a lot is only partly available
                // because earlier batches already consumed it, the remaining need is written down
                // faster than value is credited, so the line can finish costed below its true
                // cost. Preserved deliberately so figures match the desktop app; see README-PORT.md.
                qtyForValue -= adjustedQuantity;
            }

            if (qtyForValue <= 0M)
            {
                return new BatchLineCost(CostStatus.Costed, usedValue, history);
            }
        }

        return BatchLineCost.None(CostStatus.NotFound);
    }

    /// <summary>
    /// Each used line's share of the batch, as a percentage.
    ///
    /// If any ingredient is flagged <see cref="InventoryItem.IsPercentBatchOil"/>, percentages are
    /// taken against total oil weight (the soapmaking convention, where additives are expressed as
    /// a percentage of oils and can exceed 100% in total). Otherwise they are a share of the whole
    /// batch. Returns nulls if the lines do not all share one unit, since the sum would be meaningless.
    /// </summary>
    public async Task<IReadOnlyDictionary<int, decimal?>> CalculateUsedItemPercentagesAsync(
        IReadOnlyList<BatchLineInput> usedLines,
        CancellationToken ct = default)
    {
        var result = usedLines.ToDictionary(l => l.Key, _ => (decimal?)null);
        if (usedLines.Count == 0) return result;

        if (usedLines.Select(l => l.UnitOfMeasureId).Distinct().Count() != 1) return result;

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var itemIds = usedLines.Select(l => l.InventoryItemId).Distinct().ToList();
        var oilIds = await db.InventoryItems.AsNoTracking()
            .Where(i => i.IsPercentBatchOil && itemIds.Contains(i.Id))
            .Select(i => i.Id)
            .ToListAsync(ct);

        var oilSet = oilIds.ToHashSet();

        decimal basis = usedLines.Any(l => oilSet.Contains(l.InventoryItemId))
            ? usedLines.Where(l => oilSet.Contains(l.InventoryItemId)).Sum(l => l.Quantity) / 100M
            : usedLines.Sum(l => l.Quantity) / 100M;

        if (basis <= 0M) return result;

        foreach (var line in usedLines)
        {
            result[line.Key] = line.Quantity / basis;
        }

        return result;
    }

    /// <summary>
    /// Rolls line costs up into the batch totals the WPF screen showed: usage, loss, total and
    /// per-unit, each marked incomplete if any line failed to cost.
    /// </summary>
    public static BatchCostSummary Summarise(
        IReadOnlyCollection<BatchLineCost> usedCosts,
        IReadOnlyCollection<BatchLineCost> lossCosts,
        decimal? unitsProduced)
    {
        return new BatchCostSummary(
            UsageCost: usedCosts.Sum(c => c.Cost.GetValueOrDefault()),
            UsageComplete: usedCosts.All(c => c.Cost.HasValue),
            HasUsageItems: usedCosts.Count > 0,
            LossCost: lossCosts.Sum(c => c.Cost.GetValueOrDefault()),
            LossComplete: lossCosts.All(c => c.Cost.HasValue),
            HasLossItems: lossCosts.Count > 0,
            UnitsProduced: unitsProduced);
    }

    private readonly record struct ConsumptionRow(decimal Quantity, int UnitOfMeasureId);

    private readonly record struct LotRow(
        int OrderItemId, decimal Quantity, decimal UnitPrice, int UnitOfMeasureId, DateTime? DateReceived);

    private sealed record ItemCostContext(
        List<ConsumptionRow> PriorConsumption,
        List<LotRow> Lots,
        Dictionary<int, OrderItem> OrderItems);
}

/// <summary>One batch line to be costed. <paramref name="Key"/> ties the result back to your row.</summary>
public readonly record struct BatchLineInput(int Key, int InventoryItemId, decimal Quantity, int UnitOfMeasureId);
