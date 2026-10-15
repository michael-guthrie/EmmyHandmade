using System.Collections.Generic;
using AssetManager.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;

namespace AssetManager.Infrastructure;

/// <summary>
/// Replaces the <c>ItemLookup</c>, <c>ProductLookup</c> and <c>UnitLookup</c> XAML resources.
///
/// Those were three near-identical classes, each instantiated as an application resource and each
/// opening its own DbContext on a background thread in its constructor. This is one shared,
/// DI-registered object that reloads when something broadcasts a change, so every combo box in
/// every open window refreshes together — which is what the MvvmLight messenger registrations in
/// the originals were for.
///
/// The <c>KeyValuePair</c> shape is kept because the XAML combo boxes bind
/// <c>SelectedValuePath="Key"</c> / <c>DisplayMemberPath="Value"</c> against it.
/// </summary>
public sealed partial class LookupCache : ObservableObject
{
    private readonly LookupService _lookups;

    [ObservableProperty]
    private IReadOnlyList<KeyValuePair<int, string>> _items = [];

    [ObservableProperty]
    private IReadOnlyList<KeyValuePair<int, string>> _products = [];

    [ObservableProperty]
    private IReadOnlyList<KeyValuePair<int, string>> _units = [];

    /// <summary>Units with a leading blank entry, for the optional alternate-unit pickers.</summary>
    [ObservableProperty]
    private IReadOnlyList<KeyValuePair<int?, string>> _nullableUnits = [];

    private Dictionary<int, ItemLookup> _itemsById = new();

    public LookupCache(LookupService lookups)
    {
        _lookups = lookups;

        // Weak registrations: no explicit unsubscribe needed anywhere.
        WeakReferenceMessenger.Default.Register<Messages.ItemsChanged>(this, (_, _) => _ = ReloadAsync());
        WeakReferenceMessenger.Default.Register<Messages.ProductsChanged>(this, (_, _) => _ = ReloadAsync());
    }

    /// <summary>Reloads every lookup. Also used for the initial load.</summary>
    public async Task ReloadAsync(CancellationToken ct = default)
    {
        var all = await _lookups.GetAllAsync(ct);

        _itemsById = all.Items.ToDictionary(i => i.Id);

        Items = all.Items.Select(i => new KeyValuePair<int, string>(i.Id, i.Name)).ToList();
        Products = all.Products.Select(p => new KeyValuePair<int, string>(p.Id, p.Name)).ToList();
        Units = all.Units.Select(u => new KeyValuePair<int, string>(u.Id, u.Name)).ToList();

        NullableUnits = new List<KeyValuePair<int?, string>> { new(null, string.Empty) }
            .Concat(all.Units.Select(u => new KeyValuePair<int?, string>(u.Id, u.Name)))
            .ToList();

        // Not an [ObservableProperty], so the grids' "Default" columns are told explicitly.
        OnPropertyChanged(nameof(ItemUnitNames));
    }

    /// <summary>Tracking-unit name per item id, for the read-only "Default" grid columns.</summary>
    public IReadOnlyDictionary<int, string> ItemUnitNames =>
        _itemsById.ToDictionary(kv => kv.Key, kv => kv.Value.UnitName);

    public string ItemUnitName(int itemId) =>
        _itemsById.TryGetValue(itemId, out var item) ? item.UnitName : "N/A";

    /// <summary>
    /// The tracking unit of an item. Used to default a line's units when an item is picked, which
    /// is what the wrapper classes in the old view models did inline.
    /// </summary>
    public int? DefaultUnitIdForItem(int itemId) =>
        _itemsById.TryGetValue(itemId, out var item) ? item.UnitOfMeasureId : null;
}
