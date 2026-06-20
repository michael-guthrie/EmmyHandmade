namespace AssetManager.Data
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("UnitOfMeasure")]
    public partial class UnitOfMeasure : TrackingStoreEntityBase
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public UnitOfMeasure()
        {
            BatchItems = new HashSet<BatchItem>();
            BatchLossItems = new HashSet<BatchLossItem>();
            InventoryItems = new HashSet<InventoryItem>();
            InventoryItemAlternates = new HashSet<InventoryItem>();
            OrderItems = new HashSet<OrderItem>();
            UnitFromConversions = new HashSet<UnitConversion>();
            UnitToConversions = new HashSet<UnitConversion>();
        }

        [DatabaseGenerated(DatabaseGeneratedOption.None)]
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

        public int UnitType
        {
            get => _UnitType;
            set
            {
                if (value != _UnitType)
                {
                    _UnitType = value;
                    OnPropertyChanged(nameof(UnitType));
                }
            }
        }
        private int _UnitType;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<BatchItem> BatchItems { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<BatchLossItem> BatchLossItems { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<InventoryItem> InventoryItems { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<InventoryItem> InventoryItemAlternates { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<OrderItem> OrderItems { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<UnitConversion> UnitFromConversions { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<UnitConversion> UnitToConversions { get; set; }
    }
}
