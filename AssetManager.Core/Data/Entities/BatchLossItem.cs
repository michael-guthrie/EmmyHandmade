using System.ComponentModel.DataAnnotations.Schema;

namespace AssetManager.Core.Data.Entities;

/// <summary>Material lost (spilled, spoiled, scrapped) during a batch.</summary>
[Table("BatchLossItem")]
public class BatchLossItem : IBatchItem
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
