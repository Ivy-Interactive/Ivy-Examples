# Humanizer

<img width="1915" height="908" alt="image" src="https://github.com/user-attachments/assets/52e53613-3419-4c99-b37c-53babfa41c98" />

## One-Click Development Environment

[![Open in GitHub Codespaces](https://github.com/codespaces/badge.svg)](https://github.com/codespaces/new?hide_repo_select=true&ref=main&repo=Ivy-Interactive%2FIvy-Examples&machine=standardLinux32gb&devcontainer_path=.devcontainer%2Fhumanizer%2Fdevcontainer.json&location=EuropeWest)

Click the badge above to open Ivy Examples repository in GitHub Codespaces with:
- **.NET 9.0** SDK pre-installed
- **Ready-to-run** development environment
- **No local setup** required

## Created Using Ivy

Web application created using [Ivy-Framework](https://github.com/Ivy-Interactive/Ivy-Framework).

**Ivy** - The ultimate framework for building internal tools with LLM code generation by unifying front-end and back-end into a single C# codebase. With Ivy, you can build robust internal tools and dashboards using C# and AI assistance based on your existing database.

Ivy is a web framework for building interactive web applications using C# and .NET.

## Interactive Example For Text Transformation

This example demonstrates text transformation using the [Humanizer library](https://github.com/Humanizr/Humanizer) integrated with Ivy. Humanizer is a powerful .NET library that provides a set of extension methods to transform strings, enums, dates, and times into a more human-readable format.

**What This Application Does:**

This specific implementation creates a **Text Humanizer** application that allows users to:

- **Transform Text**: Convert text using various humanization techniques
- **Multiple Formats**: Support for PascalCase, camelCase, kebab-case, underscore_case, and more
- **Smart Truncation**: Intelligent text truncation with customizable length
- **Real-time Results**: Instant transformation with history tracking
- **Interactive UI**: Split-panel layout with input controls on the left and results on the right
- **Code Formatting**: Results displayed in code blocks for easy copying

**Available Transformations:**

- **Humanize**: Convert to sentence case, title case, all caps, or lowercase
- **Truncate**: Smart text truncation with length control
- **Pascalize**: Convert to PascalCase (e.g., "hello world" → "HelloWorld")
- **Camelize**: Convert to camelCase (e.g., "hello world" → "helloWorld")
- **Underscore**: Convert to snake_case (e.g., "hello world" → "hello_world")
- **Kebaberize**: Convert to kebab-case (e.g., "hello world" → "hello-world")

**Technical Implementation:**

- Uses Humanizer's extension methods for robust text processing
- Implements reactive UI with state management
- Creates responsive two-panel layout with input and results sections
- Handles form validation and user interaction
- Supports real-time transformation with history tracking
- Provides code-formatted output for easy copying


## How to Run

1. **Prerequisites**: .NET 9.0 SDK
2. **Navigate to the example**:
   ```bash
   cd humanizer
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
   cd humanizer
   ```
2. **Deploy to Ivy hosting**:
   ```bash
   ivy deploy
   ```
This will deploy your text humanizer application with a single command.

## Learn More

- Humanizer for .NET overview: [github.com/Humanizr/Humanizer](https://github.com/Humanizr/Humanizer)
- Ivy Documentation: [docs.ivy.app](https://docs.ivy.app)