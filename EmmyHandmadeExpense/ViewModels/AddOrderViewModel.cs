namespace AssetManager.ViewModels
{
    using System;
    using System.Threading.Tasks;

    public class AddOrderViewModel : OrderViewModelBase
    {
        public override string Title => "Add Order";
        
        protected override async Task InitializeAsync()
        {
            Order = await Task.Run(() =>
                {
                    var o = DataContext.Orders.Create();
                    o.DatePlaced = DateTime.Now;
                    DataContext.Orders.Add(o);
                    return o;
                });
        }
    }
}
