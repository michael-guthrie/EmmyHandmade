namespace AssetManager.Data
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("InventoryItem")]
    public partial class InventoryItem : TrackingStoreEntityBase
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public InventoryItem()
        {
            BatchItems = new HashSet<BatchItem>();
            BatchLossItems = new HashSet<BatchLossItem>();
            OrderItems = new HashSet<OrderItem>();
        }

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

        [Required]
        [StringLength(50)]
        [Index(IsUnique = true)]
        public string Name
        {
            get => _Name;
            set
            {
                if (value != _Name)
                {
                    _Name = value;
                    OnPropertyChanged(nameof(Name));
                }
            }
        }
        private string _Name;

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

        public decimal? AlternateUnitOfMeasureFactor
        {
            get => _AlternateUnitOfMeasureFactor;
            set
            {
                if (value != _AlternateUnitOfMeasureFactor)
                {
                    _AlternateUnitOfMeasureFactor = value;
                    OnPropertyChanged(nameof(AlternateUnitOfMeasureFactor));
                }
            }
        }
        private decimal? _AlternateUnitOfMeasureFactor;

        public int? AlternateUnitOfMeasureId
        {
            get => _AlternateUnitOfMeasureId;
            set
            {
                if (value != _AlternateUnitOfMeasureId)
                {
                    _AlternateUnitOfMeasureId = value;
                    OnPropertyChanged(nameof(AlternateUnitOfMeasureId));
                }
            }
        }
        private int? _AlternateUnitOfMeasureId;

        public bool IsPercentBatchOil
        {
            get => _IsPercentBatchOil;
            set
            {
                if (value != _IsPercentBatchOil)
                {
                    _IsPercentBatchOil = value;
                    OnPropertyChanged(nameof(IsPercentBatchOil));
                }
            }
        }
        private bool _IsPercentBatchOil;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<BatchItem> BatchItems { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<BatchLossItem> BatchLossItems { get; set; }

        public virtual UnitOfMeasure UnitOfMeasure { get; set; }

        public virtual UnitOfMeasure AlternateUnitOfMeasure { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<OrderItem> OrderItems { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<OutRecordMaterial> OutRecordMaterials { get; set; }
    }
}
