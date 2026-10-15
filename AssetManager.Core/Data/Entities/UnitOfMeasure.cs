using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AssetManager.Core.Data.Entities;

/// <summary>A unit a quantity can be expressed in. Ids are fixed/seeded, never generated.</summary>
[Table("UnitOfMeasure")]
public class UnitOfMeasure
{
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int Id { get; set; }

    [Required, StringLength(50)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Groups units that are freely convertible. 0=count, 1=volume, 2=weight, 3=length.</summary>
    public int UnitType { get; set; }

    public ICollection<BatchItem> BatchItems { get; set; } = new HashSet<BatchItem>();
    public ICollection<BatchLossItem> BatchLossItems { get; set; } = new HashSet<BatchLossItem>();
    public ICollection<InventoryItem> InventoryItems { get; set; } = new HashSet<InventoryItem>();
    public ICollection<InventoryItem> InventoryItemAlternates { get; set; } = new HashSet<InventoryItem>();
    public ICollection<OrderItem> OrderItems { get; set; } = new HashSet<OrderItem>();
    public ICollection<UnitConversion> UnitFromConversions { get; set; } = new HashSet<UnitConversion>();
    public ICollection<UnitConversion> UnitToConversions { get; set; } = new HashSet<UnitConversion>();
}
