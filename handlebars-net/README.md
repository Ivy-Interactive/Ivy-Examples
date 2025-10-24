# Handlebars.Net

<img width="1918" height="906" alt="image" src="https://github.com/user-attachments/assets/c84895a5-4a11-423d-8035-97403f271407" />

## One-Click Development Environment

[![Open in GitHub Codespaces](https://github.com/codespaces/badge.svg)](https://github.com/codespaces/new?hide_repo_select=true&ref=main&repo=Ivy-Interactive%2FIvy-Examples&machine=standardLinux32gb&devcontainer_path=.devcontainer%2Fhandlebars-net%2Fdevcontainer.json&location=EuropeWest)

Click the badge above to open Ivy Examples repository in GitHub Codespaces with:
- **.NET 9.0** SDK pre-installed
- **Ready-to-run** development environment
- **No local setup** required

## Created Using Ivy

Web application created using [Ivy-Framework](https://github.com/Ivy-Interactive/Ivy-Framework).

**Ivy** - The ultimate framework for building internal tools with LLM code generation by unifying front-end and back-end into a single C# codebase. With Ivy, you can build robust internal tools and dashboards using C# and AI assistance based on your existing database.

Ivy is a web framework for building interactive web applications using C# and .NET.

## Interactive Handlebars Template Engine

This example demonstrates HTML template generation using the [Handlebars.Net library](https://github.com/Handlebars-Net/Handlebars.Net) integrated with Ivy. Handlebars.Net is a blazing-fast .NET implementation of the Handlebars templating engine that compiles templates directly to IL bytecode.

**What This Application Does:**

This specific implementation creates a **Handlebars Template Engine** application that allows users to:

- **Write Handlebars Templates**: Create HTML templates with Handlebars syntax for data binding
- **Provide JSON Data**: Input data as JSON to populate template variables
- **Live Preview**: See real-time HTML output as you type
- **Template Features**: Use variables, loops, conditionals, and helpers
- **Interactive UI**: Tabbed interface with template editor, data input, and result preview
- **Error Handling**: Graceful error display for invalid templates or data
- **Syntax Highlighting**: Code editors with HTML and JSON syntax support

**Technical Implementation:**

- Uses Handlebars.Net's `Handlebars.Compile()` for template compilation
- Implements real-time template rendering with JSON data binding
- Creates responsive tabbed interface with template and data editors
- Handles template compilation errors with user-friendly error messages
- Supports Handlebars features including:
  - Variable binding: `{{variable}}`
  - Loops: `{{#each items}}...{{/each}}`
  - Conditionals: `{{#if condition}}...{{/if}}`
  - Nested data access: `{{user.name}}`
  - Helper functions and custom logic

**Handlebars Template Features:**

- **Data Binding**: Simple variable substitution with `{{variable}}`
- **Loops**: Iterate over arrays with `{{#each}}` blocks
- **Conditionals**: Show/hide content with `{{#if}}` statements
- **Nested Objects**: Access object properties with dot notation
- **Safe Output**: Automatic HTML escaping for security
- **Performance**: Templates compiled to IL bytecode for maximum speed

## How to Run

1. **Prerequisites**: .NET 9.0 SDK
2. **Navigate to the example**:
   ```bash
   cd handlebars-net
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
   cd handlebars-net
   ```
2. **Deploy to Ivy hosting**:
   ```bash
   ivy deploy
   ```
This will deploy your Handlebars template engine with a single command.

## Learn More

- Handlebars.Net Documentation: [github.com/Handlebars-Net/Handlebars.Net](https://github.com/Handlebars-Net/Handlebars.Net)
- Ivy Documentation: [docs.ivy.app](https://docs.ivy.app)
- Handlebars.js Guide: [handlebarsjs.com/guide](https://handlebarsjs.com/guide)
