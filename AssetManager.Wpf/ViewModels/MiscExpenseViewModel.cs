using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Text;
using AssetManager.Core.Data;
using AssetManager.Core.Data.Entities;
using AssetManager.Infrastructure;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore;

namespace AssetManager.ViewModels;

/// <summary>Miscellaneous expenses: an editable list saved in one batch.</summary>
public sealed partial class MiscExpenseViewModel : ExpenseViewModelBase
{
    public ObservableCollection<MiscellaneousExpense> Expenses { get; } = [];

    public event EventHandler<string>? ValidationFailed;

    public event EventHandler? SaveCompleted;

    public MiscExpenseViewModel(IDbContextFactory<TrackingStore> dbFactory) : base(dbFactory)
    {
        Expenses.CollectionChanged += OnExpensesChanged;
    }

    protected override async Task LoadAsync(CancellationToken ct)
    {
        var expenses = await DataContext.MiscellaneousExpenses
            .OrderByDescending(e => e.DateOfExpense)
            .ToListAsync(ct);

        Expenses.CollectionChanged -= OnExpensesChanged;
        Expenses.Clear();
        foreach (var expense in expenses) Expenses.Add(expense);
        Expenses.CollectionChanged += OnExpensesChanged;
    }

    private void OnExpensesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems is not null)
        {
            foreach (MiscellaneousExpense expense in e.NewItems)
            {
                if (expense.DateOfExpense == default) expense.DateOfExpense = DateTime.Today;
                DataContext.MiscellaneousExpenses.Add(expense);
            }
        }

        if (e.OldItems is not null)
        {
            foreach (MiscellaneousExpense expense in e.OldItems)
            {
                DataContext.MiscellaneousExpenses.Remove(expense);
            }
        }
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool _isSaving;

    private bool CanSave() => !IsSaving;

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        IsSaving = true;
        try
        {
            var sb = new StringBuilder();
            if (Expenses.Any(e => string.IsNullOrWhiteSpace(e.Description)))
            {
                sb.AppendLine("You have one or more expenses missing a description.");
            }

            if (sb.Length > 0)
            {
                ValidationFailed?.Invoke(this, sb.ToString());
                return;
            }

            await DataContext.SaveChangesAsync();

            WeakReferenceMessenger.Default.Send(
                new Messages.ExpensesChanged(Expenses.Select(e => e.DateOfExpense).ToList()));

            SaveCompleted?.Invoke(this, EventArgs.Empty);
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
}
