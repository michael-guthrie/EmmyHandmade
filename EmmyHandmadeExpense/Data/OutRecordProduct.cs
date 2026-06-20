namespace AssetManager.Data
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("OutRecordProduct")]
    public partial class OutRecordProduct : TrackingStoreEntityBase
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
        public int BatchProductId
        {
            get => _BatchProductId;
            set
            {
                if (value != _BatchProductId)
                {
                    _BatchProductId = value;
                    OnPropertyChanged(nameof(BatchProductId));
                }
            }
        }
        private int _BatchProductId;

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

        public virtual BatchProduct BatchProduct { get; set; }
    }
}
