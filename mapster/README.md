# Mapster

<img width="1918" height="912" alt="image" src="https://github.com/user-attachments/assets/6f200388-6a4a-46ac-a345-6a74732f572a" />

## One-Click Development Environment

[![Open in GitHub Codespaces](https://github.com/codespaces/badge.svg)](https://github.com/codespaces/new?hide_repo_select=true&ref=main&repo=Ivy-Interactive%2FIvy-Examples&machine=standardLinux32gb&devcontainer_path=.devcontainer%2Fmapster%2Fdevcontainer.json&location=EuropeWest)

Click the badge above to open Ivy Examples repository in GitHub Codespaces with:
- **.NET 9.0** SDK pre-installed
- **Ready-to-run** development environment
- **No local setup** required

## Created Using Ivy

Web application created using [Ivy-Framework](https://github.com/Ivy-Interactive/Ivy-Framework).

**Ivy** - The ultimate framework for building internal tools with LLM code generation by unifying front-end and back-end into a single C# codebase. With Ivy, you can build robust internal tools and dashboards using C# and AI assistance based on your existing database.

Ivy is a web framework for building interactive web applications using C# and .NET.

## Interactive Example For Object-to-Object Mapping

This example demonstrates object-to-object mapping using the [Mapster library](https://github.com/rubberduck-sharp/Mapster) integrated with Ivy. Mapster is a powerful mapping library for .NET that automatically transfers data between similar objects.

**What This Application Does:**

This specific implementation demonstrates **bidirectional mapping** between `Person` and `PersonDto` classes:

- **Edit and Convert**: Modify data in either class and convert it to the other using arrow buttons
- **Automatic Transformations**: See how `FirstName` and `LastName` combine into `FullName` or split back into components
- **Smart Validation**: Built-in validation ensures data consistency (e.g., preventing `HasSingleWordName=true` when `FullName` has 2+ words)
- **Random Age Generation**: Ages are randomly generated based on adult status (18-100 for adults, 0-17 for minors)
- **Real-time Updates**: Changes reflect instantly in the UI with error highlighting when validation fails

**Technical Implementation:**

- Uses Mapster's `TypeAdapterConfig` for flexible mapping configuration
- Implements bidirectional mapping with `Person ↔ PersonDto` transformations
- Combines fields using `FirstName + LastName → FullName`
- Splits `FullName` back using `AfterMapping` with smart logic
- Calculates derived properties (`IsAdult` from `Age`, `HasSingleWordName` validation)
- Generates random ages in appropriate ranges based on adult status
- Shows validation errors inline using Ivy's `.Invalid()` method
- Handles edge cases like empty `LastName` without adding extra spaces

## How to Run

1. **Prerequisites**: .NET 9.0 SDK
2. **Navigate to the example**:
   ```bash
   cd mapster
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
   cd mapster
   ```
2. **Deploy to Ivy hosting**:
   ```bash
   ivy deploy
   ```
This will deploy your mapping demonstration application with a single command.

## Learn More

- Mapster for .NET overview: [github.com/rubberduck-sharp/Mapster](https://github.com/rubberduck-sharp/Mapster)
- Ivy Documentation: [docs.ivy.app](https://docs.ivy.app)
