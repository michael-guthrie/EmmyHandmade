namespace AssetManager.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class OrderHistoryViewModel : ExpenseViewModelBase
    {
        public class OrderHistoryItem
        {
            public int Id { get; set; }
            public string Source { get; set; }
            public DateTime OrderDate { get; set; }
            public DateTime? ReceiptDate { get; set; }
            public int OrderedItems { get; set; }
            public decimal TotalCost { get; set; }
        }

        public IEnumerable<OrderHistoryItem> Items { get; private set; }

        public OrderHistoryViewModel()
        {
            MessengerInstance.Register<IEnumerable<int>>(this, Helpers.MessengerToken.Order, orderIds => InitializeBackground());
        }

        protected override async Task InitializeAsync()
        {
            var results = await Task.Run(() =>
                {
                    var qResult = from o in DataContext.Orders
                                  select new
                                  {
                                      o.Id,
                                      o.Source,
                                      o.DatePlaced,
                                      o.DateReceived,
                                      OrderedItems = o.OrderItems.Count,
                                      TotalCost = (decimal?)((o.Shipping ?? 0M) +
                                                             (o.Tax ?? 0M) +
                                                             (o.OrderItems.Sum(oi => (decimal?)(oi.Quantity * oi.UnitPrice)) ?? 0M) -
                                                             (o.Discount ?? 0M)),
                                  };
                    return qResult.ToList();
                });
            Items = results.Select(i => new OrderHistoryItem()
                {
                    Id = i.Id,
                    Source = i.Source,
                    OrderDate = i.DatePlaced,
                    ReceiptDate = i.DateReceived,
                    OrderedItems = i.OrderedItems,
                    TotalCost = i.TotalCost ?? 0M,
                });
            RaisePropertyChanged(nameof(Items));
        }
    }
}
