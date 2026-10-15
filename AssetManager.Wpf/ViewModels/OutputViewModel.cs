using AssetManager.Core.Data;
using AssetManager.Core.Data.Entities;
using AssetManager.Infrastructure;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore;

namespace AssetManager.ViewModels;

/// <summary>
/// Add or edit an output record — a sale, a loss, or anything else leaving inventory. Replaces
/// <c>OutputViewModelBase</c> / <c>AddOutputViewModel</c> / <c>EditOutputViewModel</c>.
/// </summary>
public sealed partial class OutputViewModel : ExpenseViewModelBase
{
    private readonly LookupCache _lookups;

    /// <summary>Null for a new record; set before initialising to edit an existing one.</summary>
    public int? OutRecordId { get; set; }

    public string Title => OutRecordId is null ? "Add Sale / Loss / Other Output" : "View / Edit Output Record";

    public LookupCache Lookups => _lookups;

    public OutRecord OutRecord { get; private set; } = new();

    public event EventHandler? SaveCompleted;

    public OutputViewModel(IDbContextFactory<TrackingStore> dbFactory, LookupCache lookups)
        : base(dbFactory)
    {
        _lookups = lookups;
    }

    protected override async Task LoadAsync(CancellationToken ct)
    {
        if (OutRecordId is null)
        {
            OutRecord = new OutRecord { Date = DateTime.Today };
            DataContext.OutRecords.Add(OutRecord);
        }
        else
        {
            OutRecord = await DataContext.OutRecords
                .Include(o => o.Materials)
                .Include(o => o.Products)
                .SingleAsync(o => o.Id == OutRecordId, ct);
        }

        OnPropertyChanged(nameof(OutRecord));
        OnPropertyChanged(nameof(Title));
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        // Both line tables are keyed on (record, item), so the same item cannot appear twice.
        // Caught here rather than surfacing as a primary-key violation.
        if (OutRecord.Materials.Select(m => m.InventoryItemId).Distinct().Count() != OutRecord.Materials.Count)
        {
            ReportError(
                new InvalidOperationException("The same material is listed more than once. Combine the quantities."),
                "Could not save.");
            return;
        }

        if (OutRecord.Products.Select(p => p.BatchProductId).Distinct().Count() != OutRecord.Products.Count)
        {
            ReportError(
                new InvalidOperationException("The same product is listed more than once. Combine the quantities."),
                "Could not save.");
            return;
        }

        try
        {
            await DataContext.SaveChangesAsync();

            WeakReferenceMessenger.Default.Send(new Messages.ExpensesChanged([OutRecord.Date]));
            WeakReferenceMessenger.Default.Send(new Messages.ItemsChanged(
                OutRecord.Materials.Select(m => m.InventoryItemId).Distinct().ToList()));

            SaveCompleted?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            ReportError(ex);
        }
    }
}
