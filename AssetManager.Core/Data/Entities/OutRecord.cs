using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AssetManager.Core.Data.Entities;

/// <summary>Anything leaving inventory: a sale, a giveaway, or a write-off.</summary>
[Table("OutRecord")]
public class OutRecord
{
    public int Id { get; set; }

    public DateTime Date { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>Revenue received. Zero for losses and giveaways.</summary>
    public decimal TotalSale { get; set; }

    // ObservableCollection rather than List: the WPF output window binds its two grids straight
    // at these collections, and a plain List gives the DataGrid nothing to react to when a row is
    // added or deleted. EF Core treats either the same.
    public IList<OutRecordMaterial> Materials { get; set; } = new ObservableCollection<OutRecordMaterial>();
    public IList<OutRecordProduct> Products { get; set; } = new ObservableCollection<OutRecordProduct>();
}
