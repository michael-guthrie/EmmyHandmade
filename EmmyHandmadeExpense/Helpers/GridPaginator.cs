namespace AssetManager.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Documents;
    using System.Windows.Media;
    using d = System.Drawing;

    public class GridPaginator : DocumentPaginator
    {
        #region Fields

        private int _rows;
        private int _columns;
        private int _rowsPerPage;
        private int _columnsPerPage;
        private int _firstColumnWidth;
        private int _horizontalPageCount;
        private int _verticalPageCount;
        private System.Windows.Size _pageSize;
        private DataGrid _dataGrid;
        private List<string> _columnsList;
        private bool _isFirstColumnLarger;
        private List<Dictionary<int, Pair<int, int>>> _columnSlotsList;
        private string _strTitle;
        private string _strComment;

        #endregion Fields

        #region Constructor

        public GridPaginator(DataGrid dataGrid, System.Windows.Size pageSize, List<string> columnsList,
                             bool isFirstColumnLarger = false, string strTitle = null, string strComment = null)
        {
            _rows = dataGrid.Items.Count;
            _columns = dataGrid.Columns.Count;
            _dataGrid = dataGrid;
            _columnsList = columnsList;
            _isFirstColumnLarger = isFirstColumnLarger;
            _strTitle = strTitle;
            _strComment = strComment;

            CalculateFirstColumnWidth();

            PageSize = pageSize;

            _horizontalPageCount = HorizontalPageCount;
            _verticalPageCount = VerticalPageCount;

            GenerateColumnSlots();
        }

        #endregion Constructor

        #region Public Methods

        public override DocumentPage GetPage(int pageNumber)
        {
            double pgNumber = Math.IEEERemainder(pageNumber, _verticalPageCount) >= 0 ?
                                Math.IEEERemainder(pageNumber, _verticalPageCount) :
                                    Math.IEEERemainder(pageNumber, _verticalPageCount) + _verticalPageCount;

            int currentRow = Convert.ToInt32(_rowsPerPage * pgNumber);

            var page = new PageElement(currentRow, Math.Min(_rowsPerPage, _rows - currentRow), _dataGrid,
                                       _columnsList, _firstColumnWidth, _isFirstColumnLarger,
                                       GetColumnSlot(pageNumber), _strTitle, _strComment, GetPageNumber(pageNumber))
            {
                Width = PageSize.Width,
                Height = PageSize.Height,
            };

            page.Measure(PageSize);
            page.Arrange(new Rect(new System.Windows.Point(0, 0), PageSize));

            return new DocumentPage(page);
        }



        #endregion Public Methods

        #region Public Properties

        public override bool IsPageCountValid => true;

        public override int PageCount => _horizontalPageCount * _verticalPageCount;

        public override System.Windows.Size PageSize
        {
            get => _pageSize;
            set
            {
                _pageSize = value;
                _rowsPerPage = PageElement.RowsPerPage(PageSize.Height);
                _columnsPerPage = PageElement.ColumnsPerPage(PageSize.Width, _firstColumnWidth);

                //Can't print anything if you can't fit a row on a page
                System.Diagnostics.Debug.Assert(_rowsPerPage > 0);
            }
        }

        public override IDocumentPaginatorSource Source => null;

        public int HorizontalPageCount => (int)Math.Ceiling((_columns - 1) / (double)_columnsPerPage);

        public int VerticalPageCount => (int)Math.Ceiling(_rows / (double)_rowsPerPage);

        #endregion Public Properties

        #region Private Methods

        private void CalculateFirstColumnWidth()
        {
            int maxDataLen = 0;

            for (int i = 0; i < _dataGrid.Items.Count; i++)
            {
                List<Object> icol = (List<Object>)_dataGrid.Items[i];
                var largestDataItem = (from d in icol
                                       select d != null ? d.ToString().Length : 0).Max();
                maxDataLen = maxDataLen < largestDataItem ? largestDataItem : maxDataLen;
            }

            string strDataLen = string.Join("a", new string[maxDataLen + 1]);

            _firstColumnWidth = PageElement.CalculateBitLength(strDataLen,
                                                new d.Font("Tahoma", 8, System.Drawing.FontStyle.Regular, d.GraphicsUnit.Point));
        }

        private void GenerateColumnSlots()
        {
            _columnSlotsList = new List<Dictionary<int, Pair<int, int>>>();

            for (int i = 0; i < _horizontalPageCount; i++)
            {
                Dictionary<int, Pair<int, int>> columnSlot = new Dictionary<int, Pair<int, int>>();
                columnSlot.Add(1, new Pair<int, int>((_columnsPerPage * i) + 1,
                                                        Math.Min(_columnsPerPage * (i + 1), _columns - 1)));

                _columnSlotsList.Add(columnSlot);
            }
        }

        private Dictionary<int, Pair<int, int>> GetColumnSlot(int pageNumber)
        {
            for (int i = 0; i <= _columnSlotsList.Count; i++)
            {
                if (i == Math.Ceiling(Convert.ToDouble(pageNumber / _verticalPageCount)))
                    return _columnSlotsList[i];
            }
            return new Dictionary<int, Pair<int, int>>();
        }

        private string GetPageNumber(int intPageNumber)
        {
            string strPageNumber = String.Empty;

            if (_horizontalPageCount == 1)
                strPageNumber = (intPageNumber + 1).ToString();
            else
            { }

            return strPageNumber;
        }

        #endregion Private Methods
    }

    public class Pair<TStart, TEnd>
    {
        public TStart Start { get; set; }
        public TEnd End { get; set; }

        public Pair(TStart start, TEnd end)
        {
            Start = start;
            End = end;
        }
    }

    public class PageElement : UserControl
    {
        #region Constants

        private const int PAGE_MARGIN = 40;
        private const int HEADER_HEIGHT = 50;
        private const int LINE_HEIGHT = 20;
        private const int COLUMN_WIDTH = 60;
        private const int HEADER_CHR_WIDTH = 9;
        private const int HEADER_LINE_HEIGHT = 12;
        private const string EXCAPE_CHAR = "\r\n";
        private const string NOT_APPLICAPLE = "N/A";

        #endregion Constants

        #region Fields

        private int _currentRow;
        private int _rows;
        private DataGrid _dataGrid;
        private List<string> _columns;
        private int _firstColumnWidth;
        private bool _isFirstColumnLarger;
        private static int _columnsPerPage;
        private Dictionary<int, Pair<int, int>> _columnSlot;
        private string _strTitle;
        private string _strComment;
        private string _strPageNumber;

        #endregion Fields

        #region Constructor

        public PageElement(int currentRow, int rows, DataGrid dataGrid, List<string> columns,
                           int firstColumnWidth, bool isFirstColumnLarger, Dictionary<int, Pair<int, int>> columnSlot,
                           string strTitle, string strComment, string strPageNumber)
        {
            Margin = new Thickness(PAGE_MARGIN);
            _currentRow = currentRow;
            _rows = rows;
            _dataGrid = dataGrid;
            _columns = columns;
            _firstColumnWidth = firstColumnWidth;
            _isFirstColumnLarger = isFirstColumnLarger;
            _columnSlot = columnSlot;
            _strTitle = strTitle;
            _strComment = strComment;
            _strPageNumber = strPageNumber;
        }

        #endregion Constructor

        #region Public Static Functions

        public static int RowsPerPage(double height)
        {
            // 5 times Line Height deducted for: 1 for Title and Comments each; 2 for Page Number and 1 for Date
            return (int)Math.Floor((height - (2 * PAGE_MARGIN) - HEADER_HEIGHT - (5 * LINE_HEIGHT)) / LINE_HEIGHT);
        }

        public static int ColumnsPerPage(double width, int firstColumnWidth)
        {
            _columnsPerPage = (int)Math.Floor((width - (2 * PAGE_MARGIN) - firstColumnWidth) / COLUMN_WIDTH);
            return _columnsPerPage;
        }

        public static int CalculateBitLength(string strData, d.Font font)
        {
            using (var graphics = d.Graphics.FromImage(new d.Bitmap(1, 1)))
            {
                d.SizeF dtsize = graphics.MeasureString(strData, font);

                return Convert.ToInt32(dtsize.Width);
            }
        }

        #endregion Public Static Functions

        #region Private Functions

        private static FormattedText MakeText(string text, int fontSize)
        {
            return new FormattedText(text, CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight, new Typeface("Tahoma"), fontSize, Brushes.Black);
        }

        #endregion Private Functions

        #region Protected Functions

        protected override void OnRender(DrawingContext dc)
        {
            Point curPoint = new Point(0, 0);
            int beginCounter = 0;
            double intYAxisTracker = 0;
            double TitleHeight = 0;

            //Print Title.
            if (_strTitle != null)
            {
                int intTitleLength = CalculateBitLength(_strTitle, new d.Font("Tahoma", 9, d.FontStyle.Regular));
                curPoint.X = ((Width - (2 * PAGE_MARGIN)) / 2) - (intTitleLength / 2);
                dc.DrawText(MakeText(_strTitle, 9), curPoint);
                curPoint.Y += LINE_HEIGHT;
                curPoint.X = 0;
            }

            //Print Comment.
            if (_strTitle != null)
            {
                int intCommentLength = CalculateBitLength(_strComment, new d.Font("Tahoma", 9, d.FontStyle.Regular));
                curPoint.X = ((Width - (2 * PAGE_MARGIN)) / 2) - (intCommentLength / 2);
                dc.DrawText(MakeText(_strComment, 9), curPoint);
                curPoint.Y += LINE_HEIGHT;
                curPoint.X = 0;
            }

            //Print current Date.
            int intDatLength = CalculateBitLength(String.Format("{0:MMMM dd, yyyy}", DateTime.Now), new d.Font("Tahoma", 9, d.FontStyle.Regular));
            curPoint.X = ((Width - (2 * PAGE_MARGIN)) / 2) - (intDatLength / 2);
            dc.DrawText(MakeText(String.Format("{0:MMMM dd, yyyy}", DateTime.Now), 9), curPoint);
            curPoint.Y += LINE_HEIGHT;
            curPoint.X = 0;

            TitleHeight = curPoint.Y;

            //Print First column of header row.
            dc.DrawText(MakeText(_columns[0], 9), curPoint);
            curPoint.X += _firstColumnWidth;
            beginCounter = _columnSlot[1].Start;

            //Print other columns of header row
            for (int i = beginCounter; i <= _columnSlot[1].End; i++)
            {
                //Remove unwanted characters
                _columns[i] = _columns[i].Replace(EXCAPE_CHAR, " ");

                if (_columns[i].Length > HEADER_CHR_WIDTH)
                {
                    //Loop through to wrap the header text
                    for (int k = 0; k < _columns[i].Length; k += HEADER_CHR_WIDTH)
                    {
                        int subsLength = k > _columns[i].Length - HEADER_CHR_WIDTH ? _columns[i].Length - k : HEADER_CHR_WIDTH;
                        dc.DrawText(MakeText(_columns[i].Substring(k, subsLength), 9), curPoint);
                        curPoint.Y += HEADER_LINE_HEIGHT;
                    }
                }
                else
                    dc.DrawText(MakeText(_columns[i], 9), curPoint);

                //YAxisTracker keeps track of maximum lines used to print the headers.
                intYAxisTracker = intYAxisTracker < curPoint.Y ? curPoint.Y : intYAxisTracker;
                curPoint.X += COLUMN_WIDTH;
                curPoint.Y = TitleHeight;
            }

            //Reset X and Y pointers
            curPoint.X = 0;
            curPoint.Y += intYAxisTracker - TitleHeight;

            //Draw a solid line
            dc.DrawRectangle(System.Windows.Media.Brushes.Black, null, new Rect(curPoint, new System.Windows.Size(Width, 2)));
            curPoint.Y += HEADER_HEIGHT - (2 * LINE_HEIGHT);

            //Loop through each collection in dataGrid to print the data
            for (int i = _currentRow; i < _currentRow + _rows; i++)
            {
                List<Object> icol = (List<Object>)_dataGrid.Items[i];

                //Print first column data
                dc.DrawText(MakeText(icol[0].ToString(), 10), curPoint);
                curPoint.X += _firstColumnWidth;
                beginCounter = _columnSlot[1].Start;

                //Loop through items in the collection; Loop only the items for currect column slot.
                for (int j = beginCounter; j <= _columnSlot[1].End; j++)
                {
                    dc.DrawText(MakeText(icol[j] == null ? NOT_APPLICAPLE : icol[j].ToString(), 10), curPoint);
                    curPoint.X += COLUMN_WIDTH;
                }
                curPoint.Y += LINE_HEIGHT;
                curPoint.X = 0;
            }

            //Print Page numbers
            curPoint.Y = Height - (2 * PAGE_MARGIN) - LINE_HEIGHT;
            curPoint.X = Width - (2 * PAGE_MARGIN) - COLUMN_WIDTH;

            dc.DrawText(MakeText(_strPageNumber, 9), curPoint);

        }

        #endregion Protected Functions
    }
}
