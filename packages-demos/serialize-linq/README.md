# Serialize.Linq

<img width="976" height="831" alt="image" src="https://github.com/user-attachments/assets/2e6760c0-58cd-4d1c-8721-dff85c56bfab" />

## One-Click Development Environment

[![Open in GitHub Codespaces](https://github.com/codespaces/badge.svg)](https://github.com/codespaces/new?hide_repo_select=true&ref=main&repo=Ivy-Interactive%2FIvy-Examples&machine=standardLinux32gb&devcontainer_path=.devcontainer%2Fserialize-linq%2Fdevcontainer.json&location=EuropeWest)

Click the badge above to open Ivy Examples repository in GitHub Codespaces with:
- **.NET 9.0** SDK pre-installed
- **Ready-to-run** development environment
- **No local setup** required

## Created Using Ivy

Web application created using [Ivy-Framework](https://github.com/Ivy-Interactive/Ivy-Framework).

**Ivy** - The ultimate framework for building internal tools with LLM code generation by unifying front-end and back-end into a single C# codebase. With Ivy, you can build robust internal tools and dashboards using C# and AI assistance based on your existing database.

Ivy is a web framework for building interactive web applications using C# and .NET.

## Interactive Example For LINQ Expression Serialization

This example demonstrates LINQ expression serialization and deserialization using the [Serialize.Linq library](https://github.com/esskar/Serialize.Linq) integrated with Ivy. Serialize.Linq allows you to serialize LINQ expressions to JSON and deserialize them back, enabling expression storage, transmission, and dynamic evaluation.

**What This Application Does:**

This specific implementation creates a **LINQ Expression Serialization** application that allows users to:

- **Create Comparison Expressions**: Enter two numeric values and select a comparison operator (=, <, <=, >, >=, !=)
- **Serialize to JSON**: Convert LINQ expressions to JSON format with proper indentation and syntax highlighting
- **Deserialize Expressions**: Restore serialized expressions from JSON and evaluate them
- **Evaluate Comparisons**: Test expressions with different values and see the comparison results
- **Visual Feedback**: Display results using color-coded callouts (Success/Error) and formatted JSON code blocks
- **Interactive UI**: Split-panel layout with input controls on the left and results on the right

**Technical Implementation:**

- Uses Serialize.Linq's `ExpressionSerializer` with `JsonSerializer` for expression serialization
- Generates formatted JSON output with proper indentation using `JsonDocument` and `JsonSerializerOptions`
- Implements expression compilation and evaluation for dynamic comparison testing
- Creates responsive card-based layout with visual feedback using Callout components
- Handles expression state management with proper null checking and error handling
- Supports multiple comparison operators with dynamic expression tree generation
- Displays serialized JSON with syntax highlighting, line numbers, and copy functionality

## How to Run

1. **Prerequisites**: .NET 9.0 SDK
2. **Navigate to the example**:
   ```bash
   cd serialize-linq
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
   cd serialize-linq
   ```
2. **Deploy to Ivy hosting**:
   ```bash
   ivy deploy
   ```
This will deploy your LINQ expression serialization application with a single command.

## Learn More

- Serialize.Linq for .NET overview: [github.com/esskar/Serialize.Linq](https://github.com/esskar/Serialize.Linq)
- Ivy Documentation: [docs.ivy.app](https://docs.ivy.app)
