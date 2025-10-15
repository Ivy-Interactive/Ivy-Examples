# CSV Helper

<img width="1913" height="904" alt="image" src="https://github.com/user-attachments/assets/2b471250-20a2-4318-8f64-30a3a87dd151" />

## One-Click Development Environment

[![Open in GitHub Codespaces](https://github.com/codespaces/badge.svg)](https://github.com/codespaces/new?hide_repo_select=true&ref=main&repo=Ivy-Interactive%2FIvy-Examples&machine=standardLinux32gb&devcontainer_path=.devcontainer%2Fcsvhelper%2Fdevcontainer.json&location=EuropeWest)

Click the badge above to open Ivy Examples repository in GitHub Codespaces with:
- **.NET 9.0** SDK pre-installed
- **Ready-to-run** development environment
- **No local setup** required

## Created Using Ivy

Web application created using [Ivy-Framework](https://github.com/Ivy-Interactive/Ivy-Framework).

**Ivy** - The ultimate framework for building internal tools with LLM code generation by unifying front-end and back-end into a single C# codebase. With Ivy, you can build robust internal tools and dashboards using C# and AI assistance based on your existing database.

Ivy is a web framework for building interactive web applications using C# and .NET.

## Interactive Example For CSV File Operations

This example demonstrates CSV file operations using the [CsvHelper library](https://github.com/JoshClose/CsvHelper) integrated with Ivy. CsvHelper is an extremely fast, flexible, and easy-to-use library for reading and writing CSV files.

**What This Application Does:**

This specific implementation creates a **Product Management** application that allows users to:

- **Create Product Records**: Fill out a form with product information (name, description, price, category)
- **Export to CSV**: Download product data as properly formatted CSV files
- **Import from CSV**: Upload CSV files to bulk import products with automatic validation
- **Manage Products**: View, add, and delete products in a real-time data table
- **Form Validation**: Validates required fields and data types for product creation
- **Interactive UI**: Split-panel layout with controls on the left and data table on the right

**Technical Implementation:**

- Uses CsvHelper's `CsvWriter` and `CsvReader` for robust CSV processing
- Generates downloadable CSV files with proper headers and formatting
- Implements file upload handling with automatic ID generation and timestamp assignment
- Creates responsive table layout with delete functionality
- Handles form submission with toast notifications and state management
- Supports custom class object serialization/deserialization

## How to Run

1. **Prerequisites**: .NET 9.0 SDK
2. **Navigate to the example**:
   ```bash
   cd csvhelper
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
   cd csvhelper
   ```
2. **Deploy to Ivy hosting**:
   ```bash
   ivy deploy
   ```
This will deploy your CSV management application with a single command.

## Learn More

- CsvHelper for .NET overview: [github.com/JoshClose/CsvHelper](https://github.com/JoshClose/CsvHelper)
- Ivy Documentation: [docs.ivy.app](https://docs.ivy.app)