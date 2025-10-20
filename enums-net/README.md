# Enums.NET 

<img width="1912" height="909" alt="image" src="https://github.com/user-attachments/assets/ee23135f-8211-44e2-83cc-0efa99286c11" />

## One-Click Development Environment

[![Open in GitHub Codespaces](https://github.com/codespaces/badge.svg)](https://github.com/codespaces/new?hide_repo_select=true&ref=main&repo=Ivy-Interactive%2FIvy-Examples&machine=standardLinux32gb&devcontainer_path=.devcontainer%2Fenums-net%2Fdevcontainer.json&location=EuropeWest)

Click the badge above to open Ivy Examples repository in GitHub Codespaces with:
- **.NET 9.0** SDK pre-installed
- **Ready-to-run** development environment
- **No local setup** required

## Created Using Ivy

Web application created using [Ivy-Framework](https://github.com/Ivy-Interactive/Ivy-Framework).

**Ivy** - The ultimate framework for building internal tools with LLM code generation by unifying front-end and back-end into a single C# codebase. With Ivy, you can build robust internal tools and dashboards using C# and AI assistance based on your existing database.

Ivy is a web framework for building interactive web applications using C# and .NET.

## Interactive Example For Enum Operations

This example demonstrates advanced enum operations using the [Enums.NET library](https://github.com/TylerBrinkley/Enums.NET) integrated with Ivy. Enums.NET is a high-performance, type-safe .NET enum utility library that extends System.Enum with powerful operations for enumeration, flag manipulation, validation, parsing, and attribute handling.

**What This Application Does:**

This specific implementation creates an **Enum Explorer** application that allows users to:

- **Browse Enum Types**: View different enum types (NumericOperator, DaysOfWeek, DayType, PriorityLevel) with their members and metadata
- **Enum Enumeration**: Explore various enumeration modes (All, Distinct, DisplayOrder, Flags) with detailed member information
- **Flag Operations**: Perform advanced flag manipulations (HasAllFlags, HasAnyFlags, CombineFlags, CommonFlags, RemoveFlags, GetFlags, ToggleFlags)
- **Validation Testing**: Test enum validation with different types including custom validators
- **Interactive UI**: Split-panel layout with operations on the left and enum viewers on the right
- **Markdown Results**: Beautiful formatted results with explanations and code examples

**Technical Implementation:**

- Uses Enums.NET's `Enums.GetMembers<T>()`, `FlagEnums`, and validation methods
- Implements custom `EnumMemberInfo` record for structured enum metadata
- Creates responsive dropdown menus for operation selection
- Generates Markdown-formatted results with syntax highlighting
- Handles custom validators like `DayTypeValidatorAttribute`
- Supports attribute inspection (`DescriptionAttribute`, `SymbolAttribute`, `PrimaryEnumMemberAttribute`)
- Single C# view (`Apps/EnumsNetDemoApp.cs`) built with Ivy UI primitives

**Enums.NET Features Demonstrated:**

- **Enumeration**: All, Distinct, DisplayOrder, and Flags modes
- **Flag Operations**: Complete set of flag manipulation methods
- **Validation**: Standard and custom enum validation
- **Attributes**: Accessing and displaying enum member attributes
- **Parsing**: Safe enum parsing with custom formats
- **Performance**: High-performance enum operations with caching

## How to Run

1. **Prerequisites**: .NET 9.0 SDK
2. **Navigate to the example**:
   ```bash
   cd enums-net
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
   cd enums-net
   ```
2. **Deploy to Ivy hosting**:
   ```bash
   ivy deploy
   ```
This will deploy your enum exploration application with a single command.

## Learn More

- Enums.NET GitHub repository: [github.com/TylerBrinkley/Enums.NET](https://github.com/TylerBrinkley/Enums.NET)
- Ivy Documentation: [docs.ivy.app](https://docs.ivy.app)