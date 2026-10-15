using System.ComponentModel;
using System.Windows;
using AssetManager.Infrastructure;
using AssetManager.ViewModels;
using AssetManager.Views;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Win32;

namespace AssetManager;

/// <summary>
/// The hub window. Child windows are opened through <see cref="WindowManager"/>, which replaced
/// the two generic <c>CommonWindowHandling</c> methods and the hand-kept list of open windows.
/// </summary>
public partial class MainWindow : Window
{
    private readonly WindowManager _windows;
    private readonly DatabaseBackupService _backup;

    public MainWindow(
        WindowManager windows,
        DatabaseBackupService backup,
        QuarterlyExpenseViewModel quarterlyViewModel,
        ExpenseDetailViewModel detailViewModel)
    {
        _windows = windows;
        _backup = backup;

        InitializeComponent();

        QuarterlyPanel.DataContext = quarterlyViewModel;
        DetailPanel.DataContext = detailViewModel;

        Loaded += (_, _) => quarterlyViewModel.InitializeBackground();
        Closing += OnClosing;
    }

    #region File menu

    private async void ExportDataMenuItem_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            AddExtension = true,
            CheckPathExists = true,
            DefaultExt = ".bak",
            FileName = _backup.SuggestedFileName,
            Filter = "Database backup (*.bak)|*.bak",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
            OverwritePrompt = true,
        };

        if (dialog.ShowDialog(this) != true) return;

        var previousCursor = Cursor;
        Cursor = System.Windows.Input.Cursors.Wait;
        try
        {
            await _backup.BackupAsync(dialog.FileName);

            MessageBox.Show(this,
                "Backup complete:" + Environment.NewLine + dialog.FileName,
                "Export data", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (BackupFailedException ex)
        {
            MessageBox.Show(this,
                "The backup did not complete." + Environment.NewLine + Environment.NewLine + ex.Message,
                "Export data", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch (Exception ex)
        {
            WeakReferenceMessenger.Default.Send(new Messages.AppError(ex, "Could not export the data."));
        }
        finally
        {
            Cursor = previousCursor;
        }
    }

    private void AppExitMenuItem_Click(object sender, RoutedEventArgs e) => Close();

    #endregion

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (_windows.OpenWindows.Count(w => w != this) == 0) return;

        var message = _windows.HasUnsavedChanges
            ? "You have other windows open with outstanding changes. Do you want to close all windows, losing all changes, and exit?"
            : "You have other windows open. Do you want to close all windows and exit?";

        var icon = _windows.HasUnsavedChanges ? MessageBoxImage.Warning : MessageBoxImage.Question;
        var defaultChoice = _windows.HasUnsavedChanges ? MessageBoxResult.Cancel : MessageBoxResult.OK;

        var choice = MessageBox.Show(this, message,
            _windows.HasUnsavedChanges ? "Warning!" : "Confirmation",
            MessageBoxButton.OKCancel, icon, defaultChoice);

        if (choice != MessageBoxResult.OK)
        {
            e.Cancel = true;
            return;
        }

        foreach (var window in _windows.OpenWindows.Where(w => w != this).ToList())
        {
            window.Close();
        }
    }

    #region Navigation

    private void ViewInvButton_Click(object sender, RoutedEventArgs e) =>
        _windows.ShowSingleton<ViewInventory>();

    private void ManageItemsButton_Click(object sender, RoutedEventArgs e) =>
        _windows.ShowSingleton<ManageItems>();

    private void ViewProductsButton_Click(object sender, RoutedEventArgs e) =>
        _windows.ShowSingleton<ViewProducts>();

    private void OrderHistoryButton_Click(object sender, RoutedEventArgs e) =>
        _windows.ShowSingleton<OrderHistory>();

    private void BatchHistoryButton_Click(object sender, RoutedEventArgs e) =>
        _windows.ShowSingleton<BatchHistory>();

    private void OutputHistoryButton_Click(object sender, RoutedEventArgs e) =>
        _windows.ShowSingleton<OutputHistory>();

    private void ManageMiscExpButton_Click(object sender, RoutedEventArgs e) =>
        _windows.ShowSingleton<ManageMiscExpenses>();

    private void AddOrderButton_Click(object sender, RoutedEventArgs e) =>
        _windows.Show<ModifyOrder>(w => w.LoadForNew());

    private void AddBatchButton_Click(object sender, RoutedEventArgs e) =>
        _windows.Show<ModifyBatch>(w => w.LoadForNew());

    private void AddOutputButton_Click(object sender, RoutedEventArgs e) =>
        _windows.Show<ModifyOutput>(w => w.LoadForNew());

    #endregion
}
