namespace AssetManager.Data
{
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("OrderItem")]
    public partial class OrderItem : TrackingStoreEntityBase
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

        public int OrderId
        {
            get => _OrderId;
            set
            {
                if (value != _OrderId)
                {
                    _OrderId = value;
                    OnPropertyChanged(nameof(OrderId));
                }
            }
        }
        private int _OrderId;

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

        public decimal UnitPrice
        {
            get => _UnitPrice;
            set
            {
                if (value != _UnitPrice)
                {
                    _UnitPrice = value;
                    OnPropertyChanged(nameof(UnitPrice));
                }
            }
        }
        private decimal _UnitPrice;

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

        public virtual InventoryItem InventoryItem { get; set; }

        public virtual Order Order { get; set; }

        public virtual UnitOfMeasure UnitOfMeasure { get; set; }
    }
}
