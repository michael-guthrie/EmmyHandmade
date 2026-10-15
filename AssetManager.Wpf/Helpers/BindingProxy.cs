using System.Windows;

namespace AssetManager.Helpers;

/// <summary>
/// Carries a DataContext into places that are outside the visual tree.
///
/// <para><c>DataGridComboBoxColumn</c> and the other DataGrid column types are not visual
/// children of the grid, so a <c>RelativeSource AncestorType=Window</c> binding on their
/// ItemsSource silently resolves to nothing and the combo box comes up empty. The old app worked
/// around this by making each lookup list an application-level <c>StaticResource</c>; now that
/// the lookups live on the view model, a <see cref="Freezable"/> is the usual way across.</para>
///
/// <para>A Freezable inherits the DataContext of the element whose resources hold it, so a
/// <c>Data="{Binding}"</c> on the proxy resolves, and columns bind through
/// <c>{Binding Data.Something, Source={StaticResource Proxy}}</c>.</para>
/// </summary>
public sealed class BindingProxy : Freezable
{
    protected override Freezable CreateInstanceCore() => new BindingProxy();

    public object? Data
    {
        get => GetValue(DataProperty);
        set => SetValue(DataProperty, value);
    }

    public static readonly DependencyProperty DataProperty =
        DependencyProperty.Register(nameof(Data), typeof(object), typeof(BindingProxy),
            new UIPropertyMetadata(null));
}
