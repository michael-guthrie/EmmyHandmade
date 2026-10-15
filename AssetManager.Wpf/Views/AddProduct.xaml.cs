using System.Windows;
using AssetManager.Core.Data;
using AssetManager.Core.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AssetManager.Views;

/// <summary>
/// The small "New product" dialog behind the New button on the batch window.
/// </summary>
public partial class AddProduct : Window
{
    private readonly IDbContextFactory<TrackingStore> _dbFactory;

    /// <summary>Id of the product created, once the dialog closes with OK.</summary>
    public int CreatedProductId { get; private set; }

    public AddProduct(IDbContextFactory<TrackingStore> dbFactory)
    {
        _dbFactory = dbFactory;
        InitializeComponent();
        Loaded += (_, _) => ProductName.Focus();
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var name = ProductName.Text.Trim();
        if (string.IsNullOrEmpty(name))
        {
            ErrorBlock.Text = "Enter a name.";
            return;
        }

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            if (await db.BatchProducts.AnyAsync(p => p.Name == name))
            {
                ErrorBlock.Text = "A product with that name already exists.";
                return;
            }

            var product = new BatchProduct { Name = name };
            db.BatchProducts.Add(product);
            await db.SaveChangesAsync();

            CreatedProductId = product.Id;
            DialogResult = true;
        }
        catch (Exception ex)
        {
            ErrorBlock.Text = ex.Message;
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
