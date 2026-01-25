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
- **Interactive Dashboard** with multiple visualization panels:
  1. **KPI Cards** - Total downloads, total versions, latest version, and most popular version with animated count-up effects
  2. **Top Popular Versions** - Bar chart showing top 3 most downloaded versions
  3. **Monthly Downloads** - Line chart with average trend line comparing current month vs historical average
  4. **Releases vs Pre-releases** - Pie chart showing distribution of release types
  5. **Recent Versions Distribution** - Filterable bar chart showing versions with most downloads:
     - Date range filtering (from/to dates)
     - Pre-release toggle (include/exclude pre-releases)
     - Configurable count (2-20 versions)
  6. **Version Releases Over Time** - Timeline chart showing release frequency by month
  7. **All Versions Table** - Complete searchable, sortable, and filterable table with all package versions
- **Smart Caching** - 15-minute cache for NuGet API responses to minimize API calls
- **Automatic Data Refresh** - Background revalidation keeps data fresh
- **Animated Metrics** - Smooth count-up animations for download and version numbers
- **Responsive Design** - Clean, modern UI with optimized layouts
- **Error Handling** - Graceful error states with retry functionality
- **Loading States** - Skeleton loaders during data fetching

## Prerequisites

1. **.NET 10.0 SDK** or later
2. **Ivy Framework** - This project uses local project references to Ivy Framework
   - Ensure you have the Ivy Framework cloned locally at: `C:\git\Ivy-Interactive\Ivy-Framework`

## Setup

### 1. Navigate to the Project Directory

```bash
cd project-demos/ivy-insights
```

### 2. Restore Dependencies

```bash
dotnet restore
```

### 3. Run the Application

```bash
dotnet watch
```

### 4. Open Your Browser

Navigate to the URL shown in the terminal (typically `http://localhost:5010/ivy-insights`)

## How It Works

1. **Data Fetching**: The app fetches data from NuGet API v3:
   - Package registration data (all versions with published dates)
   - Package search API (download statistics per version)
   - Additional version downloads for versions not in search results
2. **Data Processing**: Statistics are calculated and aggregated:
   - Total downloads across all versions
   - Monthly download trends
   - Version popularity rankings
   - Release vs pre-release distribution
   - Growth metrics (current month vs average)
3. **Caching**: Data is cached for 15 minutes to:
   - Reduce API calls
   - Improve performance
   - Share cache across all users (server-side caching)
4. **Display**: Results are shown in an interactive dashboard with:
   - Animated number count-ups
   - Interactive charts with filtering
   - Real-time data updates
   - Responsive layouts

## Architecture

```
IvyInsights/
├── Apps/
│   └── NuGetStatsApp.cs          # Main application with dashboard
├── Models/
│   └── Models.cs                  # Data models (PackageStatistics, VersionInfo, etc.)
├── Services/
│   ├── INuGetStatisticsProvider.cs
│   ├── NuGetApiClient.cs          # NuGet API v3 client
│   └── NuGetStatisticsProvider.cs # Statistics aggregation service
├── Program.cs                      # Application entry point
└── GlobalUsings.cs                 # Global using directives
```

## Technologies Used

- **Ivy Framework** - UI framework for building interactive applications
- **Ivy.Charts** - Bar charts, line charts, and pie charts for data visualization
- **NuGet API v3** - Package registration and search APIs
- **UseQuery Hook** - Automatic data fetching, caching, and state management
- **JobScheduler** - Coordinated animations for number count-ups
- **.NET 10.0** - Runtime platform
- **HttpClient** - API communication with compression support

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
