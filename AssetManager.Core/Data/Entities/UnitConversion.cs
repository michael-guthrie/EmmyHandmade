using System.ComponentModel.DataAnnotations.Schema;

namespace AssetManager.Core.Data.Entities;

/// <summary>Multiply a quantity in <see cref="UnitFromId"/> by <see cref="Factor"/> to get <see cref="UnitToId"/>.</summary>
[Table("UnitConversion")]
public class UnitConversion
{
    public int UnitFromId { get; set; }
    public int UnitToId { get; set; }
    public decimal Factor { get; set; }

    public UnitOfMeasure? UnitOfMeasureFrom { get; set; }
    public UnitOfMeasure? UnitOfMeasureTo { get; set; }
}
