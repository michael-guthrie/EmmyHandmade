using AssetManager.Core.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AssetManager.Core.Data;

/// <summary>
/// Reference data carried over from the EF6 <c>Migrations/Configuration.Seed</c> method.
///
/// <para>This runs at startup rather than through EF Core's <c>HasData</c>, deliberately. HasData
/// bakes the rows into the model, so migrations try to insert them — which breaks against a
/// database that already holds this data (the existing SQL Server one), and makes the two apps
/// fight over the same rows. Seeding at runtime instead mirrors what EF6 actually did: its
/// <c>Seed</c> method ran on migrate and used <c>AddOrUpdate</c>, which was idempotent.</para>
///
/// <para>Every step here is idempotent and additive. Nothing is updated or deleted, so a unit or
/// item the user has since renamed or re-costed is left exactly as they left it.</para>
/// </summary>
public static class SeedData
{
    /// <summary>
    /// Inserts any missing reference data. Safe to call on every startup, and safe to call
    /// against a database already populated by the old EF6 app.
    /// </summary>
    /// <returns>The number of rows inserted.</returns>
    public static async Task<int> EnsureSeededAsync(TrackingStore db, CancellationToken ct = default)
    {
        var inserted = 0;
        inserted += await SeedUnitsAsync(db, ct);
        inserted += await SeedConversionsAsync(db, ct);
        inserted += await SeedInventoryItemsAsync(db, ct);
        return inserted;
    }

    private static async Task<int> SeedUnitsAsync(TrackingStore db, CancellationToken ct)
    {
        var existingIds = await db.UnitsOfMeasure.AsNoTracking().Select(u => u.Id).ToListAsync(ct);
        var existing = existingIds.ToHashSet();

        var missing = Units.Where(u => !existing.Contains(u.Id)).ToList();
        if (missing.Count == 0) return 0;

        db.UnitsOfMeasure.AddRange(missing);
        await db.SaveChangesAsync(ct);
        return missing.Count;
    }

    private static async Task<int> SeedConversionsAsync(TrackingStore db, CancellationToken ct)
    {
        var existingKeys = await db.UnitConversions.AsNoTracking()
            .Select(c => new { c.UnitFromId, c.UnitToId })
            .ToListAsync(ct);
        var existing = existingKeys.Select(k => (k.UnitFromId, k.UnitToId)).ToHashSet();

        // A conversion can only be inserted if both of its units exist.
        var unitIds = (await db.UnitsOfMeasure.AsNoTracking().Select(u => u.Id).ToListAsync(ct)).ToHashSet();

        var missing = Conversions
            .Where(c => !existing.Contains((c.UnitFromId, c.UnitToId)))
            .Where(c => unitIds.Contains(c.UnitFromId) && unitIds.Contains(c.UnitToId))
            .ToList();

        if (missing.Count == 0) return 0;

        db.UnitConversions.AddRange(missing);
        await db.SaveChangesAsync(ct);
        return missing.Count;
    }

    private static async Task<int> SeedInventoryItemsAsync(TrackingStore db, CancellationToken ct)
    {
        // Matched on name, exactly as the EF6 seed did (AddOrUpdate(i => i.Name, ...)). Ids are
        // left to the database, so this never collides with rows already present.
        var existingNames = await db.InventoryItems.AsNoTracking().Select(i => i.Name).ToListAsync(ct);
        var existing = existingNames.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var unitIds = (await db.UnitsOfMeasure.AsNoTracking().Select(u => u.Id).ToListAsync(ct)).ToHashSet();

        var missing = InventoryItems()
            .Where(i => !existing.Contains(i.Name))
            .Where(i => unitIds.Contains(i.UnitOfMeasureId)
                        && (i.AlternateUnitOfMeasureId is null || unitIds.Contains(i.AlternateUnitOfMeasureId.Value)))
            .ToList();

        if (missing.Count == 0) return 0;

        db.InventoryItems.AddRange(missing);
        await db.SaveChangesAsync(ct);
        return missing.Count;
    }

    private static readonly UnitOfMeasure[] Units =
    [
        new() { Id = 101, Name = "fl oz", UnitType = 1 },
        new() { Id = 102, Name = "oz", UnitType = 2 },
        new() { Id = 103, Name = "gal", UnitType = 1 },
        new() { Id = 104, Name = "lb", UnitType = 2 },
        new() { Id = 105, Name = "ea", UnitType = 0 },
        new() { Id = 106, Name = "yd", UnitType = 3 },
        new() { Id = 107, Name = "ft", UnitType = 3 },
        new() { Id = 108, Name = "m", UnitType = 3 },
        new() { Id = 109, Name = "in", UnitType = 3 },
        new() { Id = 110, Name = "cm", UnitType = 3 },
        new() { Id = 111, Name = "L", UnitType = 1 },
        new() { Id = 112, Name = "mL", UnitType = 1 },
        new() { Id = 113, Name = "pack", UnitType = 1 },
    ];

    private static readonly UnitConversion[] Conversions =
    [
        new() { UnitFromId = 103, UnitToId = 101, Factor = 128M },
        new() { UnitFromId = 111, UnitToId = 101, Factor = 33.81402270M },
        new() { UnitFromId = 112, UnitToId = 101, Factor = 0.03814023M },
        new() { UnitFromId = 104, UnitToId = 102, Factor = 16M },
        new() { UnitFromId = 101, UnitToId = 103, Factor = 0.00781250M },
        new() { UnitFromId = 111, UnitToId = 103, Factor = 0.26417205M },
        new() { UnitFromId = 112, UnitToId = 103, Factor = 0.00026417M },
        new() { UnitFromId = 102, UnitToId = 104, Factor = 0.06250000M },
        new() { UnitFromId = 107, UnitToId = 106, Factor = 0.33333333M },
        new() { UnitFromId = 108, UnitToId = 106, Factor = 1.09361330M },
        new() { UnitFromId = 109, UnitToId = 106, Factor = 0.02777778M },
        new() { UnitFromId = 110, UnitToId = 106, Factor = 0.01093613M },
        new() { UnitFromId = 106, UnitToId = 107, Factor = 3.00000000M },
        new() { UnitFromId = 108, UnitToId = 107, Factor = 3.28083990M },
        new() { UnitFromId = 110, UnitToId = 107, Factor = 0.03280840M },
        new() { UnitFromId = 106, UnitToId = 108, Factor = 0.91440000M },
        new() { UnitFromId = 107, UnitToId = 108, Factor = 0.30480000M },
        new() { UnitFromId = 109, UnitToId = 108, Factor = 0.02540000M },
        new() { UnitFromId = 110, UnitToId = 108, Factor = 0.01000000M },
        new() { UnitFromId = 106, UnitToId = 109, Factor = 36.00000000M },
        new() { UnitFromId = 108, UnitToId = 109, Factor = 39.37007870M },
        new() { UnitFromId = 110, UnitToId = 109, Factor = 0.39370079M },
        new() { UnitFromId = 106, UnitToId = 110, Factor = 91.44000000M },
        new() { UnitFromId = 107, UnitToId = 110, Factor = 30.48000000M },
        new() { UnitFromId = 108, UnitToId = 110, Factor = 100.00000000M },
        new() { UnitFromId = 109, UnitToId = 110, Factor = 2.54000000M },
        new() { UnitFromId = 101, UnitToId = 111, Factor = 0.02957353M },
        new() { UnitFromId = 103, UnitToId = 111, Factor = 3.78541178M },
        new() { UnitFromId = 101, UnitToId = 112, Factor = 29.57352960M },
        new() { UnitFromId = 103, UnitToId = 112, Factor = 3785.41178000M },
        new() { UnitFromId = 111, UnitToId = 112, Factor = 1000.00000000M },
    ];

    private static IEnumerable<InventoryItem> InventoryItems() =>
    [
        new() { Name = "Lye", UnitOfMeasureId = 102 },
        new() { Name = "Coconut Oil", UnitOfMeasureId = 102, AlternateUnitOfMeasureId = 103, AlternateUnitOfMeasureFactor = 123.1277420000M, IsPercentBatchOil = true },
        new() { Name = "Olive Oil", UnitOfMeasureId = 102, IsPercentBatchOil = true },
        new() { Name = "Shea Butter", UnitOfMeasureId = 102, IsPercentBatchOil = true },
        new() { Name = "Palm Oil", UnitOfMeasureId = 102, IsPercentBatchOil = true },
        new() { Name = "Canola Oil", UnitOfMeasureId = 102, IsPercentBatchOil = true },
        new() { Name = "Castor Oil", UnitOfMeasureId = 102, IsPercentBatchOil = true },
        new() { Name = "Sweet Almond Oil", UnitOfMeasureId = 102 },
        new() { Name = "Avacado Oil", UnitOfMeasureId = 102 },
        new() { Name = "Cocoa Butter", UnitOfMeasureId = 102 },
        new() { Name = "Gift Box 4x4x4", UnitOfMeasureId = 105 },
        new() { Name = "Gift Box 6x6x6", UnitOfMeasureId = 105 },
        new() { Name = "Distilled Water", UnitOfMeasureId = 102, AlternateUnitOfMeasureId = 103, AlternateUnitOfMeasureFactor = 133.980134M },
        new() { Name = "Epsom Salt", UnitOfMeasureId = 102 },
        new() { Name = "Dead Sea Salt", UnitOfMeasureId = 102 },
        new() { Name = "Pink Himalaya Salt", UnitOfMeasureId = 102 },
        new() { Name = "Sea Salt", UnitOfMeasureId = 102 },
        new() { Name = "Lavender Buds", UnitOfMeasureId = 102 },
        new() { Name = "Dried Calendula Flower", UnitOfMeasureId = 102 },
        new() { Name = "Dried Rose Petals", UnitOfMeasureId = 102 },
        new() { Name = "Beeswax", UnitOfMeasureId = 102 },
        new() { Name = "Lip Balm Tube", UnitOfMeasureId = 105 },
        new() { Name = "Dropper", UnitOfMeasureId = 105 },
        new() { Name = "Orange Essential Oil", UnitOfMeasureId = 102 },
        new() { Name = "Lavender Essential Oil", UnitOfMeasureId = 102 },
    ];
}
