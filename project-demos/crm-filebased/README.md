# CRM File-Based

## Description 
A simple CRM (Customer Relationship Management) application built with Ivy Framework using SQLite file-based database. This demo showcases how to build a full-featured CRUD application with data visualization, blade navigation, and reactive UI updates.

## One-Click Development Environment

[![Open in GitHub Codespaces](https://github.com/codespaces/badge.svg)](https://github.com/codespaces/new?hide_repo_select=true&ref=main&repo=Ivy-Interactive%2FIvy-Examples&machine=standardLinux32gb&devcontainer_path=.devcontainer%2Fcrm-filebased%2Fdevcontainer.json&location=EuropeWest)

Launch a ready-to-code workspace with:
- **.NET 10.0** SDK pre-installed
- **Microsoft.Data.Sqlite** and Ivy tooling available out of the box
- **Zero local setup** required

## Built With Ivy

This web application is powered by [Ivy Framework](https://github.com/Ivy-Interactive/Ivy-Framework).

**Ivy** unifies front-end and back-end development in C#, enabling rapid internal tool development with AI-assisted workflows, typed components, and reactive UI primitives.

## CRM File-Based Application

This demo showcases how to build a complete CRM application using SQLite file-based database within an Ivy application. Perfect for learning Ivy framework patterns with file-based data persistence.

### Features

- **Dashboard** – Overview statistics with interactive data visualization
  - Statistics cards showing totals for Tasks, Notes, and Contacts
  - Task Status Pie Chart displaying distribution of completed vs pending tasks
  - Activity Over Time Bar Chart showing items created per day
  
- **Tasks Management** – Complete task tracking system
  - Create, edit, and delete tasks
  - Mark tasks as completed or pending
  - Search and filter tasks
  - Blade-based navigation for detailed task views
  
- **Notes Management** – Simple note-taking functionality
  - Create and manage notes with title and content
  - Full text search capabilities
  - Edit and delete operations
  
- **Contacts Management** – Contact information storage
  - Store contact details (name, email, phone)
  - Search contacts by name, email, or phone
  - Full CRUD operations

### Technical Highlights

- **File-Based Storage** – SQLite database stored in `db.sqlite` file
- **Reactive UI Updates** – Uses `UseRefreshToken()` for automatic data synchronization
- **Blade Navigation** – Hierarchical navigation pattern with detail views
- **Data Visualization** – Interactive charts using Ivy's chart components
- **Form Validation** – Client-side validation with data annotations
- **Async Data Operations** – Proper async/await patterns throughout

## How to Run Locally

1. **Prerequisites:** .NET 10.0 SDK
2. **Navigate to the project:**
   ```bash
   cd project-demos/crm-filebased
   ```
3. **Start the app:**
   ```bash
   dotnet run CrmApp.cs
   ```
4. **Open your browser** to the URL shown in the terminal (typically `http://localhost:5010`)

The application will automatically create a `db.sqlite` database file in the project directory on first run.

## Database Structure

The application uses SQLite with three main tables:

- **Tasks** – `Id`, `Title`, `IsCompleted`, `CreatedAt`
- **Notes** – `Id`, `Title`, `Content`, `CreatedAt`
- **Contacts** – `Id`, `Name`, `Email`, `Phone`, `CreatedAt`

Data is persisted locally in the `db.sqlite` file, which is automatically created if it doesn't exist.
## Learn More

- SQLite Documentation: [sqlite.org](https://www.sqlite.org/)
- Microsoft.Data.Sqlite: [docs.microsoft.com](https://docs.microsoft.com/en-us/dotnet/standard/data/sqlite/)
- Ivy documentation: [docs.ivy.app](https://docs.ivy.app)

## Tags 
CRM, SQLite, File-Based Database, Data Visualization, CRUD Application, Task Management, Contact Management

