namespace AssetManager.Views.UserControls
{
    using Helpers;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Interactivity;

    public class DataGridDecimalColumn : DataGridTextColumn
    {
        protected override FrameworkElement GenerateEditingElement(DataGridCell cell, object dataItem)
        {
            var baseElement = (TextBox)base.GenerateEditingElement(cell, dataItem);

            var bDecimal = new TextBoxInputBehavior() { InputMode = TextBoxInputMode.DecimalInput };
            Interaction.GetBehaviors(baseElement).Add(bDecimal);

            return baseElement;
        }
    }
}
