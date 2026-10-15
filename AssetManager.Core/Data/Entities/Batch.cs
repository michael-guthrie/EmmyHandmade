using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AssetManager.Core.Data.Entities;

/// <summary>One production run: materials consumed and lost, producing units of a product.</summary>
[Table("Batch")]
public class Batch
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public DateTime BatchDate { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>Total cost of materials used and lost. Computed and stored when the batch is saved.</summary>
    public decimal? Cost { get; set; }

    public decimal? UnitsProduced { get; set; }

    public BatchProduct? Product { get; set; }

    public ICollection<BatchItem> BatchItems { get; set; } = new HashSet<BatchItem>();
    public ICollection<BatchLossItem> BatchLossItems { get; set; } = new HashSet<BatchLossItem>();
}
