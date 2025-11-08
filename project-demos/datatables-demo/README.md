# DataTables Demo

This Ivy application demonstrates the powerful DataTable widget capabilities, showcasing different features and use cases.

## Features Demonstrated

- **Basic DataTable**: Simple table with automatic column detection
- **Sortable Columns**: Interactive sorting by clicking column headers
- **Filterable Data**: Real-time filtering across all columns
- **Large Dataset Performance**: High-performance handling of 10,000+ rows using Apache Arrow
- **Custom Columns**: Custom formatting and column types
- **Interactive Demo**: Live data manipulation with add/remove functionality

## Running the Application

```bash
dotnet run
```

The application will start on port 5010 and open in your default browser.

## DataTable Features

The DataTable widget provides:

- **High Performance**: Built on Apache Arrow for optimal performance with large datasets
- **Automatic Column Detection**: Automatically detects column types (text, number, boolean, date, icon)
- **Sorting**: Click column headers to sort data
- **Filtering**: Real-time search across all columns
- **Pagination**: Automatic pagination for large datasets
- **Custom Formatting**: Support for custom column formatting

## Sample Data

The demo uses generated user data with:

- Full names
- Email addresses
- Salary information
- Status icons
- Active/inactive flags

## Built With

- [Ivy Framework](https://github.com/Ivy-Interactive/Ivy-Framework)
- .NET 9.0
- Apache Arrow (for high-performance data processing)
