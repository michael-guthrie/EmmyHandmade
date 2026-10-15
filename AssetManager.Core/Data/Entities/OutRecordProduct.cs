using System.ComponentModel.DataAnnotations.Schema;

namespace AssetManager.Core.Data.Entities;

/// <summary>Finished product leaving inventory.</summary>
[Table("OutRecordProduct")]
public class OutRecordProduct
{
    public int OutRecordId { get; set; }
    public int BatchProductId { get; set; }
    public decimal Quantity { get; set; }

    public OutRecord? OutRecord { get; set; }
    public BatchProduct? BatchProduct { get; set; }
}
