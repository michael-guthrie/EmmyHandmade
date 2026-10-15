using AssetManager.Core.Data;
using AssetManager.Core.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AssetManager.Core.Services;

/// <summary>
/// Port of the WPF <c>InventoryCalculationHelper</c>.
///
/// Stock on hand is purchases received minus everything consumed: batch usage, batch losses, and
/// direct material output. Value is assigned by walking received orders oldest-first and crediting
/// value only once the running quantity turns positive, which values what remains against the most
/// recent purchases. Both behaviours are preserved exactly; only the querying is restructured so
/// each total is a handful of queries rather than one per transaction row.
/// </summary>
public sealed class InventoryCalculationService
{
    private readonly IDbContextFactory<TrackingStore> _dbFactory;

    public InventoryCalculationService(IDbContextFactory<TrackingStore> dbFactory) => _dbFactory = dbFactory;

    public Task<InventoryTotal> GetTotalCurrentAsync(int inventoryItemId, CancellationToken ct = default) =>
        GetTotalAtDateAsync(inventoryItemId, DateTime.Now, ct);

    public async Task<InventoryTotal> GetTotalAtDateAsync(
        int inventoryItemId, DateTime targetDate, CancellationToken ct = default)
    {
        var totals = await GetTotalsAtDateAsync(new[] { inventoryItemId }, targetDate, ct);
        return totals.Single();
    }

    /// <summary>Totals for every inventory item as of now.</summary>
    public Task<IReadOnlyList<InventoryTotal>> GetAllTotalsCurrentAsync(CancellationToken ct = default) =>
        GetTotalsAtDateAsync(null, DateTime.Now, ct);

    /// <summary>Totals for every inventory item as of a chosen date.</summary>
    public Task<IReadOnlyList<InventoryTotal>> GetAllTotalsAtDateAsync(
        DateTime targetDate, CancellationToken ct = default) =>
        GetTotalsAtDateAsync(null, targetDate, ct);

    /// <summary>
    /// Totals for the given items (or all items when <paramref name="inventoryItemIds"/> is null)
    /// as of <paramref name="targetDate"/>.
    /// </summary>
    public async Task<IReadOnlyList<InventoryTotal>> GetTotalsAtDateAsync(
        IReadOnlyCollection<int>? inventoryItemIds,
        DateTime targetDate,
        CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var converter = await UnitConversionService.CreateAsync(db, ct);

        var itemsQuery = db.InventoryItems.AsNoTracking();
        if (inventoryItemIds is not null)
        {
            itemsQuery = itemsQuery.Where(i => inventoryItemIds.Contains(i.Id));
        }

        var items = await itemsQuery.OrderBy(i => i.Name).ToListAsync(ct);
        var ids = items.Select(i => i.Id).ToHashSet();

        // Consumption: batch usage, batch losses, direct material output.
        var batchUsage = await db.BatchItems.AsNoTracking()
            .Where(bi => ids.Contains(bi.InventoryItemId) && bi.Batch!.BatchDate <= targetDate)
            .Select(bi => new ConsumptionRow(bi.InventoryItemId, bi.Quantity, bi.UnitOfMeasureId))
            .ToListAsync(ct);

        var batchLoss = await db.BatchLossItems.AsNoTracking()
            .Where(bli => ids.Contains(bli.InventoryItemId) && bli.Batch!.BatchDate <= targetDate)
            .Select(bli => new ConsumptionRow(bli.InventoryItemId, bli.Quantity, bli.UnitOfMeasureId))
            .ToListAsync(ct);

        // Direct output always uses the item's own tracking unit, so no conversion is applied.
        var directOut = await db.OutRecordMaterials.AsNoTracking()
            .Where(m => ids.Contains(m.InventoryItemId) && m.OutRecord!.Date <= targetDate)
            .GroupBy(m => m.InventoryItemId)
            .Select(g => new { InventoryItemId = g.Key, Quantity = g.Sum(m => m.Quantity) })
            .ToDictionaryAsync(x => x.InventoryItemId, x => x.Quantity, ct);

        // Receipts, oldest first. The EF6 version filtered on DateReceived <= targetDate, which
        // also excludes orders that have not been received at all (null date).
        // Id is the tiebreaker after date: several orders can share a received date, and valuation
        // walks lots in sequence, so without a total order the same data could value differently
        // between runs.
        var receipts = await db.OrderItems.AsNoTracking()
            .Where(oi => ids.Contains(oi.InventoryItemId) && oi.Order!.DateReceived <= targetDate)
            .OrderBy(oi => oi.Order!.DateReceived)
            .ThenBy(oi => oi.Id)
            .Select(oi => new ReceiptRow(
                oi.Id, oi.InventoryItemId, oi.Quantity, oi.UnitPrice, oi.UnitOfMeasureId, oi.Order!.DateReceived))
            .ToListAsync(ct);

        var usageByItem = batchUsage.Concat(batchLoss)
            .GroupBy(r => r.InventoryItemId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var receiptsByItem = receipts
            .GroupBy(r => r.InventoryItemId)
            .ToDictionary(g => g.Key, g => g.OrderBy(r => r.DateReceived).ThenBy(r => r.OrderItemId).ToList());

        var results = new List<InventoryTotal>(items.Count);

        foreach (var item in items)
        {
            decimal usedItemQty = 0M;
            var computable = true;

            if (usageByItem.TryGetValue(item.Id, out var consumption))
            {
                foreach (var row in consumption)
                {
                    if (!converter.TryConvertQuantity(item, row.UnitOfMeasureId, row.Quantity, out var adjusted))
                    {
                        computable = false;
                        break;
                    }
                    usedItemQty += adjusted;
                }
            }

            if (!computable)
            {
                results.Add(new InventoryTotal(item, false, 0M, 0M));
                continue;
            }

            usedItemQty += directOut.GetValueOrDefault(item.Id);

            decimal quantity = -1M * usedItemQty;
            decimal value = 0M;

            if (receiptsByItem.TryGetValue(item.Id, out var lots))
            {
                foreach (var lot in lots)
                {
                    if (!converter.TryGetFactor(item, lot.UnitOfMeasureId, out var factor) || factor == 0M)
                    {
                        computable = false;
                        break;
                    }

                    var adjustedQuantity = lot.Quantity * factor;
                    var adjustedUnitPrice = lot.UnitPrice / factor;

                    quantity += adjustedQuantity;
                    if (quantity > 0M)
                    {
                        value += Math.Min(quantity, adjustedQuantity) * adjustedUnitPrice;
                    }
                }
            }

            results.Add(computable
                ? new InventoryTotal(item, true, quantity, value)
                : new InventoryTotal(item, false, 0M, 0M));
        }

        return results;
    }

    public Task<ProductTotal> GetProductTotalCurrentAsync(int batchProductId, CancellationToken ct = default) =>
        GetProductTotalAtDateAsync(batchProductId, DateTime.Now, ct);

    public async Task<ProductTotal> GetProductTotalAtDateAsync(
        int batchProductId, DateTime targetDate, CancellationToken ct = default)
    {
        var totals = await GetProductTotalsAtDateAsync(new[] { batchProductId }, targetDate, ct);
        return totals.Single();
    }

    public Task<IReadOnlyList<ProductTotal>> GetAllProductTotalsCurrentAsync(CancellationToken ct = default) =>
        GetProductTotalsAtDateAsync(null, DateTime.Now, ct);

    public Task<IReadOnlyList<ProductTotal>> GetAllProductTotalsAtDateAsync(
        DateTime targetDate, CancellationToken ct = default) =>
        GetProductTotalsAtDateAsync(null, targetDate, ct);

    /// <summary>
    /// Produced, out and remaining value per product. Value is credited batch by batch in date
    /// order at that batch's own per-unit cost, so what remains is valued at the cost of the most
    /// recent production.
    /// </summary>
    public async Task<IReadOnlyList<ProductTotal>> GetProductTotalsAtDateAsync(
        IReadOnlyCollection<int>? batchProductIds,
        DateTime targetDate,
        CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var productsQuery = db.BatchProducts.AsNoTracking();
        if (batchProductIds is not null)
        {
            productsQuery = productsQuery.Where(p => batchProductIds.Contains(p.Id));
        }

        var products = await productsQuery.OrderBy(p => p.Name).ToListAsync(ct);
        var ids = products.Select(p => p.Id).ToHashSet();

        var outByProduct = await db.OutRecordProducts.AsNoTracking()
            .Where(orp => ids.Contains(orp.BatchProductId) && orp.OutRecord!.Date <= targetDate)
            .GroupBy(orp => orp.BatchProductId)
            .Select(g => new { BatchProductId = g.Key, Quantity = g.Sum(x => x.Quantity) })
            .ToDictionaryAsync(x => x.BatchProductId, x => x.Quantity, ct);

        var batches = await db.Batches.AsNoTracking()
            .Where(b => ids.Contains(b.ProductId)
                        && b.BatchDate <= targetDate
                        && b.UnitsProduced != null)
            .OrderBy(b => b.BatchDate)
            .Select(b => new { b.ProductId, b.UnitsProduced, b.Cost })
            .ToListAsync(ct);

        var batchesByProduct = batches
            .GroupBy(b => b.ProductId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var results = new List<ProductTotal>(products.Count);

        foreach (var product in products)
        {
            var totalOut = outByProduct.GetValueOrDefault(product.Id);
            decimal totalProduced = 0M;
            decimal remainingValue = 0M;

            if (batchesByProduct.TryGetValue(product.Id, out var productBatches))
            {
                foreach (var b in productBatches)
                {
                    var unitsProduced = b.UnitsProduced ?? 0M;
                    var cost = b.Cost ?? 0M;
                    totalProduced += unitsProduced;

                    if (unitsProduced > 0M)
                    {
                        remainingValue +=
                            Math.Max(0M, Math.Min(totalProduced - totalOut, unitsProduced))
                            * (cost / unitsProduced);
                    }
                }
            }

            results.Add(new ProductTotal(product, totalProduced, totalOut, remainingValue));
        }

        return results;
    }

    private readonly record struct ConsumptionRow(int InventoryItemId, decimal Quantity, int UnitOfMeasureId);

    private readonly record struct ReceiptRow(
        int OrderItemId, int InventoryItemId, decimal Quantity, decimal UnitPrice, int UnitOfMeasureId, DateTime? DateReceived);
}
