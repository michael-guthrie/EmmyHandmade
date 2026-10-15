using System.Collections.ObjectModel;
using System.Collections.Specialized;
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
/// Add or edit a batch, with live costing. Replaces <c>BatchViewModelBase</c> /
/// <c>AddBatchViewModel</c> / <c>EditBatchViewModel</c>.
///
/// <para>The costing itself moved to the shared <see cref="BatchCostingService"/>. That removed
/// roughly 250 lines of inline LINQ from this class, and — more to the point — means the desktop
/// and web apps cannot drift apart on what a batch costs. It is also considerably faster: the
/// original re-read the entire purchase history once per line on every keystroke, where this
/// costs every line against a single snapshot.</para>
/// </summary>
public sealed partial class BatchViewModel : ExpenseViewModelBase
{
    private readonly BatchCostingService _costing;
    private readonly LookupCache _lookups;

    /// <summary>Null for a new batch; set before initialising to edit an existing one.</summary>
    public int? BatchId { get; set; }

    public string Title => BatchId is null ? "Add Batch" : "View / Edit Batch";

    public LookupCache Lookups => _lookups;

    public Batch Batch { get; private set; } = new();

    public ObservableCollection<BatchLineViewModel> UsedItems { get; } = [];

    public ObservableCollection<BatchLineViewModel> LossItems { get; } = [];

    public event EventHandler? SaveCompleted;

    private int _nextKey = 1;
    private BatchCostSummary? _summary;

    public BatchViewModel(
        IDbContextFactory<TrackingStore> dbFactory,
        BatchCostingService costing,
        LookupCache lookups)
        : base(dbFactory)
    {
        _costing = costing;
        _lookups = lookups;

        UsedItems.CollectionChanged += OnLinesChanged;
        LossItems.CollectionChanged += OnLinesChanged;
    }

    protected override async Task LoadAsync(CancellationToken ct)
    {
        if (BatchId is null)
        {
            Batch = new Batch { BatchDate = DateTime.Now };
            DataContext.Batches.Add(Batch);
        }
        else
        {
            Batch = await DataContext.Batches
                .Include(b => b.BatchItems)
                .Include(b => b.BatchLossItems)
                .SingleAsync(b => b.Id == BatchId, ct);

            UsedItems.CollectionChanged -= OnLinesChanged;
            LossItems.CollectionChanged -= OnLinesChanged;

            foreach (var item in Batch.BatchItems.ToList())
            {
                UsedItems.Add(CreateLine(item));
            }

            foreach (var item in Batch.BatchLossItems.ToList())
            {
                LossItems.Add(CreateLine(item));
            }

            UsedItems.CollectionChanged += OnLinesChanged;
            LossItems.CollectionChanged += OnLinesChanged;
        }

        OnPropertyChanged(nameof(Batch));
        OnPropertyChanged(nameof(Title));
        RaiseBatchFieldsChanged();

        await RecalculateAsync(ct);
    }

    private BatchLineViewModel CreateLine(IBatchItem item)
    {
        var line = new BatchLineViewModel(_nextKey++, item, _lookups);
        line.LineChanged += OnLineChanged;
        return line;
    }

    private void OnLineChanged(object? sender, EventArgs e) => _ = RecalculateAsync();

    private void OnLinesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        var isLoss = ReferenceEquals(sender, LossItems);

        if (e.NewItems is not null)
        {
            foreach (BatchLineViewModel line in e.NewItems)
            {
                // A row typed into the grid arrives via the parameterless constructor and has no
                // entity yet; which collection it landed in decides whether it is use or loss.
                line.Initialize(_nextKey++, _lookups, isLoss);
                line.LineChanged += OnLineChanged;

                if (isLoss) Batch.BatchLossItems.Add((BatchLossItem)line.Item);
                else Batch.BatchItems.Add((BatchItem)line.Item);
            }
        }

        if (e.OldItems is not null)
        {
            foreach (BatchLineViewModel line in e.OldItems)
            {
                line.LineChanged -= OnLineChanged;

                if (isLoss)
                {
                    var entity = (BatchLossItem)line.Item;
                    DataContext.BatchLossItems.Remove(entity);
                    Batch.BatchLossItems.Remove(entity);
                }
                else
                {
                    var entity = (BatchItem)line.Item;
                    DataContext.BatchItems.Remove(entity);
                    Batch.BatchItems.Remove(entity);
                }
            }
        }

        _ = RecalculateAsync();
    }

    #region Batch header fields

    public int? ProductId
    {
        get => Batch.ProductId == 0 ? null : Batch.ProductId;
        set
        {
            if (Batch.ProductId == value.GetValueOrDefault()) return;
            Batch.ProductId = value.GetValueOrDefault();
            OnPropertyChanged();
        }
    }

    public DateTime? BatchDate
    {
        get => Batch.BatchDate;
        set
        {
            if (Batch.BatchDate == value.GetValueOrDefault()) return;
            Batch.BatchDate = value.GetValueOrDefault();
            OnPropertyChanged();
            _ = RecalculateAsync();
        }
    }

    public string? Description
    {
        get => Batch.Description;
        set
        {
            if (Batch.Description == value) return;
            Batch.Description = value;
            OnPropertyChanged();
        }
    }

    public decimal? UnitsProduced
    {
        get => Batch.UnitsProduced;
        set
        {
            if (Batch.UnitsProduced == value) return;
            Batch.UnitsProduced = value;
            OnPropertyChanged();
            RefreshSummary();
        }
    }

    private void RaiseBatchFieldsChanged()
    {
        OnPropertyChanged(nameof(ProductId));
        OnPropertyChanged(nameof(BatchDate));
        OnPropertyChanged(nameof(Description));
        OnPropertyChanged(nameof(UnitsProduced));
    }

    #endregion

    #region Cost display

    public string? UsageCostDisplay => _summary?.UsageCostDisplay;
    public string? LossCostDisplay => _summary?.LossCostDisplay;
    public string? TotalCostDisplay => _summary?.TotalCostDisplay;
    public string? PerUnitCostDisplay => _summary?.PerUnitCostDisplay;

    [ObservableProperty]
    private bool _isRecalculating;

    /// <summary>
    /// Recosts every line against one snapshot of the purchase history, then recomputes the
    /// batch percentages and the aggregate totals.
    /// </summary>
    private async Task RecalculateAsync(CancellationToken ct = default)
    {
        IsRecalculating = true;
        try
        {
            var allLines = UsedItems.Concat(LossItems)
                .Select(l => new BatchLineInput(l.Key, l.InventoryItemId, l.Quantity, l.UnitOfMeasureId))
                .ToList();

            var costs = await _costing.CalculateLineCostsAsync(allLines, BatchDate, ct);
            foreach (var line in UsedItems.Concat(LossItems))
            {
                line.ApplyCost(costs.GetValueOrDefault(line.Key));
            }

            var usedInputs = UsedItems
                .Select(l => new BatchLineInput(l.Key, l.InventoryItemId, l.Quantity, l.UnitOfMeasureId))
                .ToList();

            var percentages = await _costing.CalculateUsedItemPercentagesAsync(usedInputs, ct);
            foreach (var line in UsedItems)
            {
                line.Percent = percentages.GetValueOrDefault(line.Key);
            }

            RefreshSummary();
        }
        catch (Exception ex)
        {
            ReportError(ex, "Could not recalculate batch costs.");
        }
        finally
        {
            IsRecalculating = false;
        }
    }

    private void RefreshSummary()
    {
        _summary = BatchCostingService.Summarise(
            UsedItems.Select(l => l.Cost ?? BatchLineCost.None(CostStatus.NotFound)).ToList(),
            LossItems.Select(l => l.Cost ?? BatchLineCost.None(CostStatus.NotFound)).ToList(),
            UnitsProduced);

        // The computed cost is stored on the batch; product valuation reads it back rather than
        // recosting, so it has to be kept current here as it was in the original.
        Batch.Cost = _summary.TotalCost;

        OnPropertyChanged(nameof(UsageCostDisplay));
        OnPropertyChanged(nameof(LossCostDisplay));
        OnPropertyChanged(nameof(TotalCostDisplay));
        OnPropertyChanged(nameof(PerUnitCostDisplay));
    }

    #endregion

    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            await DataContext.SaveChangesAsync();

            WeakReferenceMessenger.Default.Send(new Messages.BatchesChanged([Batch.Id]));
            WeakReferenceMessenger.Default.Send(new Messages.ItemsChanged(
                UsedItems.Concat(LossItems).Select(l => l.InventoryItemId).Distinct().ToList()));

            SaveCompleted?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            ReportError(ex);
        }
    }

    /// <summary>Called by the view after a new product is created, to select it.</summary>
    public async Task SelectNewProductAsync(int productId)
    {
        await _lookups.ReloadAsync();
        ProductId = productId;
    }
}
