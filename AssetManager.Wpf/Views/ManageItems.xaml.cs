using System.Windows;
using AssetManager.ViewModels;

namespace AssetManager.Views;

public partial class ManageItems : Window
{
    public ManageItems(ManageItemsViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();

        viewModel.ValidationFailed += (_, errors) =>
            MessageBox.Show(this, errors, "Please fix these first",
                MessageBoxButton.OK, MessageBoxImage.Warning);

        viewModel.InitializeBackground();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}
