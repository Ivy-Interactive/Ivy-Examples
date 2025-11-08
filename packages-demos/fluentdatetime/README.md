# FluentDateTime

<img width="1906" height="911" alt="image" src="https://github.com/user-attachments/assets/a16b8f54-3142-4472-9ebc-380f260d805a" />

## One-Click Development Environment

[![Open in GitHub Codespaces](https://github.com/codespaces/badge.svg)](https://github.com/codespaces/new?hide_repo_select=true&ref=main&repo=Ivy-Interactive%2FIvy-Examples&machine=standardLinux32gb&devcontainer_path=.devcontainer%2Ffluentdatetime%2Fdevcontainer.json&location=EuropeWest)

Click the badge above to open Ivy Examples repository in GitHub Codespaces with:
- **.NET 9.0** SDK pre-installed
- **Ready-to-run** development environment
- **No local setup** required

## Created Using Ivy

Web application created using [Ivy-Framework](https://github.com/Ivy-Interactive/Ivy-Framework).

**Ivy** - The ultimate framework for building internal tools with LLM code generation by unifying front-end and back-end into a single C# codebase. With Ivy, you can build robust internal tools and dashboards using C# and AI assistance based on your existing database.

Ivy is a web framework for building interactive web applications using C# and .NET.

## Interactive Example For DateTime Operations

This example demonstrates DateTime operations using the [FluentDateTime library](https://github.com/FluentDateTime/FluentDateTime) integrated with Ivy. FluentDateTime allows cleaner DateTime expressions and operations with fluent syntax.

**What This Application Does:**

This specific implementation creates a **Date Calculator** application that allows users to:

- **Select Base Date & Time**: Choose any date and time as the starting point for calculations
- **Choose Operation**: Add or subtract time from the base date
- **Select Time Units**: Choose from Minutes, Hours, Days, Weeks, Months, or Years
- **Enter Amount**: Specify the quantity of time to add or subtract
- **Calculate Results**: Get computed date with time difference in days
- **Clear Form**: Reset all fields to default values
- **Interactive UI**: Real-time calculation with responsive design

**Technical Implementation:**

- Uses FluentDateTime's fluent syntax for clean DateTime operations
- Implements DateTimeInput with date and time selection
- Provides NumberInput with min/max validation (1-9999)
- Features SelectInput dropdowns for operation and time unit selection
- Calculates time differences and displays results in days
- Supports all major time units: Minutes, Hours, Days, Weeks, Months, Years
- Handles both Add and Subtract operations with proper DateTime arithmetic
- Uses Markdown formatting for enhanced result display

## How to Run

1. **Prerequisites**: .NET 9.0 SDK
2. **Navigate to the example**:
   ```bash
   cd fluentdatetime
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
   cd fluentdatetime
   ```
2. **Deploy to Ivy hosting**:
   ```bash
   ivy deploy
   ```
This will deploy your DateTime calculator application with a single command.

## Learn More

- FluentDateTime for .NET overview: [github.com/FluentDateTime/FluentDateTime](https://github.com/FluentDateTime/FluentDateTime)
- Ivy Documentation: [docs.ivy.app](https://docs.ivy.app)