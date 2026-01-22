# Ivy Insights - NuGet Statistics Dashboard

Web application created using [Ivy](https://github.com/Ivy-Interactive/Ivy) that displays real-time NuGet package statistics for the Ivy framework.

Ivy is a web framework for building interactive web applications using C# and .NET.

## Features

- Automatic loading of Ivy package statistics on startup
- Real-time data fetching from NuGet API
- Interactive charts and visualizations:
  - Version distribution (bar chart)
  - Version releases timeline (line chart)
  - Recent versions table
- Key metrics display:
  - Total versions count
  - Latest version
  - First and last published dates
  - Package information

## Run

```
dotnet watch
```

## Deploy

```
ivy deploy
```

## Built With

- [Ivy Framework](https://github.com/Ivy-Interactive/Ivy-Framework)
- [NuGet API v3](https://learn.microsoft.com/en-us/nuget/api/overview)