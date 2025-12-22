# Snowflake

## Description 
Snowflake Database Explorer is a web application for exploring Snowflake databases with interactive navigation through databases, schemas, and tables, real-time statistics, table structure inspection, and paginated data preview.

## Live Demo

[![Live Demo](https://img.shields.io/badge/Live%20Demo-Snowflake-blue?style=for-the-badge)](https://ivy-projectdemos-snowflake.sliplane.app)

https://github.com/user-attachments/assets/eb3594a8-bcb6-40e4-b0f2-cc475147ad8b

## One-Click Development Environment

[![Open in GitHub Codespaces](https://github.com/codespaces/badge.svg)](https://github.com/codespaces/new?hide_repo_select=true&ref=main&repo=Ivy-Interactive%2FIvy-Examples&machine=standardLinux32gb&devcontainer_path=.devcontainer%2Fsnowflake%2Fdevcontainer.json&location=EuropeWest)

Launch a ready-to-code workspace with:
- **.NET 10.0** SDK pre-installed
- **Snowflake.Data** SDK and Ivy tooling available out of the box
- **Zero local setup** required

## Built With Ivy

This web application is powered by [Ivy Framework](https://github.com/Ivy-Interactive/Ivy-Framework).

**Ivy** unifies front-end and back-end development in C#, enabling rapid internal tool development with AI-assisted workflows, typed components, and reactive UI primitives.

## Interactive Snowflake Database Explorer

This demo showcases how to build an interactive database explorer using the official [Snowflake .NET Connector](https://github.com/snowflakedb/snowflake-connector-net) within an Ivy application.

### Features

- **Database Browser** – Navigate through databases, schemas, and tables with cascading dropdowns
- **Real-time Statistics** – View total counts of databases, schemas, and tables dynamically
- **Table Structure View** – Inspect column names, types, and nullable properties
- **Data Preview with Pagination** – Browse table data with paginated results (30 rows per page)
- **Skeleton Loading States** – Elegant loading placeholders during data fetching
- **Error Handling** – Clear error messages for connection and query issues
- **Responsive Layout** – Clean two-panel interface with explorer and preview sections

### Configuration

You can configure Snowflake credentials in two ways:

#### Option 1: Using User Secrets (Recommended for Development)

Set your credentials using dotnet user secrets:

```bash
dotnet user-secrets set "Snowflake:Account" "your-account-identifier"
dotnet user-secrets set "Snowflake:User" "your-username"
dotnet user-secrets set "Snowflake:Password" "your-password"
```

**Note:** Credentials from user secrets will be automatically loaded when the application starts.

#### Option 2: Using UI Input

Credentials can be entered through the application UI when you first run it. Click the "Enter Credentials" button in the Settings tab.

## How to Run Locally

1. **Prerequisites:** .NET 10.0 SDK and Snowflake account credentials
2. **Navigate to the project:**
   ```bash
   cd project-demos/snowflake
   ```
3. **Restore dependencies:**
   ```bash
   dotnet restore
   ```
4. **Configure credentials (optional):**
   
   Using user secrets (recommended):
   ```bash
   dotnet user-secrets set "Snowflake:Account" "your-account-identifier"
   dotnet user-secrets set "Snowflake:User" "your-username"
   dotnet user-secrets set "Snowflake:Password" "your-password"
   ```
   
   Or enter credentials through the UI after starting the app.
5. **Start the app:**
   ```bash
   dotnet watch
   ```
6. **Open your browser** to the URL shown in the terminal (typically `http://localhost:5010`)
7. **If using UI input**, enter your Snowflake credentials in the Settings tab when prompted

## Deploy to Ivy Hosting

1. **Navigate to the project:**
   ```bash
   cd project-demos/snowflake
   ```
2. **Deploy:**
   ```bash
   ivy deploy
   ```

## Learn More

- Snowflake Documentation: [docs.snowflake.com](https://docs.snowflake.com/)
- Snowflake .NET Connector: [github.com/snowflakedb/snowflake-connector-net](https://github.com/snowflakedb/snowflake-connector-net)
- Ivy documentation: [docs.ivy.app](https://docs.ivy.app)


## Tags 
Snowflake, Database Explorer, Data Visualization, SQL, Data Warehouse
