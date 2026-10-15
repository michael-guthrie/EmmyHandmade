using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AssetManager.Core.Data.Entities;

/// <summary>A raw material held in inventory.</summary>
[Table("InventoryItem")]
public class InventoryItem
{
    public int Id { get; set; }

    [Required, StringLength(50)]
    public string Name { get; set; } = string.Empty;

    /// <summary>The unit this item's stock level and cost are tracked in.</summary>
    public int UnitOfMeasureId { get; set; }

    /// <summary>
    /// Bridges unit groups that have no direct conversion (e.g. gallons to ounces for a
    /// specific liquid, where the factor depends on that liquid's density).
    /// </summary>
    public decimal? AlternateUnitOfMeasureFactor { get; set; }

    public int? AlternateUnitOfMeasureId { get; set; }

    /// <summary>Marks the item as an oil, so batch percentages are computed against oil weight only.</summary>
    public bool IsPercentBatchOil { get; set; }

    public UnitOfMeasure? UnitOfMeasure { get; set; }
    public UnitOfMeasure? AlternateUnitOfMeasure { get; set; }

    public ICollection<BatchItem> BatchItems { get; set; } = new HashSet<BatchItem>();
    public ICollection<BatchLossItem> BatchLossItems { get; set; } = new HashSet<BatchLossItem>();
    public ICollection<OrderItem> OrderItems { get; set; } = new HashSet<OrderItem>();
    public ICollection<OutRecordMaterial> OutRecordMaterials { get; set; } = new HashSet<OutRecordMaterial>();
}
