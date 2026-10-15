using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AssetManager.Core.Data.Entities;

/// <summary>A business expense not tied to a material order.</summary>
[Table("MiscellaneousExpense")]
public class MiscellaneousExpense
{
    public int Id { get; set; }

    public DateTime DateOfExpense { get; set; }

    public decimal Amount { get; set; }

    [Required, StringLength(500)]
    public string Description { get; set; } = string.Empty;
}
