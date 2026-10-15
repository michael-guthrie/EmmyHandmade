using System.ComponentModel.DataAnnotations.Schema;

namespace AssetManager.Core.Data.Entities;

/// <summary>One purchased line. Units may differ from the item's tracking unit.</summary>
[Table("OrderItem")]
public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int InventoryItemId { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Quantity { get; set; }
    public int UnitOfMeasureId { get; set; }

    public Order? Order { get; set; }
    public InventoryItem? InventoryItem { get; set; }
    public UnitOfMeasure? UnitOfMeasure { get; set; }

    [NotMapped]
    public decimal ExtendedPrice => Quantity * UnitPrice;
}
