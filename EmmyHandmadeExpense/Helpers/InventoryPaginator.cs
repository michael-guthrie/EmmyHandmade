namespace AssetManager.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Documents;
    using ViewModels;
    using d = System.Drawing;

    public class InventoryPaginator : DocumentPaginator
    {
        private readonly List<DocumentPage> Pages;

        public override bool IsPageCountValid { get; }

        public override int PageCount { get; }

        public override Size PageSize { get; set; }

        public override IDocumentPaginatorSource Source => null;

        private InventoryPaginator(List<DocumentPage> pages)
        {
            IsPageCountValid = pages.Count > 0;
            PageCount = pages.Count;
            Pages = pages;
        }

        public override DocumentPage GetPage(int pageNumber) => Pages[pageNumber];

        public static InventoryPaginator Create(ViewInventoryViewModel model, Size pageSize, Rect printArea)
        {
            bool isValid = ((!pageSize.IsEmpty) && (pageSize.Height > 40.0) && (pageSize.Width > 200.0));
            if (!isValid)
            {
                return new InventoryPaginator(new List<DocumentPage>());
            }

            int rowsPerPage = (int)(pageSize.Height / 20);
            int dataRowsPerPage = rowsPerPage - 2;
            List<DocumentPage> docPages = new List<DocumentPage>();

            // Find the column width based on existing content.
            List<double> colWidths = CalculateColumnWidths(model.Items);

            int printedRows = 0;

            while (printedRows < model.Items.Count())
            {
                // Layout the grid to hold the page content.
                Grid pageContent = new Grid();
                pageContent.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(colWidths[0]) });
                pageContent.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(20.0) });
                pageContent.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(colWidths[1]) });
                pageContent.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(20.0) });
                pageContent.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(colWidths[2]) });
                pageContent.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(20.0) });
                pageContent.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(colWidths[3]) });
                pageContent.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1.0, GridUnitType.Star) });
                for (int i = 0; i < rowsPerPage; i++)
                {
                    pageContent.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(20.0) });
                }

                // Add content to the grid for each page.
                AddHeader(pageContent, model.InventoryAsOfDate);
                int row = 1;
                foreach (var item in model.Items.Skip(docPages.Count * dataRowsPerPage).Take(dataRowsPerPage))
                {
                    AddRow(pageContent, item, ++row);
                }

                pageContent.Measure(pageSize);
                pageContent.Arrange(printArea);

                // Add the page to the print list and continue.
                docPages.Add(new DocumentPage(pageContent, pageSize, new Rect(pageSize), printArea));
                printedRows += dataRowsPerPage;
            }

            var p = new InventoryPaginator(docPages)
            {
                PageSize = pageSize
            };
            return p;
        }

        private static List<double> CalculateColumnWidths(IEnumerable<ViewInventoryViewModel.InventoryGridItem> items)
        {
            // Find the column width based on existing content.
            List<double> colWidths = new List<double>();
            using (var graphics = d.Graphics.FromImage(new d.Bitmap(1, 1)))
            {
                d.Font f = d.SystemFonts.DefaultFont;
                string sMeasure;
                int maxLength;
                d.SizeF dtsize;

                // Name column width.
                maxLength = Math.Max(items.Select(i => i.Name.Length).Max(), 4);
                sMeasure = string.Join("a", new string[maxLength]);
                dtsize = graphics.MeasureString(sMeasure, f);
                colWidths.Add(dtsize.Width);

                // Quantity column width.
                maxLength = Math.Max(items.Select(i => $"{i.Quantity:N2}".Length).Max(), 8);
                sMeasure = string.Join("a", new string[maxLength]);
                dtsize = graphics.MeasureString(sMeasure, f);
                colWidths.Add(dtsize.Width);

                // UnitOfMeasure column width.
                maxLength = Math.Max(items.Select(i => i.UnitOfMeasure.Length).Max(), 5);
                sMeasure = string.Join("a", new string[maxLength]);
                dtsize = graphics.MeasureString(sMeasure, f);
                colWidths.Add(dtsize.Width);

                // Value column width.
                maxLength = Math.Max(items.Select(i => $"{i.Value:N2}".Length).Max(), 5);
                sMeasure = string.Join("a", new string[maxLength]);
                dtsize = graphics.MeasureString(sMeasure, f);
                colWidths.Add(dtsize.Width);
            }
            return colWidths;
        }

        private static void AddHeader(Grid g, DateTime inventoryDate)
        {
            var hTitle = new TextBlock() { Text = $"Inventory as of: {inventoryDate:d}", FontWeight = FontWeights.ExtraBold, HorizontalAlignment = HorizontalAlignment.Center };
            Grid.SetColumnSpan(hTitle, 99);
            Grid.SetRow(hTitle, 0);
            g.Children.Add(hTitle);

            var hName = new TextBlock() { Text = "Name", FontWeight = FontWeights.Bold };
            Grid.SetColumn(hName, 0);
            Grid.SetRow(hName, 1);
            g.Children.Add(hName);

            var hQuantity = new TextBlock() { Text = "Quantity", FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Center };
            Grid.SetColumn(hQuantity, 2);
            Grid.SetRow(hQuantity, 1);
            g.Children.Add(hQuantity);

            var hUnitOfMeasure = new TextBlock() { Text = "Units", FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Center };
            Grid.SetColumn(hUnitOfMeasure, 4);
            Grid.SetRow(hUnitOfMeasure, 1);
            g.Children.Add(hUnitOfMeasure);

            var hValue = new TextBlock() { Text = "Value", FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Center };
            Grid.SetColumn(hValue, 6);
            Grid.SetRow(hValue, 1);
            g.Children.Add(hValue);
        }

        private static void AddRow(Grid g, ViewInventoryViewModel.InventoryGridItem item, int rowNumber)
        {
            var rName = new TextBlock() { Text = item.Name };
            Grid.SetColumn(rName, 0);
            Grid.SetRow(rName, rowNumber);
            g.Children.Add(rName);

            var rQuantity = new TextBlock() { Text = $"{item.Quantity:N2}", HorizontalAlignment = HorizontalAlignment.Right };
            Grid.SetColumn(rQuantity, 2);
            Grid.SetRow(rQuantity, rowNumber);
            g.Children.Add(rQuantity);

            var rUnitOfMeasure = new TextBlock() { Text = item.UnitOfMeasure, HorizontalAlignment = HorizontalAlignment.Center };
            Grid.SetColumn(rUnitOfMeasure, 4);
            Grid.SetRow(rUnitOfMeasure, rowNumber);
            g.Children.Add(rUnitOfMeasure);

            var rValue = new TextBlock() { Text = $"{item.Value:N2}", HorizontalAlignment = HorizontalAlignment.Right };
            Grid.SetColumn(rValue, 6);
            Grid.SetRow(rValue, rowNumber);
            g.Children.Add(rValue);
        }
    }
}
