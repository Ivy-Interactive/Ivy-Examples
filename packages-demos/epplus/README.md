# EPPlus

<img width="1915" height="911" alt="image" src="https://github.com/user-attachments/assets/1f05a450-1d2b-4379-9875-01fc0204307c" />

## One-Click Development Environment

[![Open in GitHub Codespaces](https://github.com/codespaces/badge.svg)](https://github.com/codespaces/new?hide_repo_select=true&ref=main&repo=Ivy-Interactive%2FIvy-Examples&machine=standardLinux32gb&devcontainer_path=.devcontainer%2Fepplus%2Fdevcontainer.json&location=EuropeWest)

Click the badge above to open Ivy Examples repository in GitHub Codespaces with:
- **.NET 9.0** SDK pre-installed
- **Ready-to-run** development environment
- **No local setup** required

## Created Using Ivy

Web application created using [Ivy-Framework](https://github.com/Ivy-Interactive/Ivy-Framework).

**Ivy** - The ultimate framework for building internal tools with LLM code generation by unifying front-end and back-end into a single C# codebase. With Ivy, you can build robust internal tools and dashboards using C# and AI assistance based on your existing database.

Ivy is a web framework for building interactive web applications using C# and .NET.

## Interactive Example For Excel (XLSX) Operations

This example demonstrates Excel file operations using the [EPPlus library](https://github.com/EPPlusSoftware/EPPlus) integrated with Ivy. EPPlus is a powerful .NET library for creating and reading `.xlsx` files without requiring Microsoft Office.

**What This Application Does:**

This specific implementation creates a **Books Manager** that allows users to:

- **Generate Excel**: Create a `books.xlsx` with headers and sample or current records
- **Add Books**: Submit a form to append rows directly into the Excel worksheet
- **View Data**: Display the parsed rows in a table (kept in sync with the file)
- **Download .xlsx**: Download the current Excel file
- **Delete All Records**: Clear all non-header rows from the worksheet

**Technical Implementation:**

- Uses EPPlus `ExcelPackage`, worksheets, cell writes, and `AutoFitColumns`
- Reads/writes a file in the temp folder: `%TEMP%/books.xlsx`
- Maps rows to strongly typed records via `[Column]` attributes and a helper (`ConvertSheetToObjects<T>`) in `Helpers/EPPLusExtensions.cs`
- UI built with Ivy components in a split-card layout (actions on the left, table on the right)
- Buttons enable/disable based on whether the worksheet has records

## How to Run

1. **Prerequisites**: .NET 9.0 SDK
2. **Navigate to the example**:
   ```bash
   cd epplus
   ```
3. **Restore dependencies**:
   ```bash
   dotnet restore
   ```
4. **Run the application**:
   ```bash
   dotnet watch
   ```
5. **Open your browser** to the URL shown in the terminal (typically `http://localhost:5010`)

## How to Deploy

Deploy this example to Ivy's hosting platform:

1. **Navigate to the example**:
   ```bash
   cd epplus
   ```
2. **Deploy to Ivy hosting**:
   ```bash
   ivy deploy
   ```
This will deploy your Excel management application with a single command.

## EPPlus Licensing Note

EPPlus 8 uses a dual license model (free for noncommercial use under Polyform Noncommercial 1.0.0; commercial license required for business use). You must set the license context before use. See setup details and licensing guidance in the official repository: [github.com/EPPlusSoftware/EPPlus](https://github.com/EPPlusSoftware/EPPlus).

In this example, noncommercial mode is configured in `Program.cs`:

```csharp
ExcelPackage.License.SetNonCommercialOrganization("Ivy");
```

## Learn More

- EPPlus GitHub repository: [github.com/EPPlusSoftware/EPPlus](https://github.com/EPPlusSoftware/EPPlus)
- Ivy Documentation: [docs.ivy.app](https://docs.ivy.app)
