namespace AssetManager.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class QuarterlyExpenseViewModel : GalaSoft.MvvmLight.ViewModelBase
    {
        public IEnumerable<int> AvailableQuarters { get; } = new[] { 1, 2, 3, 4 };

        public IEnumerable<int> AvailableYears
        {
            get => _AvailableYears;
            private set
            {
                if (value != _AvailableYears)
                {
                    _AvailableYears = value;
                    RaisePropertyChanged(nameof(AvailableYears));
                }
            }
        }
        private IEnumerable<int> _AvailableYears;

        public int SelectedQuarter
        {
            get => _SelectedQuarter;
            set
            {
                if (value != _SelectedQuarter)
                {
                    _SelectedQuarter = value;
                    RaisePropertyChanged(nameof(SelectedQuarter));
                    LoadExpensesForSelectionBackground();
                }
            }
        }
        private int _SelectedQuarter;

        public int SelectedYear
        {
            get => _SelectedYear;
            set
            {
                if (value != _SelectedYear)
                {
                    _SelectedYear = value;
                    RaisePropertyChanged(nameof(SelectedYear));
                    LoadExpensesForSelectionBackground();
                }
            }
        }
        private int _SelectedYear;

        public decimal ExpenseTotal
        {
            get => _ExpenseTotal;
            private set
            {
                if (value != _ExpenseTotal)
                {
                    _ExpenseTotal = value;
                    RaisePropertyChanged(nameof(ExpenseTotal));
                }
            }
        }
        private decimal _ExpenseTotal;

        public decimal SalesTotal
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
        private decimal _SalesTotal;

        public QuarterlyExpenseViewModel()
        {
            _AvailableYears = new[] { DateTime.Now.Year };
            _SelectedYear = DateTime.Now.Year;
            _SelectedQuarter = ((DateTime.Now.Month - 1) / 3) + 1;

            MessengerInstance.Register<IEnumerable<DateTime>>(this, Helpers.MessengerToken.Expense, dates =>
                {
                    // If an expense has changed impacting the current selection, update the expense value.
                    if (dates.Any(dt => ((dt.Year == SelectedYear) && ((((dt.Month - 1) / 3) + 1) == SelectedQuarter))))
                    {
                        LoadExpensesForSelectionBackground();
                    }
                });
        }

        public async void InitializeBackground()
        {
            await InitializeAsync();
        }

        public async Task InitializeAsync()
        {
            AvailableYears = await Task.Run(() =>
                {
                    using (var db = new Data.TrackingStore())
                    {
                        var q = db.Orders.Select(o => o.DatePlaced.Year).Distinct();
                        var l = q.ToList();
                        l.Add(DateTime.Now.Year);
                        var result = l.Distinct().OrderBy(year => year).ToList();
                        db.Database.Connection.Close();
                        return result;
                    }
                });
            await LoadExpensesForSelectionAsync();
        }

        private async void LoadExpensesForSelectionBackground()
        {
            await LoadExpensesForSelectionAsync();
        }

        private async Task LoadExpensesForSelectionAsync()
        {
            DateTime qBegin = new DateTime(SelectedYear, (SelectedQuarter * 3) - 2, 1),
                     qEnd = new DateTime(SelectedYear, (SelectedQuarter * 3), 1).AddMonths(1).AddMilliseconds(-1.0);
            ExpenseTotal = await Task.Run(() =>
                {
                    using (var db = new Data.TrackingStore())
                    {
                        var orderExpenses = db.Orders
                            .Where(o => o.DatePlaced >= qBegin && o.DatePlaced <= qEnd)
                            .Select(o => (o.Shipping ?? 0M) +
                                         (o.Tax ?? 0M) +
                                         (o.OrderItems.Sum(oi => (decimal?)(oi.Quantity * oi.UnitPrice)) ?? 0M) -
                                         (o.Discount ?? 0M))
                            .Sum(e => (decimal?)e) ?? 0M;
                        var miscExpenses = db.MiscellaneousExpenses
                                .Where(e => e.DateOfExpense >= qBegin && e.DateOfExpense <= qEnd)
                                .Sum(e => (decimal?)e.Amount) ?? 0M;
                        db.Database.Connection.Close();
                        return orderExpenses + miscExpenses;
                    }
                });
            SalesTotal = await Task.Run(() =>
                {
                    using (var db = new Data.TrackingStore())
                    {
                        return db.OutRecords.Where(or => or.Date >= qBegin && or.Date <= qEnd).Sum(or => (decimal?)or.TotalSale) ?? 0M;
                    }
                });
        }
    }
}
