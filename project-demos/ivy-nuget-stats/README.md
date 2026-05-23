# Ivy Insights - NuGet Statistics Dashboard

## Description

Ivy Insights is a comprehensive web application for visualizing and analyzing NuGet package statistics. It displays real-time data about package versions, downloads, releases, and trends with interactive charts, animated metrics, and detailed analytics. Built specifically for monitoring the Ivy framework package, but can be easily adapted for any NuGet package.

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
- **Smart Caching** - 15-minute cache for NuGet API responses, 5-minute cache for database queries
- **Automatic Data Refresh** - Background revalidation keeps data fresh
- **Animated Metrics** - Smooth count-up animations for download and version numbers
- **Responsive Design** - Clean, modern UI with optimized layouts
- **Error Handling** - Graceful error states with retry functionality
- **Loading States** - Skeleton loaders during data fetching

## Prerequisites

1. **.NET 10.0 SDK** or later
2. **Ivy Framework** - This project uses local project references to Ivy Framework
   - Ensure you have the Ivy Framework cloned locally at: `C:\git\Ivy-Interactive\Ivy-Framework`
3. **PostgreSQL Database** (optional, for daily download tracking)

## Setup

### 1. Navigate to the Project Directory

```bash
cd project-demos/ivy-insights
```

### 2. Restore Dependencies

```bash
dotnet restore
```

### 3. Configure Database Connection (Optional)

If you want to use daily download tracking, you need to set up a PostgreSQL database connection using secrets.

#### Using User Secrets (Recommended for Development)

```bash
dotnet user-secrets set "DB_CONNECTION_STRING" "Host=hostname;Port=5432;Database=dbname;Username=user;Password=pass"
```

### 4. Run the Application

```bash
dotnet watch
```

### 5. Open Your Browser

Navigate to the URL shown in the terminal (typically `http://localhost:5010/ivy-insights`)

## How It Works

1. **Data Fetching**: The app fetches data from multiple sources:
   - **NuGet API v3**: Package registration data (all versions with published dates), package search API (download statistics per version)
   - **PostgreSQL Database**: Daily download statistics stored by GitHub Actions workflow
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
- Runs daily at midnight UTC
- Fetches current download count from NuGet API
- Stores data in PostgreSQL database
- Supports manual runs with custom dates for historical data simulation

## Architecture

```
IvyInsights/
├── Apps/
│   └── NuGetStatsApp.cs          # Main application with dashboard
├── Models/
│   └── Models.cs                  # Data models (PackageStatistics, VersionInfo, DailyDownloadStats, etc.)
├── Services/
│   ├── INuGetStatisticsProvider.cs
│   ├── NuGetApiClient.cs          # NuGet API v3 client
│   ├── NuGetStatisticsProvider.cs # Statistics aggregation service
│   ├── IDatabaseService.cs        # Database service interface
│   └── DatabaseService.cs         # PostgreSQL database service for daily statistics
├── Program.cs                      # Application entry point
└── GlobalUsings.cs                 # Global using directives
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

## Customization

To monitor a different NuGet package, change the `PackageId` constant in `NuGetStatsApp.cs`:

```csharp
private const string PackageId = "YourPackageName";
```

## Deploy

Deploy this application to Ivy's hosting platform:

```bash
cd project-demos/ivy-insights
ivy deploy
```

## Learn More

- **Ivy Framework**: [github.com/Ivy-Interactive/Ivy-Framework](https://github.com/Ivy-Interactive/Ivy-Framework)
- **Ivy Documentation**: [docs.ivy.app](https://docs.ivy.app)
- **NuGet API Documentation**: [learn.microsoft.com/nuget/api](https://learn.microsoft.com/en-us/nuget/api/overview)

## Tags

NuGet, Statistics, Analytics, Data Visualization, Dashboard, Ivy Framework, C#, .NET, Package Management, Metrics
