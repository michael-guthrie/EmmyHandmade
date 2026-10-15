using AssetManager.Core.Data;
using AssetManager.Infrastructure;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore;

namespace AssetManager.ViewModels;

/// <summary>
/// Base class for the window view models, ported from MvvmLight's <c>ViewModelBase</c> to
/// CommunityToolkit's <c>ObservableObject</c>.
///
/// <para>The ownership model is deliberately unchanged: each window's view model holds one
/// <see cref="TrackingStore"/> for its lifetime and edits entities tracked by it, which is what
/// makes <see cref="HasChanges"/> — and therefore the "you have unsaved changes" prompt on
/// exit — work. The difference is that the context now comes from an injected factory rather than
/// being constructed with a static connection string.</para>
/// </summary>
public abstract partial class ExpenseViewModelBase : ObservableObject, IDisposable
{
    private readonly IDbContextFactory<TrackingStore> _dbFactory;
    private TrackingStore? _dataContext;
    private bool _disposed;

    protected ExpenseViewModelBase(IDbContextFactory<TrackingStore> dbFactory) => _dbFactory = dbFactory;

    /// <summary>The context this window's edits are tracked in. Created on first use.</summary>
    protected TrackingStore DataContext => _dataContext ??= _dbFactory.CreateDbContext();

    public bool HasChanges => _dataContext?.ChangeTracker.HasChanges() ?? false;

    [ObservableProperty]
    private bool _isInitializing;

    public event EventHandler? Initialized;

    /// <summary>
    /// Loads the window's data. Errors are reported through the messenger, exactly as the
    /// MvvmLight version did, so one handler in App shows them all.
    /// </summary>
    public async Task InitializeAsync(CancellationToken ct = default)
    {
        IsInitializing = true;
        try
        {
            await LoadAsync(ct);
        }
        catch (Exception ex)
        {
            WeakReferenceMessenger.Default.Send(new Messages.AppError(ex));
        }
        finally
        {
            IsInitializing = false;
        }

        Initialized?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Fire-and-forget initialisation, for calling from a window constructor.</summary>
    public void InitializeBackground() => _ = InitializeAsync();

    protected abstract Task LoadAsync(CancellationToken ct);

    /// <summary>Reports an error through the same channel as a failed load.</summary>
    protected static void ReportError(Exception ex, string? context = null) =>
        WeakReferenceMessenger.Default.Send(new Messages.AppError(ex, context));

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _dataContext?.Dispose();
        _dataContext = null;

        WeakReferenceMessenger.Default.UnregisterAll(this);
        GC.SuppressFinalize(this);
    }
}
