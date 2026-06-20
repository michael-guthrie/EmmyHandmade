namespace AssetManager.Data
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("UnitConversion")]
    public partial class UnitConversion : TrackingStoreEntityBase
    {
        [Key]
        [Column(Order = 0)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int UnitFromId
        {
            get => _UnitFromId;
            set
            {
                if (value != _UnitFromId)
                {
                    _UnitFromId = value;
                    OnPropertyChanged(nameof(UnitFromId));
                }
            }
        }
        private int _UnitFromId;

        [Key]
        [Column(Order = 1)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int UnitToId
        {
            get => _UnitToId;
            set
            {
                if (value != _UnitToId)
                {
                    _UnitToId = value;
                    OnPropertyChanged(nameof(UnitToId));
                }
            }
        }
        private int _UnitToId;

        public decimal Factor
        {
            get => _Factor;
            set
            {
                if (value != _Factor)
                {
                    _Factor = value;
                    OnPropertyChanged(nameof(Factor));
                }
            }
        }
        private decimal _Factor;

        public virtual UnitOfMeasure UnitOfMeasureFrom { get; set; }

        public virtual UnitOfMeasure UnitOfMeasureTo { get; set; }
    }
}
