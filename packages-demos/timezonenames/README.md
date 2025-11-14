# TimeZoneNames

<img width="1607" height="909" alt="image" src="https://github.com/user-attachments/assets/81738375-7197-49b7-a2e3-560088eaa1ab" />

## One-Click Development Environment

[![Open in GitHub Codespaces](https://github.com/codespaces/badge.svg)](https://github.com/codespaces/new?hide_repo_select=true&ref=main&repo=Ivy-Interactive%2FIvy-Examples&machine=standardLinux32gb&devcontainer_path=.devcontainer%2Ftimezonenames%2Fdevcontainer.json&location=EuropeWest)

Click the badge above to open Ivy Examples repository in GitHub Codespaces with:
- **.NET 9.0** SDK pre-installed
- **Ready-to-run** development environment
- **No local setup** required

## Created Using Ivy

Web application created using [Ivy-Framework](https://github.com/Ivy-Interactive/Ivy-Framework).

**Ivy** - The ultimate framework for building internal tools with LLM code generation by unifying front-end and back-end into a single C# codebase. With Ivy, you can build robust internal tools and dashboards using C# and AI assistance based on your existing database.

Ivy is a web framework for building interactive web applications using C# and .NET.

## Interactive Example For Time Zone Conversion

This example demonstrates time zone conversion and lookup using the [TimeZoneConverter library](https://github.com/mj1856/TimeZoneConverter) integrated with Ivy. TimeZoneConverter is a lightweight library that provides conversion between IANA, Windows, and Rails time zone identifiers.

**What This Application Does:**

This specific implementation creates a **Time Zone Converter** application that allows users to:

- **Search Time Zones**: Select a time zone type (IANA, Windows, or Rails) and search through available time zones
- **Convert Between Formats**: Automatically convert between IANA, Windows, and Rails time zone formats
- **View Current Time**: Display the current time in the selected time zone
- **Synchronized Display**: All three time zone formats are automatically synchronized when a zone is selected
- **Interactive Search**: Real-time search filtering through time zone lists
- **Split-Panel Layout**: Search interface on the left, results and details on the right

**Technical Implementation:**

- Uses TimeZoneConverter's `TZConvert` class for robust time zone conversion
- Implements async search with filtering for large time zone lists
- Creates interactive list components with click handlers for zone selection
- Handles automatic synchronization between IANA, Windows, and Rails formats
- Displays current time conversion based on selected IANA time zone
- Supports real-time search with case-insensitive filtering
- Uses Ivy's reactive state management with `UseState` and `UseEffect` hooks

## How to Run

1. **Prerequisites**: .NET 9.0 SDK
2. **Navigate to the example**:
   ```bash
   cd packages-demos/timezonenames
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
   cd packages-demos/timezonenames
   ```
2. **Deploy to Ivy hosting**:
   ```bash
   ivy deploy
   ```
This will deploy your time zone converter application with a single command.

## Learn More

- TimeZoneConverter for .NET overview: [github.com/mj1856/TimeZoneConverter](https://github.com/mj1856/TimeZoneConverter)
- Ivy Documentation: [docs.ivy.app](https://docs.ivy.app)
