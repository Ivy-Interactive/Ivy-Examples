# Cronos 

<img width="1919" height="759" alt="image" src="https://github.com/user-attachments/assets/40d090f4-be98-4a49-bfab-f7b885666263" />

## One-Click Development Environment

[![Open in GitHub Codespaces](https://github.com/codespaces/badge.svg)](https://github.com/codespaces/new?hide_repo_select=true&ref=main&repo=Ivy-Interactive%2FIvy-Examples&machine=standardLinux32gb&devcontainer_path=.devcontainer%2Fcronos%2Fdevcontainer.json&location=EuropeWest)

Click the badge above to open Ivy Examples repository in GitHub Codespaces with:
- **.NET 9.0** SDK pre-installed
- **Ready-to-run** development environment
- **No local setup** required

## Created Using Ivy

Web application created using [Ivy-Framework](https://github.com/Ivy-Interactive/Ivy-Framework).

**Ivy** - The ultimate framework for building internal tools with LLM code generation by unifying front-end and back-end into a single C# codebase. With Ivy, you can build robust internal tools and dashboards using C# and AI assistance based on your existing database.

Ivy is a web framework for building interactive web applications using C# and .NET.

## Interactive Example For Cron Expression Parsing

This example demonstrates cron expression parsing and validation using the [Cronos library](https://github.com/HangfireIO/Cronos) integrated with Ivy. Cronos is a fully-featured .NET library for working with Cron expressions, built with time zones in mind and intuitively handles daylight saving time transitions.

**What This Application Does:**

This specific implementation creates a **Cron Expression Parser** application that allows users to:

- **Parse Cron Expressions**: Validate and parse cron expressions with real-time feedback
- **Timezone Support**: Select from all available system timezones for accurate calculations
- **Next Occurrence Calculation**: Calculate the next occurrence time for any valid cron expression
- **Predefined Templates**: Choose from common cron patterns (every minute, daily, weekly, etc.)
- **Seconds Support**: Toggle between 5-field and 6-field cron expressions (with seconds)
- **Interactive UI**: Split-panel layout with controls on the left and quick guide on the right
- **Built-in Help**: Comprehensive guide with cron format explanation and examples

**Technical Implementation:**

- Uses Cronos's `CronExpression.Parse()` method with `CronFormat` enumeration
- Supports both standard (5-field) and extended (6-field with seconds) cron formats
- Handles timezone conversions using `TimeZoneInfo.FindSystemTimeZoneById()`
- Implements dropdown selection for predefined cron patterns
- Creates automatic cron expression application via `UseEffect` hooks
- Single C# view (`Apps/CronosApp.cs`) built with Ivy UI primitives

## How to Run

1. **Prerequisites**: .NET 9.0 SDK
2. **Navigate to the example**:
   ```bash
   cd cronos
   ```
3. **Restore dependencies**:
   ```bash
   dotnet restore
   ```
4. **Run the application**:
   ```bash
   dotnet watch
   ```
5. **Open your browser** to the URL shown in the terminal (typically `http://localhost:5010`)

## How to Deploy

Deploy this example to Ivy's hosting platform:

1. **Navigate to the example**:
   ```bash
   cd cronos
   ```
2. **Deploy to Ivy hosting**:
   ```bash
   ivy deploy
   ```
This will deploy your cron expression parser application with a single command.

## Learn More

- Cronos GitHub repository: [github.com/HangfireIO/Cronos](https://github.com/HangfireIO/Cronos)
- Ivy Documentation: [docs.ivy.app](https://docs.ivy.app)