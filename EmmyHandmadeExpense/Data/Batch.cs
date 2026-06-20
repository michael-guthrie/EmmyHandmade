namespace AssetManager.Data
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("Batch")]
    public partial class Batch : TrackingStoreEntityBase
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Batch()
        {
            BatchItems = new HashSet<BatchItem>();
            BatchLossItems = new HashSet<BatchLossItem>();
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

        public int ProductId
        {
            get => _ProductId;
            set
            {
                if (value != _ProductId)
                {
                    _ProductId = value;
                    OnPropertyChanged(nameof(ProductId));
                }
            }
        }
        private int _ProductId;

        public virtual BatchProduct Product { get; set; }

        public DateTime BatchDate
        {
            get => _BatchDate;
            set
            {
                if (value != _BatchDate)
                {
                    _BatchDate = value;
                    OnPropertyChanged(nameof(BatchDate));
                }
            }
        }
        private DateTime _BatchDate;

        [StringLength(500)]
        public string Description
        {
            get => _Description;
            set
            {
                if (value != _Description)
                {
                    _Description = value;
                    OnPropertyChanged(nameof(Description));
                }
            }
        }
        private string _Description;

        public decimal? Cost
        {
            get => _Cost;
            set
            {
                if (value != _Cost)
                {
                    _Cost = value;
                    OnPropertyChanged(nameof(Cost));
                }
            }
        }
        private decimal? _Cost;

        public decimal? UnitsProduced
        {
            get => _UnitsProduced;
            set
            {
                if (value != _UnitsProduced)
                {
                    _UnitsProduced = value;
                    OnPropertyChanged(nameof(UnitsProduced));
                }
            }
        }
        private decimal? _UnitsProduced;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<BatchItem> BatchItems { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<BatchLossItem> BatchLossItems { get; set; }
    }
}
