using AssetManager.Core.Data;
using AssetManager.Core.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AssetManager.Core.Services;

/// <summary>
/// Resolves "how many of an item's tracking units is one of some other unit?".
///
/// The WPF app repeated this same cascade inline in six places (InventoryCalculationHelper twice,
/// BatchViewModelBase four times). It is one method here. The resolution order is unchanged:
///
///   1. Same unit as the item tracks in -> factor 1.
///   2. A direct UnitConversion row from the source unit to the item's tracking unit.
///   3. The item's own alternate-unit bridge, used when the two units are in different unit
///      groups (volume vs weight) and the factor therefore depends on the specific material:
///      a. source unit *is* the alternate unit -> the item's alternate factor;
///      b. otherwise convert source -> alternate unit, then apply the item's alternate factor.
///   4. No route -> false, and the caller reports the quantity as uncomputable.
///
/// Conversions are loaded once per instance. The WPF version issued a separate query per line
/// item inside its loops; the table is tiny and effectively static, so caching it removes a large
/// number of round trips without changing any result.
/// </summary>
public sealed class UnitConversionService
{
    private readonly Dictionary<(int From, int To), decimal> _factors;

    private UnitConversionService(Dictionary<(int, int), decimal> factors) => _factors = factors;

    public static async Task<UnitConversionService> CreateAsync(
        TrackingStore db, CancellationToken cancellationToken = default)
    {
        var rows = await db.UnitConversions
            .AsNoTracking()
            .Select(c => new { c.UnitFromId, c.UnitToId, c.Factor })
            .ToListAsync(cancellationToken);

        return new UnitConversionService(
            rows.ToDictionary(r => (r.UnitFromId, r.UnitToId), r => r.Factor));
    }

    /// <summary>Builds the service from an already-materialised set of conversions.</summary>
    public static UnitConversionService FromConversions(IEnumerable<UnitConversion> conversions) =>
        new(conversions.ToDictionary(c => (c.UnitFromId, c.UnitToId), c => c.Factor));

    /// <summary>
    /// Finds the factor that converts <paramref name="fromUnitId"/> into
    /// <paramref name="item"/>'s tracking unit. Returns false when no route exists.
    /// </summary>
    public bool TryGetFactor(InventoryItem item, int fromUnitId, out decimal factor)
    {
        if (fromUnitId == item.UnitOfMeasureId)
        {
            factor = 1M;
            return true;
        }

        if (_factors.TryGetValue((fromUnitId, item.UnitOfMeasureId), out factor))
        {
            return true;
        }

        if (item.AlternateUnitOfMeasureId.HasValue && item.AlternateUnitOfMeasureFactor.HasValue)
        {
            if (fromUnitId == item.AlternateUnitOfMeasureId.Value)
            {
                factor = item.AlternateUnitOfMeasureFactor.Value;
                return true;
            }

            // The EF6 original called .Single() here, which threw if the intermediate conversion
            // was missing and surfaced as an unhandled exception. Returning false instead lets
            // the caller report "conversion unavailable" the same way it does for case 4.
            if (_factors.TryGetValue((fromUnitId, item.AlternateUnitOfMeasureId.Value), out var bridge))
            {
                factor = bridge * item.AlternateUnitOfMeasureFactor.Value;
                return true;
            }
        }

        factor = 0M;
        return false;
    }

    /// <summary>Converts a quantity into the item's tracking unit. Returns false if no route exists.</summary>
    public bool TryConvertQuantity(InventoryItem item, int fromUnitId, decimal quantity, out decimal converted)
    {
        if (!TryGetFactor(item, fromUnitId, out var factor))
        {
            converted = 0M;
            return false;
        }

        converted = quantity * factor;
        return true;
    }

    /// <summary>
    /// Converts a unit price into the item's tracking unit. Price moves inversely to quantity:
    /// if a gallon is 128 fl oz, a price per gallon is that price divided by 128 per fl oz.
    /// </summary>
    public bool TryConvertUnitPrice(InventoryItem item, int fromUnitId, decimal unitPrice, out decimal converted)
    {
        if (!TryGetFactor(item, fromUnitId, out var factor) || factor == 0M)
        {
            converted = 0M;
            return false;
        }

        converted = unitPrice / factor;
        return true;
    }
}
