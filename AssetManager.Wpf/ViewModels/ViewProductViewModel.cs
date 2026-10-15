using System.Collections.ObjectModel;
using AssetManager.Core.Data;
using AssetManager.Core.Data.Entities;
using AssetManager.Core.Services;
using AssetManager.Infrastructure;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore;

namespace AssetManager.ViewModels;

/// <summary>
/// Finished products with their produced / out / remaining position.
///
/// <para>The figures now come from the shared <see cref="InventoryCalculationService"/>. That is
/// a behaviour change worth knowing about: the old screen summed every batch's cost and divided
/// by every batch's units, giving a lifetime average cost per unit. The shared service walks
/// batches in date order and values what remains against the most recent production, which is
/// both what the Blazor app shows and what the rest of this app's valuation already did.</para>
/// </summary>
public sealed partial class ViewProductViewModel : ExpenseViewModelBase
{
    private readonly InventoryCalculationService _inventory;

    public ObservableCollection<ProductRow> Products { get; } = [];

    public event EventHandler? SaveCompleted;

    public ViewProductViewModel(
        IDbContextFactory<TrackingStore> dbFactory,
        InventoryCalculationService inventory)
        : base(dbFactory)
    {
        _inventory = inventory;
        WeakReferenceMessenger.Default.Register<Messages.ProductsChanged>(this, (_, _) => InitializeBackground());
        WeakReferenceMessenger.Default.Register<Messages.BatchesChanged>(this, (_, _) => InitializeBackground());
    }

    protected override async Task LoadAsync(CancellationToken ct)
    {
        var totals = await _inventory.GetAllProductTotalsCurrentAsync(ct);

        // Tracked entities, so renaming a product in the grid is saved by SaveChanges below.
        var tracked = await DataContext.BatchProducts.ToDictionaryAsync(p => p.Id, ct);

        Products.Clear();
        foreach (var total in totals)
        {
            if (!tracked.TryGetValue(total.Product.Id, out var product)) continue;
            Products.Add(new ProductRow(product, total));
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            await DataContext.SaveChangesAsync();

            WeakReferenceMessenger.Default.Send(
                new Messages.ProductsChanged(Products.Select(p => p.Product.Id).ToList()));

            SaveCompleted?.Invoke(this, EventArgs.Empty);
        }
        catch (DbUpdateException ex)
        {
            ReportError(ex, "Could not save. Product names must be unique.");
        }
        catch (Exception ex)
        {
            ReportError(ex);
        }
    }

    /// <summary>A product plus its computed position. The product itself stays editable.</summary>
    public sealed class ProductRow
    {
        public ProductRow(BatchProduct product, ProductTotal total)
        {
            Product = product;
            Total = total;
        }

        public BatchProduct Product { get; }

        public ProductTotal Total { get; }

        public decimal TotalProduced => Total.TotalProduced;
        public decimal TotalOut => Total.TotalOut;
        public decimal Remaining => Total.Remaining;
        public decimal RemainingValue => Total.RemainingValue;
        public decimal? CostPer => Total.UnitValue;
    }
}
