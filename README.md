# EmmyHandmade Asset Manager

## Overview

The EmmyHandmade Asset Manager is a WPF-based desktop application originally created in 2015 to support my wife's small business. It provides comprehensive tracking and management of expenses, inventory, and orders with robust financial reporting capabilities.

## Features

### Expense Tracking

- **Quarterly Analysis**: View and analyze expenses broken down by quarter for easy seasonal trend identification
- **Custom Period Reporting**: Generate expense reports for any custom time period, providing flexibility for specific business analysis needs
- **Miscellaneous Expenses**: Track additional expenses beyond standard inventory and order costs

### Inventory Management

- **Item Tracking**: Maintain detailed inventory records including costs and quantities
- **Batch Processing**: Organize products into batches with custom attributes and product yields
- **Unit Conversions**: 
  - Direct conversions between compatible units where applicable
  - Approximation-based conversions between volume and weight measurements for flexible unit handling

### Order & Batch Management

- **Order Processing**: Create and manage customer orders with individual line items
- **Batch Management**: Track product batches with loss items, product yields, and oil percentages
- **Out Records**: Maintain records of inventory disbursements and product shipments

## Technical Stack

- **Framework**: .NET Framework with WPF (Windows Presentation Foundation)
- **Database**: SQL Server (LocalDB with Entity Framework)
- **Architecture**: MVVM (Model-View-ViewModel) with MVVM Light
- **Additional Libraries**: 
  - Entity Framework 6.1.3 for ORM
  - WPF Animated GIF for enhanced UI
  - CommonServiceLocator for dependency injection

## Project Structure

```
EmmyHandmade/
├── Data/                    # Data models and database context
├── ViewModels/              # MVVM ViewModel implementations
├── Views/                   # XAML UI views
├── Helpers/                 # Utility classes and converters
├── Migrations/              # Entity Framework database migrations
├── Assets/                  # Application resources and media
└── Properties/              # Project properties and settings
```

## Building and Running

1. Open `AssetManager.sln` in Visual Studio
2. Restore NuGet packages
3. Build the solution
4. Run the application from the Debug folder

## Database

The application uses a local SQL Server database. Entity Framework Code First migrations are included to set up the schema automatically on first run.

## Icons

This application uses the Pretty Office icon set from [Custom Icon Design](https://www.customicondesign.com/). These icons are provided free for personal use only. If this project is expanded for commercial purposes, you will need to either replace the icons with alternatives or purchase a commercial license for the Pretty Office icon package.

## License

See the [LICENSE](LICENSE) file for details.
