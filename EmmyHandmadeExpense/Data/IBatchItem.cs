namespace AssetManager.Data
{
    public interface IBatchItem
    {
        int BatchId { get; set; }
        int InventoryItemId { get; set; }
        decimal Quantity { get; set; }
        int UnitOfMeasureId { get; set; }
        int Id { get; set; }
    }

    public partial class BatchItem : IBatchItem { }
    public partial class BatchLossItem : IBatchItem { }
}
