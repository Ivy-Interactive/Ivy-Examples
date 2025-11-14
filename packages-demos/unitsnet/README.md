# UnitsNet

<img width="1369" height="917" alt="image" src="https://github.com/user-attachments/assets/202d287d-10b0-4b9c-8026-f1640c12b3ca" />

## One-Click Development Environment

[![Open in GitHub Codespaces](https://github.com/codespaces/badge.svg)](https://github.com/codespaces/new?hide_repo_select=true&ref=main&repo=Ivy-Interactive%2FIvy-Examples&machine=standardLinux32gb&devcontainer_path=.devcontainer%2Funitsnet%2Fdevcontainer.json&location=EuropeWest)

Click the badge above to open Ivy Examples repository in GitHub Codespaces with:
- **.NET 9.0** SDK pre-installed
- **Ready-to-run** development environment
- **No local setup** required

## Created Using Ivy

Web application created using [Ivy-Framework](https://github.com/Ivy-Interactive/Ivy-Framework).

**Ivy** - The ultimate framework for building internal tools with LLM code generation by unifying front-end and back-end into a single C# codebase. With Ivy, you can build robust internal tools and dashboards using C# and AI assistance based on your existing database.

Ivy is a web framework for building interactive web applications using C# and .NET.

## Interactive Unit Converter Application

This example demonstrates unit conversion capabilities using the [UnitsNet library](https://github.com/angularsen/UnitsNet) integrated with Ivy. UnitsNet is a comprehensive .NET library for working with physical quantities and their units of measurement.

**What This Application Does:**

This specific implementation creates a **Unit Converter** application that allows users to:

- **Select Quantity Types**: Choose from a searchable list of measurement types (Temperature, Length, Mass, Volume, etc.)
- **Search Quantities**: Quickly find quantity types using a search input field
- **Select Source Units**: Pick the unit to convert from using an interactive list
- **Select Target Units**: Pick the unit to convert to using an interactive list
- **Enter Values**: Input numeric values to convert between units
- **View Results**: See real-time conversion results in a formatted display
- **Automatic Clearing**: Source and target units are automatically cleared when quantity type changes

**Technical Implementation:**

- Uses UnitsNet's `Quantity` API for accurate unit conversions
- Implements searchable quantity selection with real-time filtering
- Creates interactive list-based UI for unit selection with subtitles
- Handles state management with automatic unit clearing on quantity change
- Supports all UnitsNet quantity types (Temperature, Length, Mass, Area, Volume, etc.)
- Displays conversion results in code format for clarity
- Uses Ivy's `UseEffect` hook for reactive state management
- Implements responsive card-based layout

**Key Features:**

- **Search Functionality**: Search through available quantity types
- **List-Based Selection**: All selections use intuitive list interfaces with subtitles
- **Real-Time Conversion**: Instant conversion results as you type
- **Comprehensive Coverage**: Supports all UnitsNet quantity types and their units
- **Clean UI**: Modern card-based layout with clear visual hierarchy

## How to Run

1. **Prerequisites**: .NET 9.0 SDK
2. **Navigate to the example**:
   ```bash
   cd packages-demos/unitsnet
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
   cd packages-demos/unitsnet
   ```
2. **Deploy to Ivy hosting**:
   ```bash
   ivy deploy
   ```
This will deploy your unit converter application with a single command.

## Learn More

- UnitsNet library overview: [github.com/angularsen/UnitsNet](https://github.com/angularsen/UnitsNet)
- Ivy Documentation: [docs.ivy.app](https://docs.ivy.app)
