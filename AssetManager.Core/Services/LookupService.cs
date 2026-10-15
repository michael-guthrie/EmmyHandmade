using AssetManager.Core.Data;
using Microsoft.EntityFrameworkCore;

namespace AssetManager.Core.Services;

/// <summary>
/// Replaces the WPF <c>ItemLookup</c>, <c>ProductLookup</c> and <c>UnitLookup</c> XAML resources,
/// which were process-wide caches refreshed by MvvmLight messenger broadcasts.
///
/// Here the lookups are loaded per page and refreshed by the page re-fetching after a save, which
/// is simpler and cannot go stale across users.
/// </summary>
public sealed class LookupService
{
    private readonly IDbContextFactory<TrackingStore> _dbFactory;

    public LookupService(IDbContextFactory<TrackingStore> dbFactory) => _dbFactory = dbFactory;

    public async Task<IReadOnlyList<ItemLookup>> GetItemsAsync(CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await db.InventoryItems.AsNoTracking()
            .OrderBy(i => i.Name)
            .Select(i => new ItemLookup(i.Id, i.Name, i.UnitOfMeasureId, i.UnitOfMeasure!.Name))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ProductLookup>> GetProductsAsync(CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await db.BatchProducts.AsNoTracking()
            .OrderBy(p => p.Name)
            .Select(p => new ProductLookup(p.Id, p.Name))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<UnitLookup>> GetUnitsAsync(CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await db.UnitsOfMeasure.AsNoTracking()
            .OrderBy(u => u.Name)
            .Select(u => new UnitLookup(u.Id, u.Name, u.UnitType))
            .ToListAsync(ct);
    }

    /// <summary>Loads all three lookups together, for pages that need every one of them.</summary>
    public async Task<LookupSet> GetAllAsync(CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var items = await db.InventoryItems.AsNoTracking()
            .OrderBy(i => i.Name)
            .Select(i => new ItemLookup(i.Id, i.Name, i.UnitOfMeasureId, i.UnitOfMeasure!.Name))
            .ToListAsync(ct);

        var products = await db.BatchProducts.AsNoTracking()
            .OrderBy(p => p.Name)
            .Select(p => new ProductLookup(p.Id, p.Name))
            .ToListAsync(ct);

        var units = await db.UnitsOfMeasure.AsNoTracking()
            .OrderBy(u => u.Name)
            .Select(u => new UnitLookup(u.Id, u.Name, u.UnitType))
            .ToListAsync(ct);

        return new LookupSet(items, products, units);
    }
}

public record ItemLookup(int Id, string Name, int UnitOfMeasureId, string UnitName);

public record ProductLookup(int Id, string Name);

public record UnitLookup(int Id, string Name, int UnitType);

public record LookupSet(
    IReadOnlyList<ItemLookup> Items,
    IReadOnlyList<ProductLookup> Products,
    IReadOnlyList<UnitLookup> Units)
{
    private Dictionary<int, ItemLookup>? _itemsById;
    private Dictionary<int, string>? _unitNamesById;

    public ItemLookup? FindItem(int id) =>
        (_itemsById ??= Items.ToDictionary(i => i.Id)).GetValueOrDefault(id);

    /// <summary>The tracking unit of an item, for the read-only "Default" column in the WPF grids.</summary>
    public string ItemUnitName(int itemId) => FindItem(itemId)?.UnitName ?? "N/A";

    public string UnitName(int unitId) =>
        (_unitNamesById ??= Units.ToDictionary(u => u.Id, u => u.Name)).GetValueOrDefault(unitId, "N/A");

    public string ProductName(int productId) =>
        Products.FirstOrDefault(p => p.Id == productId)?.Name ?? "N/A";
}
