# NodaTime

<img width="1915" height="911" alt="image" src="https://github.com/user-attachments/assets/a638d752-a591-4245-a1c5-da19eac3353c" />

## One-Click Development Environment

[![Open in GitHub Codespaces](https://github.com/codespaces/badge.svg)](https://github.com/codespaces/new?hide_repo_select=true&ref=main&repo=Ivy-Interactive%2FIvy-Examples&machine=standardLinux32gb&devcontainer_path=.devcontainer%2Fnodatime%2Fdevcontainer.json&location=EuropeWest)

Click the badge above to open Ivy Examples repository in GitHub Codespaces with:
- **.NET 9.0** SDK pre-installed
- **Ready-to-run** development environment
- **No local setup** required

## Created Using Ivy

Web application created using [Ivy-Framework](https://github.com/Ivy-Interactive/Ivy-Framework).

**Ivy** - The ultimate framework for building internal tools with LLM code generation by unifying front-end and back-end into a single C# codebase. With Ivy, you can build robust internal tools and dashboards using C# and AI assistance based on your existing database.

Ivy is a web framework for building interactive web applications using C# and .NET.

## Interactive Example For Timezone Handling

This example demonstrates robust timezone handling using the [NodaTime library](https://github.com/nodatime/nodatime) integrated with Ivy. NodaTime is a better date and time API for .NET that provides a more intuitive, safer, and more performant alternative to the built-in `DateTime` types.

**What This Application Does:**

This specific implementation creates a **Timezone Conversion** application that allows users to:

- **Select Any Timezone**: Search and select from all available timezones in the TZDB (Time Zone Database)
- **Real-Time Conversion**: Instantly see the current time in the selected timezone
- **UTC Reference**: Always displays the current UTC time for reference
- **Formatted Display**: Shows local time in a human-readable format (e.g., "Monday, Jan 15 2024 14:30:45")
- **Interactive Search**: Searchable dropdown with all TZDB timezones for easy selection
- **Dynamic Updates**: UI automatically updates when a new timezone is selected

**Technical Implementation:**

- Uses NodaTime's `DateTimeZoneProviders.Tzdb` for accessing the full Time Zone Database
- Leverages `SystemClock.Instance.GetCurrentInstant()` for accurate UTC time
- Converts UTC `Instant` to `ZonedDateTime` in the selected timezone
- Uses `LocalDateTimePattern` for custom date/time formatting
- Implements reactive state management with Ivy's `UseState` and `UseEffect` hooks
- Creates responsive card-based layout with icons and structured information display
- Handles real-time timezone conversion with automatic UI updates

## How to Run

1. **Prerequisites**: .NET 9.0 SDK
2. **Navigate to the example**:
   ```bash
   cd nodatime
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
   cd nodatime
   ```
2. **Deploy to Ivy hosting**:
   ```bash
   ivy deploy
   ```
This will deploy your timezone conversion application with a single command.

## Learn More

- NodaTime library overview: [github.com/nodatime/nodatime](https://github.com/nodatime/nodatime)
- NodaTime documentation: [nodatime.org](https://nodatime.org)
- Ivy Documentation: [docs.ivy.app](https://docs.ivy.app)
