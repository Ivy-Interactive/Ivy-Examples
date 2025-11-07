# DiffEngine 

<img width="1909" height="912" alt="image" src="https://github.com/user-attachments/assets/3c2e5cf2-7db3-4447-aa75-3e7d04f9cd4a" />

## One-Click Development Environment

[![Open in GitHub Codespaces](https://github.com/codespaces/badge.svg)](https://github.com/codespaces/new?hide_repo_select=true&ref=main&repo=Ivy-Interactive%2FIvy-Examples&machine=standardLinux32gb&devcontainer_path=.devcontainer%2Fdiffengine%2Fdevcontainer.json&location=EuropeWest)

Click the badge above to open Ivy Examples repository in GitHub Codespaces with:
- **.NET 9.0** SDK pre-installed
- **Ready-to-run** development environment
- **No local setup** required

## Created Using Ivy

Web application created using [Ivy-Framework](https://github.com/Ivy-Interactive/Ivy).

**Ivy** - The ultimate framework for building internal tools with LLM code generation by unifying front-end and back-end into a single C# codebase. With Ivy, you can build robust internal tools and dashboards using C# and AI assistance based on your existing database.

Ivy is a web framework for building interactive web applications using C# and .NET.

## Interactive Example For Diff Tool Integration

This example demonstrates how to launch and manage external diff tools using the [DiffEngine library](https://github.com/VerifyTests/DiffEngine) integrated with Ivy. DiffEngine automatically detects and launches installed diff tools on Windows, macOS, and Linux.

**What This Application Does:**

This specific implementation creates a **Diff Tool Launcher** application that allows users to:

- **Compare Text Side-by-Side**: Enter text in two code editors and launch a diff tool to compare them
- **Compare File Contents**: Provide two file paths, which are safely copied to temp files before comparison
- **Extension Selection**: Choose file extensions (txt, json) via dropdown for proper syntax highlighting
- **Kill Active Diffs**: Close the last launched diff tool session with a single click
- **Tabbed Interface**: Organized UI with separate tabs for Text Diff and File Paths Diff workflows
- **Automatic Tool Detection**: Works with WinMerge, VS Code, KDiff3, Meld, BeyondCompare, and 15+ other tools

**Technical Implementation:**

- Uses DiffEngine's `DiffRunner.LaunchAsync()` for automatic tool detection and launching
- Generates temporary files with proper extensions for diff tool compatibility
- Implements `BuildServerDetector` to detect CI/CD environments
- Creates side-by-side card layouts with CodeInput for text comparison
- Handles cleanup with `DiffRunner.Kill()` to close specific diff tool instances
- Single C# view (`Apps/DiffEngineApp.cs`) built with Ivy UI primitives

## How to Run

1. **Prerequisites**: 
   - .NET 9.0 SDK
   - A diff tool installed (e.g. [WinMerge](https://winmerge.org/), VS Code, Meld, KDiff3). DiffEngine will detect what you have.

2. **Navigate to the example**:
   ```bash
   cd diffengine
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
   cd diffengine
   ```
2. **Deploy to Ivy hosting**:
   ```bash
   ivy deploy
   ```
This will deploy your diff tool launcher application with a single command.

## Learn More

- DiffEngine GitHub repository: [github.com/VerifyTests/DiffEngine](https://github.com/VerifyTests/DiffEngine)
- Ivy Documentation: [docs.ivy.app](https://docs.ivy.app)
