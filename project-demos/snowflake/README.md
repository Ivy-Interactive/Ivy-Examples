# Snowflake

## Description 
Snowflake Database Explorer is a web application for exploring Snowflake databases with interactive navigation through databases, schemas, and tables, real-time statistics, table structure inspection, and paginated data preview.

## Live Demo

[![Live Demo](https://img.shields.io/badge/Live%20Demo-Snowflake-blue?style=for-the-badge)](https://ivy-projectdemos-snowflake.sliplane.app)

https://github.com/user-attachments/assets/bc069ffb-d36f-4eb8-a4da-f279db05d959


https://github.com/user-attachments/assets/7fec362b-3fd5-4050-aaf5-4147c229b6eb

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

See the "Setting Up Credentials" section below for instructions on configuring these values using `dotnet user secrets`.

## Setting Up Credentials

Before running the application, you need to configure your Snowflake credentials using `dotnet user secrets`.

### Step 1: Get Your Snowflake Credentials

1. **Sign up or log in** to [Snowflake](https://www.snowflake.com/)
2. Navigate to your **Account** settings
3. Copy your **Account Identifier** (e.g., `xy12345.us-east-1`)
4. Note your **Username** and **Password**

> **Important:** Never publish credentials in public repositories or share them with unauthorized parties.

### Step 2: Configure the Credentials

Use `dotnet user secrets` to securely store your Snowflake credentials:

```bash
cd project-demos/snowflake
dotnet user secrets set "Snowflake:Account" "your_account_identifier"
dotnet user secrets set "Snowflake:User" "your_username"
dotnet user secrets set "Snowflake:Password" "your_password"
dotnet user secrets set "Snowflake:Warehouse" "COMPUTE_WH"
dotnet user secrets set "Snowflake:Database" "SNOWFLAKE_SAMPLE_DATA"
dotnet user secrets set "Snowflake:Schema" "TPCH_SF1"
```

The required secrets are:
- `Snowflake:Account` – Your Snowflake account identifier
- `Snowflake:User` – Your Snowflake username
- `Snowflake:Password` – Your Snowflake password
- `Snowflake:Warehouse` – Snowflake warehouse name (optional, default: `COMPUTE_WH`)
- `Snowflake:Database` – Database name (optional, default: `SNOWFLAKE_SAMPLE_DATA`)
- `Snowflake:Schema` – Schema name (optional, default: `TPCH_SF1`)

## How to Run Locally

1. **Prerequisites:** .NET 10.0 SDK and Snowflake account credentials
2. **Navigate to the project:**
   ```bash
   cd project-demos/snowflake
   ```
3. **Set up your Snowflake credentials** using `dotnet user secrets` (see "Setting Up Credentials" section above)
4. **Restore dependencies:**
   ```bash
   dotnet restore
   ```
5. **Start the app:**
   ```bash
   dotnet watch
   ```
6. **Open your browser** to the URL shown in the terminal (typically `http://localhost:5010`)

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
