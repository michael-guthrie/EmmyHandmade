using System.IO;
using System.Windows;
using AssetManager.Core.Data;
using AssetManager.Core.Services;
using AssetManager.Infrastructure;
using AssetManager.ViewModels;
using AssetManager.Views;
using AssetManager.Views.UserControls;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AssetManager;

/// <summary>
/// Application startup.
///
/// The .NET Framework version had no composition root — each view model constructed its own
/// <c>TrackingStore</c> and each lookup resource opened its own connection. This builds a generic
/// host so the shared Core services, the lookup cache and the window manager are all resolved,
/// and so the connection string comes from appsettings.json rather than being concatenated in
/// the splash screen's code-behind.
/// </summary>
public partial class App : Application
{
    private IHost? _host;

    public IServiceProvider Services =>
        _host?.Services ?? throw new InvalidOperationException("The host has not been built yet.");

    public static new App Current => (App)Application.Current;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration(config =>
            {
                config.SetBasePath(AppContext.BaseDirectory);
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);
                config.AddEnvironmentVariables("ASSETMANAGER_");
            })
            .ConfigureServices(ConfigureServices)
            .Build();

        await _host.StartAsync();

        // Every unhandled view-model error funnels through one place, as it did before.
        WeakReferenceMessenger.Default.Register<Messages.AppError>(this, (_, message) =>
            Dispatcher.Invoke(() => ShowError(message)));

        DispatcherUnhandledException += (_, args) =>
        {
            ShowError(new Messages.AppError(args.Exception));
            args.Handled = true;
        };

        var splash = Services.GetRequiredService<StartupSplash>();
        splash.Show();
    }

    private static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
    {
        var connectionString = context.Configuration.GetConnectionString("TrackingStore")
            ?? throw new InvalidOperationException(
                "Connection string 'TrackingStore' is missing from appsettings.json.");

        // |DataDirectory| is a .NET Framework convention that the modern SqlClient does not expand
        // on its own. Resolving it here keeps the existing appsettings value working unchanged.
        connectionString = connectionString.Replace(
            "|DataDirectory|",
            AppContext.BaseDirectory,
            StringComparison.OrdinalIgnoreCase);

        // A factory, not a scoped context: each window owns a context for its lifetime, which is
        // what makes the "you have unsaved changes" prompt work.
        services.AddDbContextFactory<TrackingStore>(options => options.UseSqlServer(connectionString));

        // Shared business logic.
        services.AddSingleton<InventoryCalculationService>();
        services.AddSingleton<BatchCostingService>();
        services.AddSingleton<ExpenseReportService>();
        services.AddSingleton<LookupService>();

        // Desktop infrastructure.
        services.AddSingleton<UserSettingsService>();
        services.AddSingleton<LookupCache>();
        services.AddSingleton<WindowManager>();
        services.AddSingleton<DatabaseBackupService>();

        // View models.
        services.AddTransient<ViewInventoryViewModel>();
        services.AddTransient<InventoryItemHistoryViewModel>();
        services.AddTransient<ManageItemsViewModel>();
        services.AddTransient<ViewProductViewModel>();
        services.AddTransient<OrderHistoryViewModel>();
        services.AddTransient<OrderViewModel>();
        services.AddTransient<BatchHistoryViewModel>();
        services.AddTransient<BatchViewModel>();
        services.AddTransient<OutputHistoryViewModel>();
        services.AddTransient<OutputViewModel>();
        services.AddTransient<MiscExpenseViewModel>();
        services.AddTransient<QuarterlyExpenseViewModel>();
        services.AddTransient<ExpenseDetailViewModel>();

        // Windows.
        services.AddTransient<StartupSplash>();
        services.AddTransient<MainWindow>();
        services.AddTransient<ViewInventory>();
        services.AddTransient<ViewInventoryItemHistory>();
        services.AddTransient<ManageItems>();
        services.AddTransient<ViewProducts>();
        services.AddTransient<AddProduct>();
        services.AddTransient<OrderHistory>();
        services.AddTransient<ModifyOrder>();
        services.AddTransient<BatchHistory>();
        services.AddTransient<ModifyBatch>();
        services.AddTransient<OutputHistory>();
        services.AddTransient<ModifyOutput>();
        services.AddTransient<ManageMiscExpenses>();
    }

    private static void ShowError(Messages.AppError error)
    {
        var heading = error.Context is null
            ? "Something went wrong. Details are:"
            : $"{error.Context} Details are:";

        MessageBox.Show(
            heading + Environment.NewLine + Environment.NewLine + error.Exception,
            "Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            await _host.StopAsync(TimeSpan.FromSeconds(2));
            _host.Dispose();
        }

        base.OnExit(e);
    }
}
