namespace AssetManager.Data
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("MiscellaneousExpense")]
    public class MiscellaneousExpense : TrackingStoreEntityBase
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

        [Index]
        public DateTime DateOfExpense
        {
            get => _DateOfExpense;
            set
            {
                if (value != _DateOfExpense)
                {
                    _DateOfExpense = value;
                    OnPropertyChanged(nameof(DateOfExpense));
                }
            }
        }
        private DateTime _DateOfExpense;

        public decimal Amount
        {
            get => _Amount;
            set
            {
                if (value != _Amount)
                {
                    _Amount = value;
                    OnPropertyChanged(nameof(Amount));
                }
            }
        }
        private decimal _Amount;

        [Required]
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
    }
}
