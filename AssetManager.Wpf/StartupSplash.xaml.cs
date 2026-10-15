using System.Windows;
using AssetManager.Core.Data;
using AssetManager.Infrastructure;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AssetManager;

/// <summary>
/// Prepares the database and the lookup cache, then opens the main window.
///
/// <para>The .NET Framework version built its connection string here by concatenating the base
/// directory with a settings value, and ran EF6's
/// <c>MigrateDatabaseToLatestVersion</c> initialiser. The connection string now comes from
/// appsettings.json, and whether to migrate at startup is a configuration switch — because this
/// app and the Blazor app can both reach a database, and only one of them should be changing its
/// schema.</para>
/// </summary>
public partial class StartupSplash : Window
{
    private readonly IDbContextFactory<TrackingStore> _dbFactory;
    private readonly IConfiguration _configuration;
    private readonly LookupCache _lookups;
    private readonly WindowManager _windows;

    public StartupSplash(
        IDbContextFactory<TrackingStore> dbFactory,
        IConfiguration configuration,
        LookupCache lookups,
        WindowManager windows)
    {
        _dbFactory = dbFactory;
        _configuration = configuration;
        _lookups = lookups;
        _windows = windows;

        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            InitializeText.Text = "Preparing data...";

            var ready = await PrepareDatabaseAsync();
            if (!ready)
            {
                Application.Current.Shutdown();
                return;
            }

            InitializeText.Text = "Loading lookups...";
            await _lookups.ReloadAsync();

            InitializeText.Text = "Preparing main screen...";
            var main = _windows.ShowSingleton<MainWindow>();
            Application.Current.MainWindow = main;
        }
        catch (Exception ex)
        {
            WeakReferenceMessenger.Default.Send(new Messages.AppError(ex, "Could not start up."));
            Application.Current.Shutdown();
        }
        finally
        {
            Close();
        }
    }

    /// <summary>
    /// Returns false if the user chose not to continue after a problem.
    /// </summary>
    private async Task<bool> PrepareDatabaseAsync()
    {
        var migrate = _configuration.GetValue("Database:MigrateOnStartup", false);
        var seed = _configuration.GetValue("Database:SeedOnStartup", true);

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            if (migrate)
            {
                InitializeText.Text = "Updating database...";
                await db.Database.MigrateAsync();
            }

            if (seed)
            {
                InitializeText.Text = "Checking reference data...";
                await SeedData.EnsureSeededAsync(db);
            }

            // Prove the connection works before showing the main window, as the old splash did
            // by counting inventory items.
            _ = await db.InventoryItems.CountAsync();
            return true;
        }
        catch (Exception ex)
        {
            var result = MessageBox.Show(
                "Error preparing data:" + Environment.NewLine +
                ex.Message + Environment.NewLine + Environment.NewLine +
                "Continue anyway?",
                "Continue?",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No);

            return result == MessageBoxResult.Yes;
        }
    }
}
