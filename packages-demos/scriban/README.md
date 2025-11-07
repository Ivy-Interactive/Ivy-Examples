# Scriban

<img width="1918" height="911" alt="image" src="https://github.com/user-attachments/assets/3b14301e-219e-4b16-8648-3a890f2e691e" />

## One-Click Development Environment

[![Open in GitHub Codespaces](https://github.com/codespaces/badge.svg)](https://github.com/codespaces/new?hide_repo_select=true&ref=main&repo=Ivy-Interactive%2FIvy-Examples&machine=standardLinux32gb&devcontainer_path=.devcontainer%2Fscriban%2Fdevcontainer.json&location=EuropeWest)

Click the badge above to open Ivy Examples repository in GitHub Codespaces with:
- **.NET 9.0** SDK pre-installed
- **Ready-to-run** development environment
- **No local setup** required

## Created Using Ivy

Web application created using [Ivy-Framework](https://github.com/Ivy-Interactive/Ivy-Framework).

**Ivy** - The ultimate framework for building internal tools with LLM code generation by unifying front-end and back-end into a single C# codebase. With Ivy, you can build robust internal tools and dashboards using C# and AI assistance based on your existing database.

Ivy is a web framework for building interactive web applications using C# and .NET.

## Interactive Example For Template Generation

This example demonstrates template rendering using the [Scriban library](https://github.com/scriban/scriban) integrated with Ivy. Scriban is a powerful, fast, and secure templating engine for .NET that supports Liquid-like syntax with advanced features.

**What This Application Does:**

This specific implementation creates a **Scriban Template Engine** application that allows users to:

- **Enter JSON Models**: Input data in JSON format that will be used as the model for template rendering
- **Create Scriban Templates**: Write Scriban templates using Liquid-like syntax with variables, loops, and filters
- **Generate Output**: Render templates with the provided model to generate formatted output
- **Edit Output**: After generation, users can edit the output directly in the text area
- **Real-time Validation**: Automatic validation of JSON syntax and template parsing errors
- **Interactive UI**: Split-panel layout with input controls on the left and template/output on the right

**Technical Implementation:**

- Uses Scriban's `Template.Parse()` and `Template.Render()` for template processing
- Supports JSON model parsing with automatic conversion to Scriban script objects
- Implements error handling for invalid JSON and template syntax errors
- Creates responsive card-based layout with horizontal split panels
- Handles state management for model, template, and output
- Supports Scriban features including:
  - Variable interpolation: `{{ model.name }}`
  - Filters: `{{ model.total | math.format "c" "en-US" }}`
  - Loops: `{{~ for item in model.items ~}}`
  - Array access: `{{ model.items[0].name }}`
  - Built-in math functions and formatting



## How to Run

1. **Prerequisites**: .NET 9.0 SDK
2. **Navigate to the example**:
   ```bash
   cd scriban
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
   cd scriban
   ```
2. **Deploy to Ivy hosting**:
   ```bash
   ivy deploy
   ```
This will deploy your Scriban template engine application with a single command.

## Learn More

- Scriban for .NET overview: [github.com/scriban/scriban](https://github.com/scriban/scriban)
- Scriban Documentation: [github.com/scriban/scriban/wiki](https://github.com/scriban/scriban/wiki)
- Ivy Documentation: [docs.ivy.app](https://docs.ivy.app)
