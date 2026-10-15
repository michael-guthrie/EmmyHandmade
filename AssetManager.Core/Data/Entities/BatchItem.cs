using System.ComponentModel.DataAnnotations.Schema;

namespace AssetManager.Core.Data.Entities;

/// <summary>Material deliberately consumed by a batch.</summary>
[Table("BatchItem")]
public class BatchItem : IBatchItem
{
    public int Id { get; set; }
    public int BatchId { get; set; }
    public int InventoryItemId { get; set; }
    public decimal Quantity { get; set; }
    public int UnitOfMeasureId { get; set; }

    public Batch? Batch { get; set; }
    public InventoryItem? InventoryItem { get; set; }
    public UnitOfMeasure? UnitOfMeasure { get; set; }
}
