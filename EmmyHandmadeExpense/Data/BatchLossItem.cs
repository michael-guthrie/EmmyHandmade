namespace AssetManager.Data
{
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("BatchLossItem")]
    public partial class BatchLossItem : TrackingStoreEntityBase
    {
        public int Id
        {
            get => _Id;
            set
            {
                if (value != _Id)
                {
                    _Id = value;
                    OnPropertyChanged(nameof(Id));
                }
            }
        }
        private int _Id;

        public int BatchId
        {
            get => _BatchId;
            set
            {
                if (value != _BatchId)
                {
                    _BatchId = value;
                    OnPropertyChanged(nameof(BatchId));
                }
            }
        }
        private int _BatchId;

        public int InventoryItemId
        {
            get => _InventoryItemId;
            set
            {
                if (value != _InventoryItemId)
                {
                    _InventoryItemId = value;
                    OnPropertyChanged(nameof(InventoryItemId));
                }
            }
        }
        private int _InventoryItemId;

        public decimal Quantity
        {
            get => _Quantity;
            set
            {
                if (value != _Quantity)
                {
                    _Quantity = value;
                    OnPropertyChanged(nameof(Quantity));
                }
            }
        }
        private decimal _Quantity;

        public int UnitOfMeasureId
        {
            get => _UnitOfMeasureId;
            set
            {
                if (value != _UnitOfMeasureId)
                {
                    _UnitOfMeasureId = value;
                    OnPropertyChanged(nameof(UnitOfMeasureId));
                }
            }
        }
        private int _UnitOfMeasureId;

        public virtual Batch Batch { get; set; }

        public virtual InventoryItem InventoryItem { get; set; }

        public virtual UnitOfMeasure UnitOfMeasure { get; set; }
    }
}
