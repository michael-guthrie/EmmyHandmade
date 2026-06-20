namespace AssetManager
{
    using AssetManager.ViewModels;
    using AssetManager.Views;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        readonly List<Window> ChildWindows = new List<Window>();

        public MainWindow()
        {
            InitializeComponent();

            if (!GalaSoft.MvvmLight.ViewModelBase.IsInDesignModeStatic)
            {
                Closing += MainWindow_Closing;
            }
        }

        #region - Event Handlers -
        private void ExportDataMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // Prompt to where we are backing up the database.
            var fileDlg = new Microsoft.Win32.SaveFileDialog()
            {
                AddExtension = true,
                CheckFileExists = false,
                CheckPathExists = true,
                DefaultExt = ".bak",
                FileName = "AssetManagerData.bak",
                Filter = "Database backup (*.bak)|*.bak",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                OverwritePrompt = true,
            };
            var fileResult = fileDlg.ShowDialog();
            if (fileResult == true)
            {
                using (var db = new Data.TrackingStore())
                {
                    string dbName = db.Database.Connection.Database;
                    if (string.IsNullOrEmpty(dbName))
                    {
                        // Since it's not supplied, it will default to filename.  Locate the database name based on filename.
                        string dataDir
                            = (AppDomain.CurrentDomain.GetData("DataDirectory") as string)
                            ?? AppDomain.CurrentDomain.BaseDirectory;
                        string targetLocalDB = dataDir + (dataDir.EndsWith("\\") ? "Data\\TrackingStore.mdf" : "\\Data\\TrackingStore.mdf");
                        targetLocalDB = targetLocalDB.Substring(targetLocalDB.IndexOf("\\EMMY"));
                        dbName = db.Database.SqlQuery<string>("SELECT name FROM sys.databases WHERE name like '%' + @p0", targetLocalDB).Single();
                    }
                    string cmd = "BACKUP DATABASE @p0 TO DISK=@p1 WITH FORMAT;";
                    try
                    {
                        db.Database.ExecuteSqlCommand(System.Data.Entity.TransactionalBehavior.DoNotEnsureTransaction, cmd, dbName, fileDlg.FileName);
                    }
                    catch (System.Data.SqlClient.SqlException ex)
                    {
                        GalaSoft.MvvmLight.Messaging.Messenger.Default.Send<Exception>(ex);

                        var dbNames = db.Database.SqlQuery<string>("SELECT name FROM sys.databases").ToList();
                        if (!dbNames.Any(n => n == dbName))
                        {
                            MessageBox.Show("Database not found. Looking for:" +
                                Environment.NewLine + dbName +
                                Environment.NewLine + "Found:" +
                                Environment.NewLine + dbNames.Aggregate((s1, s2) => s1 + Environment.NewLine + s2));
                        }
                    }
                }
            }
        }

        private void AppExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (ChildWindows.Any())
            {
                MessageBoxResult closeChoice;
                if (ChildWindows.Any(w => (w.DataContext as ExpenseViewModelBase)?.HasChanges == true))
                {
                    closeChoice = MessageBox.Show(this, "You have other windows open with outstanding changes. Do you want to close all windows, losing all changes, and exit?", "Warning!", 
                        MessageBoxButton.OKCancel, MessageBoxImage.Warning, MessageBoxResult.Cancel);
                }
                else
                {
                    closeChoice = MessageBox.Show(this, "You have other windows open. Do you want to close all windows and exit?", "Confirmation",
                        MessageBoxButton.OKCancel, MessageBoxImage.Question, MessageBoxResult.OK);
                }
                if (closeChoice != MessageBoxResult.OK)
                {
                    e.Cancel = true;
                }
                else
                {
                    var winToClose = ChildWindows.ToList();
                    foreach (var w in winToClose)
                    {
                        w.Close();
                    }
                }
            }
        }

        private void ViewInvButton_Click(object sender, RoutedEventArgs e)
        {
            CommonWindowHandling<ViewInventory>();
        }

        private void AddOrderButton_Click(object sender, RoutedEventArgs e)
        {
            CommonWindowHandling<ModifyOrder, AddOrderViewModel>();
        }

        private void AddBatchButton_Click(object sender, RoutedEventArgs e)
        {
            CommonWindowHandling<ModifyBatch, AddBatchViewModel>();
        }

        private void OrderHistoryButton_Click(object sender, RoutedEventArgs e)
        {
            CommonWindowHandling<OrderHistory>();
        }

        private void BatchHistoryButton_Click(object sender, RoutedEventArgs e)
        {
            CommonWindowHandling<BatchHistory>();
        }

        private void ManageItemsButton_Click(object sender, RoutedEventArgs e)
        {
            CommonWindowHandling<ManageItems>();
        }

        private void ManageMiscExpButton_Click(object sender, RoutedEventArgs e)
        {
            CommonWindowHandling<ManageMiscExpenses>();
        }

        private void AddOutputButton_Click(object sender, RoutedEventArgs e)
        {
            CommonWindowHandling<ModifyOutput, AddOutputViewModel>();
        }

        private void OutputHistoryButton_Click(object sender, RoutedEventArgs e)
        {
            CommonWindowHandling<OutputHistory>();
        }

        private void ViewProductsButton_Click(object sender, RoutedEventArgs e)
        {
            CommonWindowHandling<ViewProducts>();
        }
        #endregion

        private void CommonWindowHandling<TView>() where TView : Window, new()
        {
            var w = ChildWindows.OfType<TView>().SingleOrDefault();
            if (w == null)
            {
                w = new TView();
                ChildWindows.Add(w);
                w.Loaded += ChildWindowCommonLoadHandler;
                w.Closed += (wSender, wEvt) => { ChildWindows.Remove(w); w = null; };
                w.Show();
            }
            else
            {
                w.Activate();
            }
        }

        private void CommonWindowHandling<TView, TViewModel>() where TView : Window, new() where TViewModel : ExpenseViewModelBase, new()
        {
            var w = ChildWindows.OfType<TView>().SingleOrDefault();
            if (w == null)
            {
                w = new TView();
                ChildWindows.Add(w);
                var vm = new TViewModel();
                w.Loaded += ChildWindowCommonLoadHandler;
                w.Closed += (wSender, wEvt) => { w.DataContext = null; vm.Cleanup(); vm = null; ChildWindows.Remove(w); w = null; };
                w.DataContext = vm;
                vm.InitializeBackground();
                w.Show();
            }
            else
            {
                w.Activate();
            }
        }

        private static void ChildWindowCommonLoadHandler(object sender, EventArgs e)
        {
            var src = sender as Window;
            if (src == null) return;

            var savedSize = AssetManager.Properties.Settings.Default.WindowSizes[sender.GetType().FullName];
            if (savedSize != null)
            {
                var s = Size.Parse(savedSize);
                src.Height = s.Height;
                src.Width = s.Width;
            }
            src.SizeChanged += (scSender, scEvt) =>
            {
                AssetManager.Properties.Settings.Default.WindowSizes[sender.GetType().FullName] = new Size(src.Width, src.Height).ToString();
                AssetManager.Properties.Settings.Default.Save();
            };
        }
    }
}
