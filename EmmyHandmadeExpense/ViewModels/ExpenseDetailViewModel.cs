namespace AssetManager.ViewModels
{
    using GalaSoft.MvvmLight.Command;
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    public class ExpenseDetailViewModel : GalaSoft.MvvmLight.ViewModelBase
    {
        public DateTime DateOne
        {
            get => _DateOne;
            set
            {
                if (value != _DateOne)
                {
                    _DateOne = value;
                    RaisePropertyChanged(nameof(DateOne));
                }
            }
        }
        private DateTime _DateOne = DateTime.Today;

        public DateTime DateTwo
        {
            get => _DateTwo;
            set
            {
                if (value != _DateTwo)
                {
                    _DateTwo = value;
                    RaisePropertyChanged(nameof(DateTwo));
                }
            }
        }
        private DateTime _DateTwo = DateTime.Today;

        public decimal? PurchasesTotal
        {
            get => _PurchasesTotal;
            private set
            {
                if (value != _PurchasesTotal)
                {
                    _PurchasesTotal = value;
                    RaisePropertyChanged(nameof(PurchasesTotal));
                }
            }
        }
        private decimal? _PurchasesTotal;

        public decimal? ShippingTotal
        {
            get => _ShippingTotal;
            private set
            {
                if (value != _ShippingTotal)
                {
                    _ShippingTotal = value;
                    RaisePropertyChanged(nameof(ShippingTotal));
                }
            }
        }
        private decimal? _ShippingTotal;

        public decimal? TaxTotal
        {
            get => _TaxTotal;
            private set
            {
                if (value != _TaxTotal)
                {
                    _TaxTotal = value;
                    RaisePropertyChanged(nameof(TaxTotal));
                }
            }
        }
        private decimal? _TaxTotal;

        public decimal? DiscountTotal
        {
            get => _DiscountTotal;
            private set
            {
                if (value != _DiscountTotal)
                {
                    _DiscountTotal = value;
                    RaisePropertyChanged(nameof(DiscountTotal));
                }
            }
        }
        private decimal? _DiscountTotal;

        public decimal? MiscTotal
        {
            get => _MiscTotal;
            private set
            {
                if (value != _MiscTotal)
                {
                    _MiscTotal = value;
                    RaisePropertyChanged(nameof(MiscTotal));
                }
            }
        }
        private decimal? _MiscTotal;

        public decimal? SalesTotal
        {
            get => _SalesTotal;
            private set
            {
                if (value != _SalesTotal)
                {
                    _SalesTotal = value;
                    RaisePropertyChanged(nameof(SalesTotal));
                }
            }
        }
        private decimal? _SalesTotal;

        public decimal? GrandTotal
        {
            get => _GrandTotal;
            private set
            {
                if (value != _GrandTotal)
                {
                    _GrandTotal = value;
                    RaisePropertyChanged(nameof(GrandTotal));
                }
            }
        }
        private decimal? _GrandTotal;

        public string EOPMaterialTotal
        {
            get => _EOPMaterialTotal;
            private set
            {
                if (value != _EOPMaterialTotal)
                {
                    _EOPMaterialTotal = value;
                    RaisePropertyChanged(nameof(EOPMaterialTotal));
                }
            }
        }
        private string _EOPMaterialTotal;

        public string EOPProductTotal
        {
            get => _EOPProductTotal;
            private set
            {
                if (value != _EOPProductTotal)
                {
                    _EOPProductTotal = value;
                    RaisePropertyChanged(nameof(EOPProductTotal));
                }
            }
        }
        private string _EOPProductTotal;

        public RelayCommand SearchCommand { get; private set; }

        public ExpenseDetailViewModel()
        {
            SearchCommand = new RelayCommand(DoSearch);
        }

        private async void DoSearch()
        {
            DateTime sBegin = DateOne < DateTwo ? DateOne : DateTwo,
                     sEnd = DateOne > DateTwo ? DateOne.AddDays(1.0).AddMilliseconds(-1.0) : DateTwo.AddDays(1.0).AddMilliseconds(-1.0);
            EOPMaterialTotal = "Loading...";
            EOPProductTotal = "Loading...";
            var result = await Task.Run(() =>
                {
                    using (var db = new Data.TrackingStore())
                    {
                        var qOrderExpense = from o in db.Orders
                                            where o.DatePlaced >= sBegin && o.DatePlaced <= sEnd
                                            group o by 1 into g
                                            select new
                                            {
                                                Purchases = g.Sum(o => o.OrderItems.Sum(oi => (decimal?)(oi.Quantity * oi.UnitPrice)) ?? 0M),
                                                Shipping = g.Sum(o => o.Shipping ?? 0M),
                                                Taxes = g.Sum(o => o.Tax ?? 0M),
                                                Discounts = g.Sum(o => o.Discount ?? 0M),
                                            };
                        var qOrderExpenseResult = qOrderExpense.SingleOrDefault();
                        var qMiscExpense = db.MiscellaneousExpenses
                                .Where(e => e.DateOfExpense >= sBegin && e.DateOfExpense <= sEnd)
                                .Sum(e => (decimal?)e.Amount) ?? 0M;
                        var qSalesTotal = db.OutRecords.Where(or => or.Date >= sBegin && or.Date <= sEnd).Sum(or => (decimal?)or.TotalSale) ?? 0M;
                        return new
                        {
                            Purchases = qOrderExpenseResult?.Purchases ?? 0M,
                            Shipping = qOrderExpenseResult?.Shipping ?? 0M,
                            Taxes = qOrderExpenseResult?.Taxes ?? 0M,
                            Discounts = qOrderExpenseResult?.Discounts ?? 0M,
                            Misc = qMiscExpense,
                            SalesTotal = qSalesTotal,
                        };
                    }
                });
            PurchasesTotal = result.Purchases;
            ShippingTotal = result.Shipping;
            TaxTotal = result.Taxes;
            MiscTotal = result.Misc;
            DiscountTotal = result.Discounts;
            SalesTotal = result.SalesTotal;
            GrandTotal = result.Purchases + result.Shipping + result.Taxes + result.Misc - result.Discounts - result.SalesTotal;
            EOPMaterialTotal = await Task.Run(() =>
                {
                    using (var db = new Data.TrackingStore())
                    {
                        var qMaterialTotal = db.InventoryItems.ToList().Sum(i =>
                        {
                            decimal quantity, value;
                            return Helpers.InventoryCalculationHelper.TryGetTotalAtDate(i, db, sEnd, out quantity,
                                out value)
                                ? value
                                : 0M;
                        });
                        return qMaterialTotal.ToString("0.00####");
                    }
                });
            EOPProductTotal = await Task.Run(() =>
                {
                    using (var db = new Data.TrackingStore())
                    {
                        var qProductsValue = db.BatchProducts.ToList().Sum(p =>
                        {
                            decimal totalProduced, totalOut, remainingValue;
                            Helpers.InventoryCalculationHelper.GetTotalAtDate(p, db, sEnd, out totalProduced,
                                out totalOut, out remainingValue);
                            return remainingValue;
                        });
                        return qProductsValue.ToString("0.00####");
                    }
                });
        }
    }
}
