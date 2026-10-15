using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AssetManager.Core.Data.Entities;

/// <summary>A finished product that batches produce.</summary>
[Table("BatchProduct")]
public class BatchProduct
{
    public int Id { get; set; }

    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    public ICollection<Batch> Batches { get; set; } = new HashSet<Batch>();
    public ICollection<OutRecordProduct> OutRecordProducts { get; set; } = new HashSet<OutRecordProduct>();
}
