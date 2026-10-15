using AssetManager.Core.Services;
using AssetManager.Infrastructure;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

namespace AssetManager.ViewModels;

/// <summary>
/// The date-range expense breakdown on the main window.
///
/// The whole calculation — including the end-of-period inventory and product values, which used
/// to be computed here by looping every item through the calculation helper — is now one call to
/// the shared report service.
/// </summary>
public sealed partial class ExpenseDetailViewModel : ObservableObject
{
    private readonly ExpenseReportService _reports;

    [ObservableProperty]
    private DateTime _dateOne = DateTime.Today;

    [ObservableProperty]
    private DateTime _dateTwo = DateTime.Today;

    [ObservableProperty]
    private decimal? _purchasesTotal;

    [ObservableProperty]
    private decimal? _shippingTotal;

    [ObservableProperty]
    private decimal? _taxTotal;

    [ObservableProperty]
    private decimal? _discountTotal;

    [ObservableProperty]
    private decimal? _miscTotal;

    [ObservableProperty]
    private decimal? _salesTotal;

    [ObservableProperty]
    private decimal? _grandTotal;

    [ObservableProperty]
    private string? _eopMaterialTotal;

    [ObservableProperty]
    private string? _eopProductTotal;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SearchCommand))]
    private bool _isSearching;

    public ExpenseDetailViewModel(ExpenseReportService reports) => _reports = reports;

    private bool CanSearch() => !IsSearching;

    [RelayCommand(CanExecute = nameof(CanSearch))]
    private async Task SearchAsync()
    {
        IsSearching = true;
        EopMaterialTotal = "Loading...";
        EopProductTotal = "Loading...";

        try
        {
            var detail = await _reports.GetRangeDetailAsync(DateOne, DateTwo);

            PurchasesTotal = detail.Purchases;
            ShippingTotal = detail.Shipping;
            TaxTotal = detail.Tax;
            DiscountTotal = detail.Discount;
            MiscTotal = detail.Miscellaneous;
            SalesTotal = detail.Sales;
            GrandTotal = detail.GrandTotal;

            EopMaterialTotal = detail.EndOfPeriodMaterialValue.ToString("0.00####")
                + (detail.MaterialValueComplete ? string.Empty : " (incomplete)");
            EopProductTotal = detail.EndOfPeriodProductValue.ToString("0.00####");
        }
        catch (Exception ex)
        {
            EopMaterialTotal = null;
            EopProductTotal = null;
            WeakReferenceMessenger.Default.Send(new Messages.AppError(ex));
        }
        finally
        {
            IsSearching = false;
        }
    }
}
