namespace AssetManager.ViewModels
{
    using AssetManager.Data;
    using GalaSoft.MvvmLight.Command;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class ViewProductViewModel : ExpenseViewModelBase
    {
        public class ProductItem
        {
            public BatchProduct Product { get; set; }
            public decimal? TotalProduced { get; set; }
            public decimal? TotalOut { get; set; }
            public decimal? Remaining { get; set; }
            public decimal? CostPer { get; set; }
        }

        public IList<ProductItem> Products { get; private set; }

        public RelayCommand SaveCommand { get; }

        public ViewProductViewModel()
        {
            SaveCommand = new RelayCommand(OnSave, CanSave);
        }

        protected override async Task InitializeAsync()
        {
            Products = await Task.Run(() =>
                {
                    var qProducts = DataContext.BatchProducts.Select(p =>
                        new
                        {
                            Product = p,
                            TotalProduced = p.Batches.Sum(b => b.UnitsProduced),
                            TotalOut = p.OutRecordProducts.Sum(orp => (decimal?)orp.Quantity),
                            TotalCost = p.Batches.Sum(b => b.Cost),
                        });
                    return qProducts.ToList().Select(r =>
                        new ProductItem()
                        {
                            Product = r.Product,
                            TotalProduced = r.TotalProduced,
                            TotalOut = r.TotalOut,
                            Remaining = (r.TotalOut.HasValue || r.TotalProduced.HasValue) ? r.TotalProduced.GetValueOrDefault() - r.TotalOut.GetValueOrDefault() : (decimal?)null,
                            CostPer = (r.TotalProduced.GetValueOrDefault() != 0M && r.TotalCost.HasValue) ? r.TotalCost.Value / r.TotalProduced.Value : (decimal?)null,
                        }).ToList();
                });
            RaisePropertyChanged(nameof(Products));
        }

        private void OnSave()
        {
            try
            {
                DataContext.SaveChanges();
                MessengerInstance.Send<IEnumerable<int>>(Products.Select(p => p.Product.Id), Helpers.MessengerToken.Product);
            }
            catch (Exception ex)
            {
                MessengerInstance.Send<Exception>(ex);
            }
        }

        private bool CanSave() => true;
    }
}
