using System.Collections.ObjectModel;
using System.Text;
using AssetManager.Core.Data;
using AssetManager.Core.Data.Entities;
using AssetManager.Infrastructure;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore;

namespace AssetManager.ViewModels;

/// <summary>
/// The editable list of inventory items, saved in one batch. Validation rules are carried over
/// from the .NET Framework version unchanged.
/// </summary>
public sealed partial class ManageItemsViewModel : ExpenseViewModelBase
{
    private readonly LookupCache _lookups;

    public ObservableCollection<InventoryItem> Items { get; } = [];

    public LookupCache Lookups => _lookups;

    /// <summary>Raised with the combined message when validation stops a save.</summary>
    public event EventHandler<string>? ValidationFailed;

    public event EventHandler? SaveCompleted;

    public ManageItemsViewModel(IDbContextFactory<TrackingStore> dbFactory, LookupCache lookups)
        : base(dbFactory)
    {
        _lookups = lookups;
        Items.CollectionChanged += OnItemsCollectionChanged;
    }

    protected override async Task LoadAsync(CancellationToken ct)
    {
        var items = await DataContext.InventoryItems.OrderBy(i => i.Name).ToListAsync(ct);

        Items.CollectionChanged -= OnItemsCollectionChanged;
        Items.Clear();
        foreach (var item in items) Items.Add(item);
        Items.CollectionChanged += OnItemsCollectionChanged;
    }

    private void OnItemsCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems is not null)
        {
            foreach (InventoryItem item in e.NewItems)
            {
                // A row added in the grid defaults to ounces by weight, as before.
                if (item.UnitOfMeasureId == 0) item.UnitOfMeasureId = 102;
                DataContext.InventoryItems.Add(item);
            }
        }

        if (e.OldItems is not null)
        {
            foreach (InventoryItem item in e.OldItems)
            {
                DataContext.InventoryItems.Remove(item);
            }
        }
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        IsSaving = true;
        try
        {
            var errors = Validate();
            if (errors.Length > 0)
            {
                ValidationFailed?.Invoke(this, errors);
                return;
            }

            foreach (var item in Items)
            {
                item.Name = item.Name.Trim();
            }

            await DataContext.SaveChangesAsync();

            WeakReferenceMessenger.Default.Send(
                new Messages.ItemsChanged(Items.Select(i => i.Id).ToList()));

            SaveCompleted?.Invoke(this, EventArgs.Empty);
        }
        catch (DbUpdateException ex)
        {
            // Foreign keys are Restrict, so removing an item that history references fails here
            // rather than orphaning the history.
            ReportError(ex, "Could not save. An item you removed may still be used by an order, batch or output record.");
        }
        catch (Exception ex)
        {
            ReportError(ex);
        }
        finally
        {
            IsSaving = false;
        }
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool _isSaving;

    private bool CanSave() => !IsSaving;

    private string Validate()
    {
        var validUnitIds = _lookups.Units.Select(u => u.Key).ToHashSet();
        var sb = new StringBuilder();

        foreach (var item in Items)
        {
            var label = string.IsNullOrWhiteSpace(item.Name) ? "(unnamed item)" : item.Name;

            if (!validUnitIds.Contains(item.UnitOfMeasureId))
            {
                sb.AppendLine($"You need to select valid units for {label}.");
            }

            if (item.AlternateUnitOfMeasureFactor.HasValue != item.AlternateUnitOfMeasureId.HasValue)
            {
                sb.AppendLine(
                    $"For alternate unit conversion of {label} you must provide both alternate units and a conversion factor.");
            }
        }

        if (Items.Any(i => string.IsNullOrWhiteSpace(i.Name)))
        {
            sb.AppendLine("You have one or more items missing a name.");
        }

        if (Items.Count != Items.Select(i => i.Name?.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count())
        {
            sb.AppendLine("You have one or more items with a duplicate name.");
        }

        return sb.ToString();
    }
}
