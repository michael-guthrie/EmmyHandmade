namespace AssetManager.Data
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("OutRecord")]
    public partial class OutRecord : TrackingStoreEntityBase
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public OutRecord()
        {
            Materials = new List<OutRecordMaterial>();
            Products = new List<OutRecordProduct>();
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

        public DateTime Date
        {
            get => _Date;
            set
            {
                if (value != _Date)
                {
                    _Date = value;
                    OnPropertyChanged(nameof(Date));
                }
            }
        }
        private DateTime _Date;

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

        public decimal TotalSale
        {
            get => _TotalSale;
            set
            {
                if (value != _TotalSale)
                {
                    _TotalSale = value;
                    OnPropertyChanged(nameof(TotalSale));
                }
            }
        }
        private decimal _TotalSale;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual IList<OutRecordMaterial> Materials { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual IList<OutRecordProduct> Products { get; set; }
    }
}
