# Superpower

<img width="1350" height="920" alt="image" src="https://github.com/user-attachments/assets/0799430b-adfa-4f73-9256-31baf4246ed7" />

<img width="1348" height="911" alt="image" src="https://github.com/user-attachments/assets/a4f8f78b-4bed-4600-81e6-3addf13ebf50" />

<img width="1348" height="965" alt="image" src="https://github.com/user-attachments/assets/3848108a-fdc2-4b64-b9c8-5fabe99d8faa" />

## One-Click Development Environment

[![Open in GitHub Codespaces](https://github.com/codespaces/badge.svg)](https://github.com/codespaces/new?hide_repo_select=true&ref=main&repo=Ivy-Interactive%2FIvy-Examples&machine=standardLinux32gb&devcontainer_path=.devcontainer%2Fsuperpower%2Fdevcontainer.json&location=EuropeWest)

Launch the repository in GitHub Codespaces pre-configured with:
- **.NET 9.0** SDK
- **Superpower** sample dependencies restored
- **Ivy CLI** tooling pre-installed
- **No local setup** required

## Created Using Ivy

Web application built with [Ivy Framework](https://github.com/Ivy-Interactive/Ivy-Framework) and [Superpower](https://github.com/datalust/superpower).

**Ivy** unifies front‑end and back‑end C# into a single codebase and augments the developer workflow with AI-assisted code generation. You can compose interactive apps, dashboards, and internal tools entirely in C# with stateful UI components.

**Superpower** is a C# parser-combinator library from Datalust that simplifies building high-quality parsers capable of rich error reporting.

## Interactive Parser Showcase

This demo highlights three independent parsers implemented with Superpower and surfaced through Ivy UI components:

- **JSON Parser**
  - Full JSON grammar support (objects, arrays, strings, numbers, booleans, null)
  - Indented parsed output displayed with `Code` widgets
  - Expandable curated samples and JSON-aware editor with copy support
- **Arithmetic Expression Parser**
  - Tokenizer plus expression tree evaluation for `+ - * /`
  - Respect for operator precedence and parenthesis grouping
  - Helpful examples and guided copy explaining evaluation output
- **Date/Time Parser**
  - Parses ISO-8601 formats with or without delimiters
  - Presents formatted date, day-of-week, and day-of-year metadata
  - Inline helper text guides users through valid formats

All three parsers share a consistent layout: tabs for navigation, side-by-side cards splitting input and results, expandable example blocks, and context-aware messaging in idle and success states.

## Key UI Features

- `Layout.Tabs`-based navigation between parser demos
- Cards with descriptive copy explaining parser behavior and usage hints
- Expandable sections containing formatted, copyable code snippets
- `CodeInput`, `Code`, and `Button` widgets with loading states for fluent UX
- Clear toast-style messaging for success, errors, and empty states

## Parser Implementation

Core parsing logic lives in the [`Helpers`](./Helpers) folder:

- `JsonParser.cs` defines the Superpower tokenizer and parser that builds a JSON AST and delivers detailed diagnostics for invalid input.
- `ArithmeticExpressionTokenizer.cs`, `ArithmeticExpressionToken.cs`, and `ArithmeticExpressionParser.cs` work together to convert math strings into token streams, construct expression trees, and compile delegates for evaluation.
- `DateTimeTextParser.cs` uses Superpower text parsers to recognise multiple ISO-8601 styles and return `DateTime` values with precise error positioning.

Each Ivy view (`JsonParserView`, `IntegerCalculatorView`, `DateTimeParserView`) wires these helpers into the UI by invoking the parsers, handling success/error states, and rendering results in the cards shown in the app.


## How to Run Locally

1. **Install prerequisites:** .NET 9.0 SDK  
2. **Navigate to the example directory:**
   ```bash
   cd packages-demos/superpower
   ```
3. **Restore dependencies (first run only):**
   ```bash
   dotnet restore
   ```
4. **Start the development server:**
   ```bash
   dotnet watch
   ```
5. Open the browser at the URL shown in the console (usually `http://localhost:5010`).

## How to Deploy

1. **Navigate to this demo**:
   ```bash
   cd packages-demos/superpower
   ```
2. **Deploy to Ivy hosting**:
   ```bash
   ivy deploy
   ```

## Learn More

- Superpower parser-combinator library: [github.com/datalust/superpower](https://github.com/datalust/superpower)
- Ivy Framework documentation: [docs.ivy.app](https://docs.ivy.app)
