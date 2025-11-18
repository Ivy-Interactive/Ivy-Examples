# ZString

<img width="1919" height="908" alt="image" src="https://github.com/user-attachments/assets/0c892226-8f1b-4f59-bd54-17514d36c08f" />

## One-Click Development Environment

[![Open in GitHub Codespaces](https://github.com/codespaces/badge.svg)](https://github.com/codespaces/new?hide_repo_select=true&ref=main&repo=Ivy-Interactive%2FIvy-Examples&machine=standardLinux32gb&devcontainer_path=.devcontainer%2Fzstring%2Fdevcontainer.json&location=EuropeWest)

Click the badge above to open Ivy Examples repository in GitHub Codespaces with:
- **.NET 9.0** SDK pre-installed
- **Ready-to-run** development environment
- **No local setup** required

## Created Using Ivy

Web application created using [Ivy-Framework](https://github.com/Ivy-Interactive/Ivy-Framework).

**Ivy** - The ultimate framework for building internal tools with LLM code generation by unifying front-end and back-end into a single C# codebase. With Ivy, you can build robust internal tools and dashboards using C# and AI assistance based on your existing database.

Ivy is a web framework for building interactive web applications using C# and .NET.

## Interactive Example For High-Performance String Operations

This example demonstrates high-performance string operations using the [ZString library](https://github.com/Cysharp/ZString) integrated with Ivy. ZString is a zero-allocation string formatting library for .NET that provides fast, memory-efficient string operations.

**What This Application Does:**

This specific implementation creates a **ZString Operations Demo** that allows users to:

- **Explore String Operations**: Select from 10 pre-configured ZString operations via dropdown
- **View Function Code**: See the exact C# code for each operation with syntax highlighting
- **See Immediate Results**: View the execution result for each operation instantly
- **Copy Code and Results**: Copy function code or results with one click
- **Learn ZString API**: Understand how to use ZString methods in your own projects

**Available Operations:**

1. **Concat** - Concatenate multiple values into a single string
2. **Format** - Format strings with placeholders and numeric formatting
3. **Join** - Join array elements with a separator
4. **CreateStringBuilder** - Use Utf16ValueStringBuilder for complex string building
5. **Prepared Format** - Use prepared format templates for repeated formatting
6. **AppendJoin** - Join and append array elements with a separator
7. **AppendFormat Multiple** - Format with multiple arguments
8. **AppendLine** - Append lines with automatic line terminators
9. **Append With Format** - Append values with numeric format strings (F4, C, X)
10. **TryCopyTo** - Copy string builder buffer to a Span<char>

**Technical Implementation:**

- Uses ZString's zero-allocation string formatting methods
- Demonstrates `Utf16ValueStringBuilder` API with various methods
- Shows both static methods (`ZString.Concat`, `ZString.Format`, `ZString.Join`) and instance methods
- Implements prepared format templates for performance-critical scenarios
- Provides copyable code examples for easy integration
- Uses Ivy's reactive state management for instant result updates
- Displays code with C# syntax highlighting and results in text format

## How to Run

1. **Prerequisites**: .NET 9.0 SDK
2. **Navigate to the example**:
   ```bash
   cd packages-demos/zstring
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
   cd packages-demos/zstring
   ```
2. **Deploy to Ivy hosting**:
   ```bash
   ivy deploy
   ```
This will deploy your ZString demo application with a single command.

## Learn More

- ZString for .NET overview: [github.com/Cysharp/ZString](https://github.com/Cysharp/ZString)
- Ivy Documentation: [docs.ivy.app](https://docs.ivy.app)
