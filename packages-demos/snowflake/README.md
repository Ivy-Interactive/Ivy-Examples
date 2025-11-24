# Snowflake

## Description 
Snowflake Database Explorer is a web application for exploring Snowflake databases with interactive navigation through databases, schemas, and tables, real-time statistics, table structure inspection, and paginated data preview.

<img width="1919" height="911" alt="image" src="https://github.com/user-attachments/assets/5588aba2-6b33-46fd-9c9d-4e9f3f9d14d8" />
<img width="1156" height="914" alt="image" src="https://github.com/user-attachments/assets/14ffbb83-ae12-45a3-acfa-d74c910ceda3" />

## One-Click Development Environment

[![Open in GitHub Codespaces](https://github.com/codespaces/badge.svg)](https://github.com/codespaces/new?hide_repo_select=true&ref=main&repo=Ivy-Interactive%2FIvy-Examples&machine=standardLinux32gb&devcontainer_path=.devcontainer%2Fsnowflake%2Fdevcontainer.json&location=EuropeWest)

Launch a ready-to-code workspace with:
- **.NET 9.0** SDK pre-installed
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

The app reads settings from `appsettings.json` (overridable via environment variables):
- `Snowflake:Account` – Your Snowflake account identifier
- `Snowflake:User` – Your Snowflake username
- `Snowflake:Password` – Your Snowflake password
- `Snowflake:Warehouse` – Snowflake warehouse name (default: `COMPUTE_WH`)
- `Snowflake:Database` – Database name (default: `SNOWFLAKE_SAMPLE_DATA`)
- `Snowflake:Schema` – Schema name (default: `TPCH_SF1`)

## Setting Up Credentials

Before running the application, you need to configure your Snowflake credentials. There are two ways to set them up:

### Step 1: Get Your Snowflake Credentials

1. **Sign up or log in** to [Snowflake](https://www.snowflake.com/)
2. Navigate to your **Account** settings
3. Copy your **Account Identifier** (e.g., `xy12345.us-east-1`)
4. Note your **Username** and **Password**

> **Important:** Never publish credentials in public repositories or share them with unauthorized parties.

### Step 2: Configure the Credentials

For better security, especially in production, use environment variables instead of storing credentials in a file.

**Windows PowerShell:**
```powershell
$env:Snowflake__Account = "your_account_identifier"
$env:Snowflake__User = "your_username"
$env:Snowflake__Password = "your_password"
```

**Windows Command Prompt:**
```cmd
set Snowflake__Account=your_account_identifier
set Snowflake__User=your_username
set Snowflake__Password=your_password
```

**Linux/macOS:**
```bash
export Snowflake__Account="your_account_identifier"
export Snowflake__User="your_username"
export Snowflake__Password="your_password"
```

> **Note:** The double underscore `__` in the environment variable corresponds to the colon `:` in configuration (i.e., `Snowflake__Account` → `Snowflake:Account`)

After setting the environment variables, run the app in the same console:
```bash
dotnet watch
```

## How to Run Locally

1. **Prerequisites:** .NET 9.0 SDK and Snowflake account credentials
2. **Navigate to the project:**
   ```bash
   cd packages-demos/snowflake
   ```
3. **Restore dependencies:**
   ```bash
   dotnet restore
   ```
4. **Start the app:**
   ```bash
   dotnet watch
   ```
5. **Open your browser** to the URL shown in the terminal (typically `http://localhost:5010`)

## Deploy to Ivy Hosting

1. **Install Ivy CLI** if not already installed:
   ```bash
   cd packages-demos/snowflake
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
