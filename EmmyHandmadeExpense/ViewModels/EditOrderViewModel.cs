namespace AssetManager.ViewModels
{
    using System.Linq;
    using System.Threading.Tasks;

    public class EditOrderViewModel : OrderViewModelBase
    {
        public override string Title => "View / Edit Order";

        public int OrderId { get; }

        public EditOrderViewModel(int orderId)
        {
            OrderId = orderId;
        }

        protected override async Task InitializeAsync()
        {
            var asyncLoadResult = await Task.Run(() =>
                {
                    var matchingOrder = DataContext.Orders.Single(o => o.Id == OrderId);
                    var items = matchingOrder.OrderItems.ToList().Select(oi => new OrderItemWrapper(oi) { DataContext = DataContext });
                    return new { Order = matchingOrder, Items = items };
                });
            Order = asyncLoadResult.Order;
            foreach (var oiw in asyncLoadResult.Items)
            {
                Items.Add(oiw);
            }
            RaisePropertyChanged(nameof(TotalCost));
        }
    }
}
