# ExcelDataReader

<img width="1917" height="913" alt="image" src="https://github.com/user-attachments/assets/f1cd1de6-515a-41a6-832b-4b99231d5776" />

## One-Click Development Environment

[![Open in GitHub Codespaces](https://github.com/codespaces/badge.svg)](https://github.com/codespaces/new?hide_repo_select=true&ref=main&repo=Ivy-Interactive%2FIvy-Examples&machine=standardLinux32gb&devcontainer_path=.devcontainer%2Fexceldatareader%2Fdevcontainer.json&location=EuropeWest)

Click the badge above to open Ivy Examples repository in GitHub Codespaces with:
- **.NET 9.0** SDK pre-installed
- **Ready-to-run** development environment
- **No local setup** required

## Created Using Ivy

Web application created using [Ivy-Framework](https://github.com/Ivy-Interactive/Ivy-Framework).

**Ivy** - The ultimate framework for building internal tools with LLM code generation by unifying front-end and back-end into a single C# codebase. With Ivy, you can build robust internal tools and dashboards using C# and AI assistance based on your existing database.

Ivy is a web framework for building interactive web applications using C# and .NET.

## Interactive Example For Excel File Analysis

This example demonstrates Excel and CSV file analysis using the [ExcelDataReader library](https://github.com/ExcelDataReader/ExcelDataReader) integrated with Ivy. ExcelDataReader is a lightweight and fast library for reading Microsoft Excel files (2.0-2021, 365).

**What This Application Does:**

This specific implementation creates an **Excel File Analyzer** that allows users to:

- **Upload Excel Files**: Support for .xlsx, .xls, and .csv file formats
- **Analyze File Structure**: Extract detailed information about sheets, columns, rows, and headers
- **View Merged Cells**: Identify and count merged cell ranges in Excel files
- **Interactive Analysis**: Two-panel layout with file upload controls and detailed results
- **Expandable Sheets**: Each sheet's information is displayed in collapsible cards
- **Real-time Statistics**: File size, total rows, columns, and merged cells count
- **Markdown Tables**: Beautiful formatted results with structured data presentation

**Technical Implementation:**

- Uses ExcelDataReader's `ExcelReaderFactory.CreateReader()` and `CreateCsvReader()` for robust file processing
- Implements automatic file type detection based on file signatures (ZIP, OLE, CSV patterns)
- Handles null reference exceptions with comprehensive error checking
- Creates responsive two-card layout with file input and analysis results
- Supports both Excel (.xlsx, .xls) and CSV file formats
- Generates detailed Markdown tables for structured data presentation
- Implements expandable components for sheet information display

## How to Run

1. **Prerequisites**: .NET 9.0 SDK
2. **Navigate to the example**:
   ```bash
   cd exceldatareader
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
   cd exceldatareader
   ```
2. **Deploy to Ivy hosting**:
   ```bash
   ivy deploy
   ```
This will deploy your Excel analysis application with a single command.

## Learn More

- ExcelDataReader for .NET overview: [github.com/ExcelDataReader/ExcelDataReader](https://github.com/ExcelDataReader/ExcelDataReader)
- Ivy Documentation: [docs.ivy.app](https://docs.ivy.app)