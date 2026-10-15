using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using AssetManager.Core.Services;

namespace AssetManager.Helpers;

/// <summary>
/// Lays the inventory list out for printing, as in the .NET Framework app.
///
/// One change: column widths were measured with <c>System.Drawing.Graphics.MeasureString</c>,
/// which would now mean taking a dependency on System.Drawing.Common purely to measure text in a
/// WPF app. <see cref="FormattedText"/> does the same job with the font WPF will actually render,
/// so the measurements are if anything more accurate.
///
/// The old <c>GridPaginator</c> alongside this one is not ported: it cast every DataGrid row to
/// <c>List&lt;object&gt;</c>, which no grid in this app ever held, so it could not have run. See
/// README-PORT.md.
/// </summary>
public sealed class InventoryPaginator : DocumentPaginator
{
    private readonly List<DocumentPage> _pages;

    public override bool IsPageCountValid { get; }

    public override int PageCount { get; }

    public override Size PageSize { get; set; }

    public override IDocumentPaginatorSource? Source => null;

    private InventoryPaginator(List<DocumentPage> pages)
    {
        IsPageCountValid = pages.Count > 0;
        PageCount = pages.Count;
        _pages = pages;
    }

    public override DocumentPage GetPage(int pageNumber) => _pages[pageNumber];

    public static InventoryPaginator Create(
        IReadOnlyList<InventoryTotal> items,
        DateTime asOfDate,
        Size pageSize,
        Rect printArea,
        double pixelsPerDip)
    {
        var isValid = !pageSize.IsEmpty && pageSize.Height > 40.0 && pageSize.Width > 200.0;
        if (!isValid || items.Count == 0)
        {
            return new InventoryPaginator([]);
        }

        const double rowHeight = 20.0;
        var rowsPerPage = (int)(pageSize.Height / rowHeight);
        var dataRowsPerPage = rowsPerPage - 2;
        if (dataRowsPerPage < 1) return new InventoryPaginator([]);

        var colWidths = CalculateColumnWidths(items, pixelsPerDip);
        var docPages = new List<DocumentPage>();
        var printedRows = 0;

        while (printedRows < items.Count)
        {
            var pageContent = new Grid();

            // Four data columns with 20pt gutters between them.
            foreach (var width in colWidths)
            {
                pageContent.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(width) });
                pageContent.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20.0) });
            }
            pageContent.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });

            for (var i = 0; i < rowsPerPage; i++)
            {
                pageContent.RowDefinitions.Add(new RowDefinition { Height = new GridLength(rowHeight) });
            }

            AddHeader(pageContent, asOfDate);

            var row = 1;
            foreach (var item in items.Skip(docPages.Count * dataRowsPerPage).Take(dataRowsPerPage))
            {
                AddRow(pageContent, item, ++row);
            }

            pageContent.Measure(pageSize);
            pageContent.Arrange(printArea);

            docPages.Add(new DocumentPage(pageContent, pageSize, new Rect(pageSize), printArea));
            printedRows += dataRowsPerPage;
        }

        return new InventoryPaginator(docPages) { PageSize = pageSize };
    }

    private static List<double> CalculateColumnWidths(IReadOnlyList<InventoryTotal> items, double pixelsPerDip)
    {
        // Widths come from the longest value each column actually holds, with a floor so short
        // columns still fit their heading.
        var nameChars = Math.Max(items.Max(i => i.Item.Name.Length), 4);
        var quantityChars = Math.Max(items.Max(i => Format(i.Quantity, i.IsComputable).Length), 8);
        var unitChars = Math.Max(items.Max(i => UnitText(i).Length), 5);
        var valueChars = Math.Max(items.Max(i => Format(i.Value, i.IsComputable).Length), 5);

        return
        [
            MeasureChars(nameChars, pixelsPerDip),
            MeasureChars(quantityChars, pixelsPerDip),
            MeasureChars(unitChars, pixelsPerDip),
            MeasureChars(valueChars, pixelsPerDip),
        ];
    }

    private static double MeasureChars(int count, double pixelsPerDip) =>
        MakeText(new string('a', count), pixelsPerDip).Width;

    private static FormattedText MakeText(string text, double pixelsPerDip, bool bold = false) =>
        new(text,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface(
                new FontFamily("Segoe UI"),
                FontStyles.Normal,
                bold ? FontWeights.Bold : FontWeights.Normal,
                FontStretches.Normal),
            12.0,
            Brushes.Black,
            pixelsPerDip);

    private static string Format(decimal value, bool isComputable) =>
        isComputable ? value.ToString("N2") : "N/A";

    private static string UnitText(InventoryTotal total) =>
        total.IsComputable ? total.Item.UnitOfMeasure?.Name ?? string.Empty : "Conversion Error";

    private static void AddHeader(Grid g, DateTime inventoryDate)
    {
        AddCell(g, $"Inventory as of: {inventoryDate:d}", 0, 0, HorizontalAlignment.Center, bold: true, columnSpan: 99);
        AddCell(g, "Name", 0, 1, HorizontalAlignment.Left, bold: true);
        AddCell(g, "Quantity", 2, 1, HorizontalAlignment.Center, bold: true);
        AddCell(g, "Units", 4, 1, HorizontalAlignment.Center, bold: true);
        AddCell(g, "Value", 6, 1, HorizontalAlignment.Center, bold: true);
    }

    private static void AddRow(Grid g, InventoryTotal item, int rowNumber)
    {
        AddCell(g, item.Item.Name, 0, rowNumber, HorizontalAlignment.Left);
        AddCell(g, Format(item.Quantity, item.IsComputable), 2, rowNumber, HorizontalAlignment.Right);
        AddCell(g, UnitText(item), 4, rowNumber, HorizontalAlignment.Center);
        AddCell(g, Format(item.Value, item.IsComputable), 6, rowNumber, HorizontalAlignment.Right);
    }

    private static void AddCell(
        Grid g, string text, int column, int row,
        HorizontalAlignment alignment, bool bold = false, int columnSpan = 1)
    {
        var block = new TextBlock
        {
            Text = text,
            HorizontalAlignment = alignment,
            FontWeight = bold ? FontWeights.Bold : FontWeights.Normal,
        };

        Grid.SetColumn(block, column);
        Grid.SetRow(block, row);
        if (columnSpan > 1) Grid.SetColumnSpan(block, columnSpan);

        g.Children.Add(block);
    }
}
