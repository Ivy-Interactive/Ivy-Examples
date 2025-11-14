# Taskly

A backlog/sprint planning board application built with [Ivy Framework](https://github.com/Ivy-Interactive/Ivy).

Ivy is a web framework for building interactive web applications using C# and .NET.

## Features

- **Product Backlog**: Manage Epics, Stories, Tasks, and Bugs in a hierarchical structure
- **Sprint Planning**: Create sprints and add stories with their tasks
- **Sprint Board**: Kanban-style board to track task progress (To Do → In Progress → Done)
- **Sprint Archive**: View and restore previous sprints

## Getting Started

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- SQLite (included in .NET)

### First Time Setup

1. Clone the repository:
   ```bash
   git clone <your-repo-url>
   cd Taskly
   ```

2. The database will be **automatically created** from the template on first run. No manual setup needed!

### Run the Application

```bash
dotnet watch
```

The database (`Database/taskly_database.db`) will be created automatically if it doesn't exist.

## Database

The application uses SQLite for local data storage.

- **Template**: `Database/taskly_database.template.db` (committed to Git)
- **Your data**: `Database/taskly_database.db` (ignored by Git - personal data)

### How it works:

1. The template database is committed to Git (contains schema only, no data)
2. Your personal database is automatically created from the template on first run
3. Your personal data is never committed to Git (protected by `.gitignore`)
4. Each developer has their own local database

### Manual Database Operations

**Reset database** (delete all data):
```bash
dotnet run -- --clean-database
```
Or use the "Clean Database" button in the Planning app.

**Recreate from template**:
```bash
rm Database/taskly_database.db
dotnet run
```

## Deploy

```bash
ivy deploy
```