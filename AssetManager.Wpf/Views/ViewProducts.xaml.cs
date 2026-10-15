using System.Windows;
using AssetManager.ViewModels;

namespace AssetManager.Views;

public partial class ViewProducts : Window
{
    public ViewProducts(ViewProductViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
        viewModel.InitializeBackground();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}
