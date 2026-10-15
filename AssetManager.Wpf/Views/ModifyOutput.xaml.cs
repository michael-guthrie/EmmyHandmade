using System.Windows;
using AssetManager.ViewModels;

namespace AssetManager.Views;

public partial class ModifyOutput : Window
{
    private readonly OutputViewModel _viewModel;

    public ModifyOutput(OutputViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();

        viewModel.SaveCompleted += (_, _) => Close();
    }

    public void LoadForNew()
    {
        _viewModel.OutRecordId = null;
        _viewModel.InitializeBackground();
    }

    public void LoadForEdit(int outRecordId)
    {
        _viewModel.OutRecordId = outRecordId;
        _viewModel.InitializeBackground();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}
