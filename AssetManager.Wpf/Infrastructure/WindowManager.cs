using System.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace AssetManager.Infrastructure;

/// <summary>
/// Owns the set of open child windows, replacing the two <c>CommonWindowHandling</c> generic
/// methods on the old MainWindow.
///
/// Behaviour is unchanged: one window of each type at a time, activated rather than reopened if
/// already up, remembered size restored on load. The difference is that windows are resolved from
/// the DI container, so their view models get the shared services instead of each view model
/// newing up its own <c>TrackingStore</c>.
/// </summary>
public sealed class WindowManager
{
    private readonly IServiceProvider _services;
    private readonly UserSettingsService _settings;
    private readonly List<Window> _openWindows = [];

    public WindowManager(IServiceProvider services, UserSettingsService settings)
    {
        _services = services;
        _settings = settings;
    }

    public IReadOnlyList<Window> OpenWindows => _openWindows;

    /// <summary>
    /// Shows the single instance of <typeparamref name="TWindow"/>, creating it if it is not
    /// already open and bringing it to the front if it is.
    /// </summary>
    public TWindow ShowSingleton<TWindow>(Window? owner = null) where TWindow : Window
    {
        var existing = _openWindows.OfType<TWindow>().FirstOrDefault();
        if (existing is not null)
        {
            if (existing.WindowState == WindowState.Minimized) existing.WindowState = WindowState.Normal;
            existing.Activate();
            return existing;
        }

        var window = _services.GetRequiredService<TWindow>();
        Register(window, owner);
        window.Show();
        return window;
    }

    /// <summary>
    /// Shows a window that can legitimately have several instances open at once — the order and
    /// batch editors, which the user may want side by side for different records.
    /// </summary>
    public TWindow Show<TWindow>(Action<TWindow>? configure = null, Window? owner = null)
        where TWindow : Window
    {
        var window = _services.GetRequiredService<TWindow>();
        configure?.Invoke(window);
        Register(window, owner);
        window.Show();
        return window;
    }

    /// <summary>Shows a modal dialog resolved from the container.</summary>
    public bool? ShowDialog<TWindow>(Action<TWindow>? configure = null, Window? owner = null)
        where TWindow : Window
    {
        var window = _services.GetRequiredService<TWindow>();
        configure?.Invoke(window);
        window.Owner = owner ?? Application.Current.MainWindow;
        _settings.TrackWindowSize(window);
        return window.ShowDialog();
    }

    private void Register(Window window, Window? owner)
    {
        if (owner is not null) window.Owner = owner;

        _openWindows.Add(window);
        window.Loaded += (_, _) => _settings.TrackWindowSize(window);
        window.Closed += (_, _) =>
        {
            _openWindows.Remove(window);
            (window.DataContext as IDisposable)?.Dispose();
            window.DataContext = null;
        };
    }

    /// <summary>
    /// Closes every open child window. Used when the main window is closing and the user has
    /// confirmed.
    /// </summary>
    public void CloseAll()
    {
        foreach (var window in _openWindows.ToList())
        {
            window.Close();
        }
    }

    /// <summary>True if any open window has unsaved changes.</summary>
    public bool HasUnsavedChanges =>
        _openWindows.Any(w => (w.DataContext as ViewModels.ExpenseViewModelBase)?.HasChanges == true);
}
