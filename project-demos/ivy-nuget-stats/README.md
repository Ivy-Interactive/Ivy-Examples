# Ivy Insights - NuGet Statistics Dashboard

## Description

Ivy Insights is a web application for visualizing and analyzing NuGet package statistics. It shows package versions, downloads, releases, and trends with interactive charts and animated metrics.

The dashboard has two tabs: **Ivy Framework** (`Ivy` on NuGet) and **Ivy Tendril** (`Ivy.Tendril`). The same products are available over a small **HTTPS REST API** (see [HTTP API](#http-api) and [Testing](#testing)).

## One-Click Development Environment

[![Open in GitHub Codespaces](https://github.com/codespaces/badge.svg)](https://github.com/codespaces/new?hide_repo_select=true&ref=main&repo=Ivy-Interactive%2FIvy-Examples&machine=standardLinux32gb&devcontainer_path=.devcontainer%2Fivy-insights%2Fdevcontainer.json&location=EuropeWest)

Click the badge above to open Ivy Examples repository in GitHub Codespaces with:

- **.NET 10.0** SDK pre-installed
- **Ready-to-run** development environment
- **No local setup** required

## Features

- **Real-Time NuGet Statistics** - Automatic data fetching from NuGet API v3
- **Daily Download Tracking** - PostgreSQL database integration for tracking daily download statistics
- **GitHub Actions Integration** - Automated daily data collection via GitHub Actions workflow
- **Two products** - Ivy Framework and Ivy.Tendril (NuGet + matching GitHub repo per tab)
- **Interactive Dashboard** with multiple visualization panels:
  1. **KPI Cards** - Total downloads, total versions, latest version, and most popular version with animated count-up effects
  2. **Top Popular Versions** - Bar chart showing top 3 most downloaded versions
  3. **Daily Downloads (Last 30 Days)** - Line chart showing daily download growth with average trend line
  4. **Daily Download Statistics Table** - Detailed table with daily download counts and growth
  5. **Releases vs Pre-releases** - Pie chart showing distribution of release types
  6. **Recent Versions Distribution** - Filterable bar chart showing versions with most downloads:
     - Date range filtering (from/to dates)
     - Pre-release toggle (include/exclude pre-releases)
     - Configurable count (2-20 versions)
  7. **Version Releases Over Time** - Timeline chart showing release frequency by month
  8. **All Versions Table** - Complete searchable, sortable, and filterable table with all package versions
  9. **GitHub Stars** - Star history, stargazers daily chart, and refresh from GitHub API (per tab / per repo)
- **Smart Caching** - 15-minute cache for NuGet API responses, 5-minute cache for database queries
- **Automatic Data Refresh** - Background revalidation keeps data fresh
- **Animated Metrics** - Smooth count-up animations for download and version numbers
- **Responsive Design** - Clean, modern UI with optimized layouts
- **Error Handling** - Graceful error states with retry functionality
- **Loading States** - Skeleton loaders during data fetching

## Prerequisites

1. **.NET 10.0 SDK** or later
2. **Ivy Framework** - This project references Ivy via NuGet (`Ivy` package in `IvyInsights.csproj`)
3. **PostgreSQL Database** (optional for NuGet live charts; required for download history, GitHub stars from DB, and HTTP API metrics)

## Setup

### 1. Navigate to the Project Directory

```bash
cd project-demos/ivy-nuget-stats
```

### 2. Restore Dependencies

```bash
dotnet restore
```

### 3. Configure Database Connection (Optional)

For daily download tracking and GitHub statistics stored in Postgres:

```bash
dotnet user-secrets set "DB_CONNECTION_STRING" "Host=hostname;Port=5432;Database=dbname;Username=user;Password=pass"
```

### 4. Run the Application

```bash
dotnet watch
```

### 5. Open Your Browser

Open the URL from the terminal (typically `https://localhost:5010/ivy-insights`).

## Testing

### UI (local)

1. `dotnet watch` in `project-demos/ivy-nuget-stats`
2. Open `/ivy-insights` in the browser
3. Switch tabs **Ivy Framework** / **Ivy Tendril** — each tab should show its own NuGet package and GitHub repo
4. With `DB_CONNECTION_STRING` set: heatmap, download history, and star charts use Postgres; **Refresh Stargazers** updates only the current tab’s repo

### HTTP API

Use [HTTP API](#http-api) below. Quick check:

```bash
curl -s "https://ivy-nuget-stats.sliplane.app/summary?package=tendril"
curl -sk "https://localhost:5010/summary?package=ivy"   # local dotnet watch; -k for dev cert
```

Or open `https://ivy-nuget-stats.sliplane.app/scalar` (local: `https://localhost:5010/scalar`).

### CLI

From [ivy-examples-cli](../../ivy-examples-cli/README.md):

```bash
ivy-examples nuget summary
ivy-examples nuget summary --package tendril
ivy-examples config set nuget_stats_base_url https://ivy-nuget-stats.sliplane.app
# local:
ivy-examples config set nuget_stats_base_url https://localhost:5010
```

### Database / workflow

- Run workflow [update-remote-postgres.yml](../../.github/workflows/update-remote-postgres.yml) manually (`workflow_dispatch`) to refresh Postgres for both packages and both GitHub repos
- Confirm rows in `nuget_history`, `github_stars_history`, and `github_stargazers` with the expected `package_name` / `repo_name`

## How It Works

1. **Data Fetching**: The app fetches data from multiple sources:
   - **NuGet API v3**: Package registration data (all versions with published dates), package search API (download statistics per version)
   - **PostgreSQL Database**: Daily download statistics and GitHub stars/stargazers stored by GitHub Actions workflow
2. **Data Processing**: Statistics are calculated and aggregated:
   - Total downloads across all versions (from NuGet API)
   - Daily download growth (from database)
   - Monthly download trends (from database daily data)
   - Version popularity rankings
   - Release vs pre-release distribution
   - Growth metrics (current month vs average)
3. **Caching**: Data is cached to optimize performance:
   - NuGet API: 15-minute cache
   - Database queries: 5-minute cache
   - Server-side caching shared across all users
4. **Display**: Results are shown in an interactive dashboard with:
   - Animated number count-ups
   - Interactive charts with filtering
   - Real-time data updates
   - Responsive layouts

### GitHub Actions Integration

The application works with a GitHub Actions workflow (`.github/workflows/update-remote-postgres.yml`) that:

- Runs on a schedule (and supports `workflow_dispatch`)
- Fetches NuGet download counts for **Ivy** and **Ivy.Tendril**
- Fetches GitHub stars and stargazers for **Ivy-Interactive/Ivy-Framework** and **Ivy-Interactive/Ivy-Tendril**
- Stores data in PostgreSQL

## HTTP API

**Base URL:** `https://ivy-nuget-stats.sliplane.app`  
**Docs:** `https://ivy-nuget-stats.sliplane.app/scalar` (OpenAPI: `/openapi`)

Select product with `package` (default `ivy`). Optional `repo` must match the package’s GitHub repo.

| `package` | NuGet id | GitHub `repo` |
|-----------|----------|----------------|
| `ivy`, `framework` | `Ivy` | `Ivy-Interactive/Ivy-Framework` |
| `tendril`, `ivy.tendril` | `Ivy.Tendril` | `Ivy-Interactive/Ivy-Tendril` |

| Path | Query |
|------|--------|
| `/summary` | `package`, `repo` |
| `/downloads` | `package` |
| `/downloads/history` | `package`, `days` |
| `/stars`, `/starred`, `/unstarred` | `package`, `repo` |

```bash
curl -s "https://ivy-nuget-stats.sliplane.app/summary"
curl -s "https://ivy-nuget-stats.sliplane.app/summary?package=tendril"
```

Responses include `package`, `repo`, `displayName`. Bad `package` or mismatched `package`+`repo` → `400` `{ "error": "..." }`.

Routes are defined in `Program.cs`; OpenAPI document includes Ivy Insights endpoints only.

## Architecture

```
IvyInsights/
├── Apps/
│   └── NuGetStatsApp.cs          # Dashboard (tabs: Ivy / Ivy.Tendril)
├── Models/
│   └── Models.cs                 # Data models
├── Services/
│   ├── NuGetApiClient.cs
│   ├── NuGetStatisticsProvider.cs
│   ├── DatabaseService.cs
│   ├── DatabaseUpdateService.cs  # GitHub stargazers refresh
│   ├── GithubRepoCatalog.cs
│   └── InsightsMetricsCatalog.cs # package/repo resolution for API
├── Program.cs                    # HTTP API + Scalar
└── GlobalUsings.cs
```

## Technologies Used

- **Ivy Framework** - UI framework for building interactive applications
- **Ivy.Charts** - Bar charts, line charts, and pie charts for data visualization
- **NuGet API v3** - Package registration and search APIs
- **PostgreSQL** - Database for storing daily download statistics
- **Npgsql** - .NET PostgreSQL data provider
- **UseQuery Hook** - Automatic data fetching, caching, and state management
- **JobScheduler** - Coordinated animations for number count-ups
- **.NET 10.0** - Runtime platform
- **HttpClient** - API communication with compression support
- **GitHub Actions** - Automated daily data collection
- **Scalar** - OpenAPI reference UI

## Key Features Explained

### Smart Filtering

- **Date Range Filtering**: Filter versions by publication date
- **Pre-release Toggle**: Include or exclude pre-release versions
- **Download Filtering**: Only show versions with download data
- **Configurable Count**: Display 2-20 most downloaded versions

### Performance Optimizations

- **Server-Side Caching**: 15-minute TTL shared across all users
- **Request Deduplication**: Multiple components requesting same data = single request
- **Stale-While-Revalidate**: Shows cached data immediately while fetching fresh data
- **HTTP Compression**: Gzip/deflate support for API responses
- **Efficient API Usage**: Combines multiple API endpoints for complete data

### Data Accuracy

- **Multiple Data Sources**: Combines registration API and search API for complete statistics
- **Fallback Mechanisms**: Handles missing download data gracefully
- **Version Normalization**: Ensures consistent version matching across APIs

## API Rate Limits

The NuGet API is public and doesn't require authentication, but has rate limits. The app optimizes API usage by:

- Caching responses for 15 minutes
- Combining multiple API calls efficiently
- Using compression to reduce bandwidth
- Sharing cache across all users

GitHub API rate limits apply to **Refresh Stargazers** and the scheduled workflow; use a token in CI if limits are hit.

## Customization

Dashboard tabs are defined in `IvyInsightsApp` in `NuGetStatsApp.cs`. To add another product, extend `InsightsMetricsCatalog` / `GithubRepoCatalog` and the workflow package/repo lists.

## Deploy

Deploy this application to Ivy's hosting platform:

```bash
cd project-demos/ivy-nuget-stats
ivy deploy
```

Production API: `https://ivy-nuget-stats.sliplane.app`

## Learn More

- **Ivy Framework**: [github.com/Ivy-Interactive/Ivy-Framework](https://github.com/Ivy-Interactive/Ivy-Framework)
- **Ivy Documentation**: [docs.ivy.app](https://docs.ivy.app)
- **NuGet API Documentation**: [learn.microsoft.com/nuget/api](https://learn.microsoft.com/en-us/nuget/api/overview)

## Tags

NuGet, Statistics, Analytics, Data Visualization, Dashboard, Ivy Framework, Ivy Tendril, C#, .NET, Package Management, Metrics, GitHub Stars
