using System.Windows;
using System.Windows.Controls;
using AssetManager.Helpers;
using Microsoft.Xaml.Behaviors;

namespace AssetManager.Views.UserControls;

/// <summary>
/// A DataGrid text column whose editor only accepts decimal input. Unchanged except for the
/// behaviors namespace, which moved from System.Windows.Interactivity to Microsoft.Xaml.Behaviors.
/// </summary>
public class DataGridDecimalColumn : DataGridTextColumn
{
    protected override FrameworkElement GenerateEditingElement(DataGridCell cell, object dataItem)
    {
        var baseElement = (TextBox)base.GenerateEditingElement(cell, dataItem);

        Interaction.GetBehaviors(baseElement)
            .Add(new TextBoxInputBehavior { InputMode = TextBoxInputMode.DecimalInput });

        return baseElement;
    }
}
