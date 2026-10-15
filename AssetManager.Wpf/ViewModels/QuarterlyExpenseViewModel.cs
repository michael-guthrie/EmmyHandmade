using AssetManager.Core.Services;
using AssetManager.Infrastructure;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;

namespace AssetManager.ViewModels;

/// <summary>
/// The quarterly expense / sales panel on the main window.
///
/// Not an <see cref="ExpenseViewModelBase"/>: it never edits anything, so it has no need of a
/// long-lived tracking context. It reads through the shared report service instead.
/// </summary>
public sealed partial class QuarterlyExpenseViewModel : ObservableObject
{
    private readonly ExpenseReportService _reports;

    public IReadOnlyList<int> AvailableQuarters => ExpenseReportService.AvailableQuarters;

    [ObservableProperty]
    private IReadOnlyList<int> _availableYears = [DateTime.Now.Year];

    [ObservableProperty]
    private decimal _expenseTotal;

    [ObservableProperty]
    private decimal _salesTotal;

    private int _selectedQuarter = ExpenseReportService.CurrentQuarter;
    private int _selectedYear = DateTime.Now.Year;

    public int SelectedQuarter
    {
        get => _selectedQuarter;
        set
        {
            if (SetProperty(ref _selectedQuarter, value)) _ = LoadTotalsAsync();
        }
    }

    public int SelectedYear
    {
        get => _selectedYear;
        set
        {
            if (SetProperty(ref _selectedYear, value)) _ = LoadTotalsAsync();
        }
    }

    public QuarterlyExpenseViewModel(ExpenseReportService reports)
    {
        _reports = reports;

        WeakReferenceMessenger.Default.Register<Messages.ExpensesChanged>(this, (_, message) =>
        {
            // Only reload when a changed date falls inside the quarter on display, as before.
            var affectsCurrentView = message.Dates.Any(d =>
                d.Year == SelectedYear && ((d.Month - 1) / 3) + 1 == SelectedQuarter);

            if (affectsCurrentView) _ = LoadTotalsAsync();
        });
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        try
        {
            AvailableYears = await _reports.GetAvailableYearsAsync(ct);
            await LoadTotalsAsync(ct);
        }
        catch (Exception ex)
        {
            WeakReferenceMessenger.Default.Send(new Messages.AppError(ex));
        }
    }

    public void InitializeBackground() => _ = InitializeAsync();

    private async Task LoadTotalsAsync(CancellationToken ct = default)
    {
        try
        {
            var summary = await _reports.GetQuarterSummaryAsync(SelectedYear, SelectedQuarter, ct);
            ExpenseTotal = summary.ExpenseTotal;
            SalesTotal = summary.SalesTotal;
        }
        catch (Exception ex)
        {
            WeakReferenceMessenger.Default.Send(new Messages.AppError(ex));
        }
    }
}
