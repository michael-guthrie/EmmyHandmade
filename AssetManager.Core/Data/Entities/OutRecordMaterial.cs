using System.ComponentModel.DataAnnotations.Schema;

namespace AssetManager.Core.Data.Entities;

/// <summary>Raw material leaving inventory directly, always in the item's own tracking unit.</summary>
[Table("OutRecordMaterial")]
public class OutRecordMaterial
{
    public int OutRecordId { get; set; }
    public int InventoryItemId { get; set; }
    public decimal Quantity { get; set; }

    public OutRecord? OutRecord { get; set; }
    public InventoryItem? InventoryItem { get; set; }
}
