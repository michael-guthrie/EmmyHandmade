using AssetManager.Core.Data;
using AssetManager.Core.Services;
using AssetManager.Infrastructure;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore;

namespace AssetManager.ViewModels;

/// <summary>
/// Current inventory, as of a chosen date.
///
/// The valuation itself now comes from the shared <see cref="InventoryCalculationService"/>, which
/// both apps use, rather than from a copy of the algorithm in this project.
/// </summary>
public sealed partial class ViewInventoryViewModel : ExpenseViewModelBase
{
    private readonly InventoryCalculationService _inventory;

    [ObservableProperty]
    private IReadOnlyList<InventoryTotal> _items = [];

    [ObservableProperty]
    private DateTime _inventoryAsOfDate = DateTime.Today;

    [ObservableProperty]
    private decimal _totalValue;

    /// <summary>Raised when the view should open a print dialog. The view owns the printing UI.</summary>
    public event EventHandler? Printing;

    public ViewInventoryViewModel(
        IDbContextFactory<TrackingStore> dbFactory,
        InventoryCalculationService inventory)
        : base(dbFactory)
    {
        _inventory = inventory;

        // Reload when items change elsewhere, as the messenger registration did before.
        WeakReferenceMessenger.Default.Register<Messages.ItemsChanged>(this, (_, _) => InitializeBackground());
    }

    protected override async Task LoadAsync(CancellationToken ct)
    {
        Items = await _inventory.GetAllTotalsAtDateAsync(InventoryAsOfDate, ct);

        // Items with no conversion route contribute nothing, as in the original.
        TotalValue = Items.Where(i => i.IsComputable).Sum(i => i.Value);
    }

    /// <summary>True if any row could not be valued — the view surfaces this as a warning.</summary>
    public bool HasUncomputableItems => Items.Any(i => !i.IsComputable);

    [RelayCommand]
    private Task CalculateAtDate() => InitializeAsync();

    [RelayCommand]
    private void Print() => Printing?.Invoke(this, EventArgs.Empty);

    partial void OnItemsChanged(IReadOnlyList<InventoryTotal> value) =>
        OnPropertyChanged(nameof(HasUncomputableItems));
}
