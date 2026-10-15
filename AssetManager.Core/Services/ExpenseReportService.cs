using AssetManager.Core.Data;
using AssetManager.Core.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AssetManager.Core.Services;

/// <summary>
/// Port of the WPF <c>QuarterlyExpenseViewModel</c> and <c>ExpenseDetailViewModel</c> calculations:
/// the "Transaction Quick Lookup" panel on the main window.
///
/// Expenses are counted against the date an order was <em>placed</em> (not received) plus any
/// miscellaneous expenses in the period. Sales are the revenue on output records in the period.
/// </summary>
public sealed class ExpenseReportService
{
    private readonly IDbContextFactory<TrackingStore> _dbFactory;
    private readonly InventoryCalculationService _inventory;

    public ExpenseReportService(
        IDbContextFactory<TrackingStore> dbFactory,
        InventoryCalculationService inventory)
    {
        _dbFactory = dbFactory;
        _inventory = inventory;
    }

    public static int CurrentQuarter => ((DateTime.Now.Month - 1) / 3) + 1;

    public static IReadOnlyList<int> AvailableQuarters { get; } = new[] { 1, 2, 3, 4 };

    /// <summary>Years that have order activity, always including the current year.</summary>
    public async Task<IReadOnlyList<int>> GetAvailableYearsAsync(CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var years = await db.Orders.AsNoTracking()
            .Select(o => o.DatePlaced.Year)
            .Distinct()
            .ToListAsync(ct);

        years.Add(DateTime.Now.Year);
        return years.Distinct().OrderBy(y => y).ToList();
    }

    public async Task<QuarterSummary> GetQuarterSummaryAsync(
        int year, int quarter, CancellationToken ct = default)
    {
        var start = new DateTime(year, (quarter * 3) - 2, 1);
        var end = new DateTime(year, quarter * 3, 1).AddMonths(1).AddMilliseconds(-1.0);

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var orderExpenses = await db.Orders.AsNoTracking()
            .Where(o => o.DatePlaced >= start && o.DatePlaced <= end)
            .Select(o => (o.Shipping ?? 0M)
                         + (o.Tax ?? 0M)
                         + (o.OrderItems.Sum(oi => (decimal?)(oi.Quantity * oi.UnitPrice)) ?? 0M)
                         - (o.Discount ?? 0M))
            .SumAsync(e => (decimal?)e, ct) ?? 0M;

        var miscExpenses = await db.MiscellaneousExpenses.AsNoTracking()
            .Where(e => e.DateOfExpense >= start && e.DateOfExpense <= end)
            .SumAsync(e => (decimal?)e.Amount, ct) ?? 0M;

        var sales = await db.OutRecords.AsNoTracking()
            .Where(o => o.Date >= start && o.Date <= end)
            .SumAsync(o => (decimal?)o.TotalSale, ct) ?? 0M;

        return new QuarterSummary(year, quarter, orderExpenses + miscExpenses, sales);
    }

    /// <summary>
    /// The individual transactions behind a quarter's totals, for the detail popup: orders placed,
    /// miscellaneous expenses, and sales, ordered by date.
    /// </summary>
    public async Task<QuarterDetail> GetQuarterDetailAsync(
        int year, int quarter, CancellationToken ct = default)
    {
        var start = new DateTime(year, (quarter * 3) - 2, 1);
        var end = new DateTime(year, quarter * 3, 1).AddMonths(1).AddMilliseconds(-1.0);

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var orders = await db.Orders.AsNoTracking()
            .Include(o => o.OrderItems)
            .Where(o => o.DatePlaced >= start && o.DatePlaced <= end)
            .OrderBy(o => o.DatePlaced)
            .ToListAsync(ct);

        var misc = await db.MiscellaneousExpenses.AsNoTracking()
            .Where(e => e.DateOfExpense >= start && e.DateOfExpense <= end)
            .OrderBy(e => e.DateOfExpense)
            .ToListAsync(ct);

        var sales = await db.OutRecords.AsNoTracking()
            .Where(o => o.Date >= start && o.Date <= end)
            .OrderBy(o => o.Date)
            .ToListAsync(ct);

        return new QuarterDetail(year, quarter, orders, misc, sales);
    }

    /// <summary>
    /// Expense and sales figures broken out for a date range, plus closing inventory value.
    /// Port of <c>ExpenseDetailViewModel.DoSearch</c>, including its date handling: the two dates
    /// may be given in either order, and the later one is extended to the end of its day.
    /// </summary>
    public async Task<RangeDetail> GetRangeDetailAsync(
        DateTime dateOne, DateTime dateTwo, CancellationToken ct = default)
    {
        var start = (dateOne < dateTwo ? dateOne : dateTwo).Date;
        var end = (dateOne > dateTwo ? dateOne : dateTwo).Date.AddDays(1.0).AddMilliseconds(-1.0);

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var orderFigures = await db.Orders.AsNoTracking()
            .Where(o => o.DatePlaced >= start && o.DatePlaced <= end)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Purchases = g.Sum(o => o.OrderItems.Sum(oi => (decimal?)(oi.Quantity * oi.UnitPrice)) ?? 0M),
                Shipping = g.Sum(o => o.Shipping ?? 0M),
                Tax = g.Sum(o => o.Tax ?? 0M),
                Discount = g.Sum(o => o.Discount ?? 0M),
            })
            .SingleOrDefaultAsync(ct);

        var misc = await db.MiscellaneousExpenses.AsNoTracking()
            .Where(e => e.DateOfExpense >= start && e.DateOfExpense <= end)
            .SumAsync(e => (decimal?)e.Amount, ct) ?? 0M;

        var sales = await db.OutRecords.AsNoTracking()
            .Where(o => o.Date >= start && o.Date <= end)
            .SumAsync(o => (decimal?)o.TotalSale, ct) ?? 0M;

        // Closing position, valued as of the end of the range.
        var materials = await _inventory.GetAllTotalsAtDateAsync(end, ct);
        var products = await _inventory.GetAllProductTotalsAtDateAsync(end, ct);

        return new RangeDetail(
            Start: start,
            End: end,
            Purchases: orderFigures?.Purchases ?? 0M,
            Shipping: orderFigures?.Shipping ?? 0M,
            Tax: orderFigures?.Tax ?? 0M,
            Discount: orderFigures?.Discount ?? 0M,
            Miscellaneous: misc,
            Sales: sales,
            // Items with no conversion route contribute zero, as in the WPF original.
            EndOfPeriodMaterialValue: materials.Where(m => m.IsComputable).Sum(m => m.Value),
            EndOfPeriodProductValue: products.Sum(p => p.RemainingValue),
            MaterialValueComplete: materials.All(m => m.IsComputable));
    }
}

/// <summary>The transactions making up one quarter's figures.</summary>
public record QuarterDetail(
    int Year,
    int Quarter,
    IReadOnlyList<Order> Orders,
    IReadOnlyList<MiscellaneousExpense> MiscellaneousExpenses,
    IReadOnlyList<OutRecord> Sales);

/// <summary>
/// Broken-out figures for an arbitrary date range, plus the closing inventory position.
/// Port of the WPF <c>ExpenseDetailViewModel</c> search.
/// </summary>
public record RangeDetail(
    DateTime Start,
    DateTime End,
    decimal Purchases,
    decimal Shipping,
    decimal Tax,
    decimal Discount,
    decimal Miscellaneous,
    decimal Sales,
    decimal EndOfPeriodMaterialValue,
    decimal EndOfPeriodProductValue,
    bool MaterialValueComplete)
{
    /// <summary>
    /// Net cost for the period: expenses less sales. Positive means the period cost more than it
    /// brought in. This is the WPF "Grand Total" formula unchanged.
    /// </summary>
    public decimal GrandTotal => Purchases + Shipping + Tax + Miscellaneous - Discount - Sales;

    public decimal TotalExpenses => Purchases + Shipping + Tax + Miscellaneous - Discount;
}
