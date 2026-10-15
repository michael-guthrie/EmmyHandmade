namespace AssetManager.Core.Data.Entities;

/// <summary>Shared shape of the two batch consumption lines, so costing can treat them uniformly.</summary>
public interface IBatchItem
{
    int Id { get; set; }
    int BatchId { get; set; }
    int InventoryItemId { get; set; }
    decimal Quantity { get; set; }
    int UnitOfMeasureId { get; set; }
}
