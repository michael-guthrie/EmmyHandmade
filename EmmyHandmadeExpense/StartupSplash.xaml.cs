using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace AssetManager
{
    /// <summary>
    /// Interaction logic for StartupSplash.xaml
    /// </summary>
    public partial class StartupSplash : Window
    {
        public StartupSplash()
        {
            InitializeComponent();

            if (!GalaSoft.MvvmLight.ViewModelBase.IsInDesignModeStatic)
            {
                Loaded += StartupSplash_Loaded;
            }
        }

        private void StartupSplash_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeAsync();
        }

        private async void InitializeAsync()
        {
            try
            {
                InitializeText.Text = "Preparing data...";
                //if (string.IsNullOrEmpty(Properties.Settings.Default.DatabaseAppName))
                //{
                //    Properties.Settings.Default.DatabaseAppName = "Runtime1";
                //    Properties.Settings.Default.Save();
                //}
                Data.TrackingStore.ApplicationInstanceConnectionString = @"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename="
                                                                       + AppDomain.CurrentDomain.BaseDirectory 
                                                                       + @"Data\AssetManager.mdf;Initial Catalog=AssetManager_"
                                                                       + Properties.Settings.Default.DatabaseAppName
                                                                       + @";Integrated Security=True";
                bool successFlag = await Task.Run(() =>
                    {
                        var sbMigrateErrors = new StringBuilder();
                        string lastAction = null;
                        try
                        {
                            var dbConf = new Migrations.Configuration();
                            dbConf.SeedingError += (sender, e) =>
                                {
                                    sbMigrateErrors.AppendLine(e.SeedAction);
                                    sbMigrateErrors.AppendLine("  - " + e.Error.Message);
                                };
                            var migrateLatest = new MigrateDatabaseToLatestVersion<Data.TrackingStore, Migrations.Configuration>(true, dbConf);
                            Database.SetInitializer<Data.TrackingStore>(migrateLatest);
                            using (var dbInit = new Data.TrackingStore())
                            {
                                dbInit.Database.Log = sqlText => lastAction = sqlText;
                                int numII = dbInit.InventoryItems.Count();
                                dbInit.Database.Connection.Close();
                            }
                        }
                        catch (Exception ex)
                        {
                            sbMigrateErrors.AppendLine(ex.Message);
                            sbMigrateErrors.AppendLine("Last action:");
                            sbMigrateErrors.AppendLine(lastAction ?? "{null}");
                        }
                        if (sbMigrateErrors.Length > 0)
                        {
                            var result = MessageBox.Show("Error preparing data:" + Environment.NewLine + sbMigrateErrors.ToString() + Environment.NewLine + "Continue anyway?",
                                                         "Continue?", MessageBoxButton.YesNo);
                            if (result != MessageBoxResult.Yes) return false;
                        }
                        return true;
                    });
                if (successFlag)
                {
                    InitializeText.Text = "Preparing main screen...";
                    await Task.Run(() => System.Threading.Thread.Sleep(200));
                    var w = new MainWindow();
                    w.Show();
                }
            }
            catch (Exception ex)
            {
                GalaSoft.MvvmLight.Messaging.Messenger.Default.Send<Exception>(ex);
            }
            finally
            {
                Close();
            }
        }
    }
}
