# Squirrel

<img width="1314" height="916" alt="image" src="https://github.com/user-attachments/assets/9e2f17bc-c9e2-4090-b07b-17c457125699" />

<img width="1313" height="909" alt="image" src="https://github.com/user-attachments/assets/44d32216-ddb7-4f8b-acb5-a7a42e406f9c" />

<img width="1313" height="914" alt="image" src="https://github.com/user-attachments/assets/f459389e-9b56-47cd-8888-6832fda80441" />

## One-Click Development Environment

[![Open in GitHub Codespaces](https://github.com/codespaces/badge.svg)](https://github.com/codespaces/new?hide_repo_select=true&ref=main&repo=Ivy-Interactive%2FIvy-Examples&machine=standardLinux32gb&devcontainer_path=.devcontainer%2Fsquirrel%2Fdevcontainer.json&location=EuropeWest)

Click the badge above to open this example in GitHub Codespaces with:
- **.NET 9.0** SDK pre-installed
- **Ready-to-run** environment (no local setup)

## Created Using Ivy

Web application created using [Ivy-Framework](https://github.com/Ivy-Interactive/Ivy-Framework).

**Ivy** unifies front-end and back-end into a single C# codebase for building internal tools and dashboards.

## What This Application Does

This example demonstrates data processing using the [Squirrel](https://github.com/sudipto80/Squirrel) library integrated with Ivy:

- **Load CSV**: Reads `fashion_products.csv` into a Squirrel `Table`
- **Filter & Sort**: Interactive controls for rating, price, brand, category, and sort order
- **Column Visibility**: Toggle which columns are shown/exported
- **Export Filtered CSV**: Download the current filtered view
- **Pagination**: Navigate results with page controls (default page size: 25)
- **Charts**: Brand-level line chart showing counts per brand and overall average for a selected product
- **Details Table**: Tabular breakdown of brand counts for the selected product

## Technical Implementation

- Uses Squirrel `Table` APIs for CSV load, sort, and iteration
- UI built with Ivy components and reactive state (`UseState`, `UseEffect`)
- Pagination via `Pagination` widget bound to state (top/bottom of the table)
- Chart built with Ivy `LineChart` using dimensions/measures
- Key files:
  - `squirrel/Apps/SquirrelCsvApp.cs` – data editor with filtering, sorting, pagination, and CSV export
  - `squirrel/Apps/PhysicsSimulationApp.cs` – chart and details view for brand counts

## How to Run

1. Prerequisites: **.NET 9.0 SDK**
2. Navigate to this example:
   ```bash
   cd squirrel
   ```
3. Restore dependencies:
   ```bash
   dotnet restore
   ```
4. Run the application:
   ```bash
   dotnet watch
   ```
5. Open your browser to the URL shown in the terminal (typically `http://localhost:5010`).

## How to Deploy

1. Navigate to this example:
   ```bash
   cd squirrel
   ```
2. Deploy to Ivy hosting:
   ```bash
   ivy deploy
   ```

## Learn More

- Squirrel library: `https://github.com/sudipto80/Squirrel`
- Ivy Documentation: `https://docs.ivy.app`