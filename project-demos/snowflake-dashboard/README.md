# Snowflake Dashboard

## Description 

Snowflake Dashboard is a web application for visualizing and analyzing data from Snowflake databases. It provides interactive charts, metrics, and data tables to explore brand analytics, price distributions, container types, and size patterns from Snowflake sample data.

## Live Demo

[![Live Demo](https://img.shields.io/badge/Live%20Demo-Snowflake%20Dashboard-blue?style=for-the-badge)](https://ivy-projectdemos-snowflake-dashboard.sliplane.app)

https://github.com/user-attachments/assets/9bede1ff-1e43-41a6-8fcc-71c68b3adefc

## One-Click Development Environment

[![Open in GitHub Codespaces](https://github.com/codespaces/badge.svg)](https://github.com/codespaces/new?hide_repo_select=true&ref=main&repo=Ivy-Interactive%2FIvy-Examples&machine=standardLinux32gb&devcontainer_path=.devcontainer%2Fsnowflake-dashboard%2Fdevcontainer.json&location=EuropeWest)

Launch a ready-to-code workspace with:
- **.NET 9.0** SDK pre-installed
- **Snowflake.Data** SDK and Ivy tooling available out of the box
- **Zero local setup** required

## Built With Ivy

This web application is powered by [Ivy Framework](https://github.com/Ivy-Interactive/Ivy-Framework).

**Ivy** unifies front-end and back-end development in C#, enabling rapid internal tool development with AI-assisted workflows, typed components, and reactive UI primitives.

## Interactive Snowflake Analytics Dashboard

This demo showcases how to build a comprehensive analytics dashboard using the official [Snowflake .NET Connector](https://github.com/snowflakedb/snowflake-connector-net) within an Ivy application.

### Features

- **Interactive Credentials Management** – Enter Snowflake credentials via UI or configure via user secrets
- **Cards** – Display total items, average price, min/max prices, and brand count
- **Brand Distribution Chart** – Pie chart showing distribution of top brands
- **Price Analytics** – Line charts for minimum and maximum prices by brand
- **Average Price Analysis** – Bar chart comparing average prices across brands
- **Size Distribution** – Bar chart showing size distribution for the most popular brand
- **Container Analysis** – Pie chart and bar charts for container type distribution
- **Data Tables** – Sortable table with detailed brand statistics
- **Skeleton Loading States** – Elegant loading placeholders during data fetching
- **Error Handling** – Clear error messages for connection and query issues
- **Responsive Layout** – Clean, modern dashboard interface with floating action buttons

### Configuration

The dashboard can be configured in two ways:

1. **Via UI** – Enter credentials directly in the application interface (recommended for quick testing)
2. **Via User Secrets** – Configure credentials using `dotnet user secrets` (recommended for production)

## How to Run Locally

1. **Prerequisites:** .NET 9.0 SDK and Snowflake account credentials
2. **Navigate to the project:**
   ```bash
   cd project-demos/snowflake-dashboard
   ```
3. **Set up your Snowflake credentials** (choose one):
   - **Option A:** Use the UI to enter credentials when the app starts
   - **Option B:** Configure via `dotnet user secrets`
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
   cd project-demos/snowflake-dashboard
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

Snowflake, Dashboard, Data Visualization, Analytics, Business Intelligence, Charts, SQL, Data Warehouse
