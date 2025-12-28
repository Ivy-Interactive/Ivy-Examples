# ClickHouse Dashboard

## Description 
ClickHouse Dashboard is a web application for visualizing and analyzing ClickHouse database statistics with interactive charts, real-time metrics, table statistics, and comprehensive data insights.

## One-Click Development Environment

[![Open in GitHub Codespaces](https://github.com/codespaces/badge.svg)](https://github.com/codespaces/new?hide_repo_select=true&ref=main&repo=Ivy-Interactive%2FIvy-Examples&machine=standardLinux32gb&devcontainer_path=.devcontainer%2Fclickhouse-dashboard%2Fdevcontainer.json&location=EuropeWest)

Launch a ready-to-code workspace with:
- **.NET 10.0** SDK pre-installed
- **ClickHouse.Driver** and Ivy tooling available out of the box
- **Zero local setup** required

## Built With Ivy

This web application is powered by [Ivy Framework](https://github.com/Ivy-Interactive/Ivy-Framework).

**Ivy** unifies front-end and back-end development in C#, enabling rapid internal tool development with AI-assisted workflows, typed components, and reactive UI primitives.

## Interactive ClickHouse Analytics Dashboard

This demo showcases how to build a comprehensive analytics dashboard using the [ClickHouse .NET Driver](https://github.com/ClickHouse/clickhouse-csharp) within an Ivy file-based application.

### Features

- **Database Overview** – View total rows, tables count, database size, and average rows per table
- **Interactive Charts** – Pie charts for size distribution and status breakdowns, bar charts for top tables
- **Table Statistics** – Comprehensive data table showing all tables with row counts and sizes
- **Transaction Analytics** – Real-time transaction status statistics with counts and totals
- **Log Timeline** – 30-day log activity visualization with line charts
- **User Status Dashboard** – User status breakdown with interactive pie charts
- **Auto-Docker Integration** – Automatically starts ClickHouse via Docker Compose if not running
- **Skeleton Loading States** – Elegant loading placeholders during data fetching
- **Error Handling** – Clear error messages for connection and query issues
- **Responsive Layout** – Clean dashboard interface with grid-based card layouts

### Configuration

The application automatically detects and connects to ClickHouse. By default, it uses:
- **Host:** `localhost`
- **Port:** `8123`
- **Username:** `default`
- **Password:** `default`
- **Database:** `default`

You can override the ClickHouse host by setting the `CLICKHOUSE_HOST` environment variable (useful for Docker deployments).

## How to Run Locally

This is a **file-based Ivy application** - no project file (.csproj) needed!

1. **Prerequisites:** .NET 8.0+ SDK (or .NET 10.0 for latest features)

2. **Navigate to the project:**
   ```bash
   cd project-demos/clickhouse-dashboard
   ```

3. **Start the application:**
   ```bash
   dotnet run ClickHouseDashboard.cs
   ```

   The application will **automatically**:
   - Check if ClickHouse is running on port 8123
   - Start ClickHouse via Docker Compose if it's not running
   - Wait for ClickHouse to become available (up to 30 seconds)
   - Connect and display statistics for all tables

   **No manual Docker commands needed!** The application handles everything automatically.

4. **Open your browser** to the URL shown in the terminal (typically `http://localhost:5010`)

### Running with Docker Compose

For a complete containerized setup with both ClickHouse and the application:

1. **Start both services:**
   ```bash
   docker-compose up --build
   ```

   This will:
   - Start ClickHouse on port **8123**
   - Build and start the dashboard application on port **5010**
   - Wait for ClickHouse to be healthy before starting the app

2. **Run in background (detached mode):**
   ```bash
   docker-compose up -d --build
   ```

3. **Stop all services:**
   ```bash
   docker-compose down
   ```

4. **Stop and remove all data (fresh start):**
   ```bash
   docker-compose down -v
   ```

   This removes the Docker volume with all ClickHouse data. On next startup, `init.sql` will create fresh tables with new random data.

### Manual Docker Control (Optional)

If you prefer to manage ClickHouse separately:

**Start only ClickHouse:**
```bash
docker-compose up -d clickhouse
```

**Stop ClickHouse:**
```bash
docker-compose down
```

## Sample Data Structure

Docker Compose will automatically create test tables on first startup via `init.sql`:

- **`events`** – Event logs (1,000,000 rows)
- **`users`** – User records (500,000 rows)
- **`sessions`** – User sessions (500,000 rows)
- **`metrics`** – System metrics (2,000,000 rows)
- **`logs`** – Application logs (1,000,000 rows)
- **`transactions`** – Transaction records (300,000 rows)

These tables are populated with realistic sample data for demonstration purposes.

## Deploy to Ivy Hosting

1. **Navigate to the project:**
   ```bash
   cd project-demos/clickhouse-dashboard
   ```

2. **Deploy:**
   ```bash
   ivy deploy
   ```

## Learn More

- ClickHouse Documentation: [clickhouse.com/docs](https://clickhouse.com/docs)
- ClickHouse .NET Driver: [github.com/ClickHouse/clickhouse-csharp](https://github.com/ClickHouse/clickhouse-csharp)
- Ivy documentation: [docs.ivy.app](https://docs.ivy.app)

## Tags 
ClickHouse, Analytics Dashboard, Data Visualization, Database Monitoring, SQL, OLAP
