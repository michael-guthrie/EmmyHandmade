namespace AssetManager.Infrastructure;

/// <summary>
/// Replaces the MvvmLight messenger tokens (<c>Helpers.MessengerToken</c>).
///
/// MvvmLight sent loosely-typed payloads keyed by an enum token, so a mistyped token meant a
/// message that silently went nowhere. CommunityToolkit's <c>WeakReferenceMessenger</c> dispatches
/// on the message type, so each of the old tokens becomes a type here and the compiler checks the
/// pairing. Registrations are weak, so a closed window stops listening without explicit cleanup.
/// </summary>
public static class Messages
{
    /// <summary>Inventory items were added, renamed or re-configured.</summary>
    public sealed record ItemsChanged(IReadOnlyList<int> ItemIds);

    /// <summary>Orders were saved.</summary>
    public sealed record OrdersChanged(IReadOnlyList<int> OrderIds);

    /// <summary>Batches were saved.</summary>
    public sealed record BatchesChanged(IReadOnlyList<int> BatchIds);

    /// <summary>Products were added or renamed.</summary>
    public sealed record ProductsChanged(IReadOnlyList<int> ProductIds);

    /// <summary>
    /// Something changed that affects expense or sales totals on the given dates. The quarterly
    /// panel reloads only when one of the dates falls in the quarter it is showing.
    /// </summary>
    public sealed record ExpensesChanged(IReadOnlyList<DateTime> Dates);

    /// <summary>An unhandled error that should be shown to the user.</summary>
    public sealed record AppError(Exception Exception, string? Context = null);
}
