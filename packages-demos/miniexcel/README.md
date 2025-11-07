# MiniExcel

<img width="1917" height="911" alt="image" src="https://github.com/user-attachments/assets/a223a520-bb84-4ed0-933c-cc4d118c6bbd" />

<img width="1916" height="911" alt="image" src="https://github.com/user-attachments/assets/2f4c60cf-203b-4f7b-8368-d49e63ca57f5" />

## One-Click Development Environment

[![Open in GitHub Codespaces](https://github.com/codespaces/badge.svg)](https://github.com/codespaces/new?hide_repo_select=true&ref=main&repo=Ivy-Interactive%2FIvy-Examples&machine=standardLinux32gb&devcontainer_path=.devcontainer%2Fminiexcel%2Fdevcontainer.json&location=EuropeWest)

Click the badge above to open Ivy Examples repository in GitHub Codespaces with:
- **.NET 9.0** SDK pre-installed
- **Ready-to-run** development environment
- **No local setup** required

## Created Using Ivy

Web application created using [Ivy-Framework](https://github.com/Ivy-Interactive/Ivy-Framework).

**Ivy** - The ultimate framework for building internal tools with LLM code generation by unifying front-end and back-end into a single C# codebase. With Ivy, you can build robust internal tools and dashboards using C# and AI assistance based on your existing database.

Ivy is a web framework for building interactive web applications using C# and .NET.

## Interactive Example For Excel File Operations

This example demonstrates Excel file operations using the [MiniExcel library](https://github.com/mini-software/MiniExcel) integrated with Ivy. MiniExcel is a fast, efficient, and zero-dependency library for reading and writing Excel files (xlsx, csv).

**What This Application Does:**

This specific implementation creates a **Student Management** application with two integrated views that allow users to:

- **Create Student Records**: Add new students with form validation (name, email, age, course, grade)
- **Edit Students**: Update existing student information through a sheet-based editing interface
- **Delete Students**: Remove students with confirmation dialogs
- **Search & Filter**: Real-time search across student fields (name, email, course, grade, age)
- **Export to Excel**: Download student data as properly formatted Excel (.xlsx) files
- **Import from Excel**: Upload Excel files to bulk import students with automatic merge/update logic
- **Real-Time Synchronization**: Automatic data synchronization between edit and view applications
- **Data Table View**: Interactive data table with sortable columns and filtering capabilities
- **Blade Navigation**: Modern blade-based UI for seamless navigation between list and detail views

**Technical Implementation:**

- Uses MiniExcel's `SaveAs` and `Query` methods for robust Excel processing
- Implements cross-app data synchronization using `DataChanged` events and `RefreshToken` mechanism
- Creates downloadable Excel files with automatic timestamp naming
- Handles file upload with ID-based merge logic (updates existing records, adds new ones)
- Implements form validation with required fields and data type constraints
- Uses `UseEffect` hooks with `RefreshToken` for reactive state management
- Supports blade navigation for detail views and editing
- Implements real-time search with case-insensitive filtering

## How to Run

1. **Prerequisites**: .NET 9.0 SDK
2. **Navigate to the example**:
   ```bash
   cd miniexcel
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

The application provides two separate apps:
- **MiniExcel - Edit**: Full CRUD interface with search, create, edit, and delete functionality
- **MiniExcel - View**: Data table view with Excel export/import capabilities

## How to Deploy

Deploy this example to Ivy's hosting platform:

1. **Navigate to the example**:
   ```bash
   cd miniexcel
   ```
2. **Deploy to Ivy hosting**:
   ```bash
   ivy deploy
   ```
This will deploy your Excel management application with a single command.

## Learn More

- MiniExcel for .NET overview: [github.com/mini-software/MiniExcel](https://github.com/mini-software/MiniExcel)
- Ivy Documentation: [docs.ivy.app](https://docs.ivy.app)
