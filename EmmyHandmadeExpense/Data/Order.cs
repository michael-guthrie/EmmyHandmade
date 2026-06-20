namespace AssetManager.Data
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("Order")]
    public partial class Order : TrackingStoreEntityBase
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Order()
        {
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

        [StringLength(50)]
        public string Source
        {
            get => _Source;
            set
            {
                if (value != _Source)
                {
                    _Source = value;
                    OnPropertyChanged(nameof(Source));
                }
            }
        }
        private string _Source;

        [StringLength(50)]
        public string OrderNumber
        {
            get => _OrderNumber;
            set
            {
                if (value != _OrderNumber)
                {
                    _OrderNumber = value;
                    OnPropertyChanged(nameof(OrderNumber));
                }
            }
        }
        private string _OrderNumber;

        [StringLength(500)]
        public string Link
        {
            get => _Link;
            set
            {
                if (value != _Link)
                {
                    _Link = value;
                    OnPropertyChanged(nameof(Link));
                }
            }
        }
        private string _Link;

        public decimal? Tax
        {
            get => _Tax;
            set
            {
                if (value != _Tax)
                {
                    _Tax = value;
                    OnPropertyChanged(nameof(Tax));
                }
            }
        }
        private decimal? _Tax;

        public decimal? Shipping
        {
            get => _Shipping;
            set
            {
                if (value != _Shipping)
                {
                    _Shipping = value;
                    OnPropertyChanged(nameof(Shipping));
                }
            }
        }
        private decimal? _Shipping;

        public decimal? Discount
        {
            get => _Discount;
            set
            {
                if (value != _Discount)
                {
                    _Discount = value;
                    OnPropertyChanged(nameof(Discount));
                }
            }
        }
        private decimal? _Discount;

        public DateTime DatePlaced
        {
            get => _DatePlaced;
            set
            {
                if (value != _DatePlaced)
                {
                    _DatePlaced = value;
                    OnPropertyChanged(nameof(DatePlaced));
                }
            }
        }
        private DateTime _DatePlaced;

        public DateTime? DateReceived
        {
            get => _DateReceived;
            set
            {
                if (value != _DateReceived)
                {
                    _DateReceived = value;
                    OnPropertyChanged(nameof(DateReceived));
                }
            }
        }
        private DateTime? _DateReceived;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<OrderItem> OrderItems { get; set; }
    }
}
