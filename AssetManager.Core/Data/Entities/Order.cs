using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AssetManager.Core.Data.Entities;

/// <summary>A purchase of raw materials. Stock only counts once <see cref="DateReceived"/> is set.</summary>
[Table("Order")]
public class Order
{
    public int Id { get; set; }

    [StringLength(50)]
    public string? Source { get; set; }

    [StringLength(50)]
    public string? OrderNumber { get; set; }

    [StringLength(500)]
    public string? Link { get; set; }

    public decimal? Tax { get; set; }
    public decimal? Shipping { get; set; }
    public decimal? Discount { get; set; }

    public DateTime DatePlaced { get; set; }

    /// <summary>Null until the order arrives. Inventory valuation ignores unreceived orders.</summary>
    public DateTime? DateReceived { get; set; }

    public ICollection<OrderItem> OrderItems { get; set; } = new HashSet<OrderItem>();

    /// <summary>Line totals plus tax and shipping, less discount. Matches the quarterly expense formula.</summary>
    [NotMapped]
    public decimal Total =>
        (Shipping ?? 0M) + (Tax ?? 0M)
        + OrderItems.Sum(oi => oi.Quantity * oi.UnitPrice)
        - (Discount ?? 0M);
}
