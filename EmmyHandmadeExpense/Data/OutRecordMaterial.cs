namespace AssetManager.Data
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("OutRecordMaterial")]
    public partial class OutRecordMaterial : TrackingStoreEntityBase
    {
        [Key, Column(Order = 0)]
        public int OutRecordId
        {
            get => _OutRecordId;
            set
            {
                if (value != _OutRecordId)
                {
                    _OutRecordId = value;
                    OnPropertyChanged(nameof(OutRecordId));
                }
            }
        }
        private int _OutRecordId;

        [Key, Column(Order = 1)]
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

        public virtual OutRecord OutRecord { get; set; }

        public virtual InventoryItem InventoryItem { get; set; }
    }
}
