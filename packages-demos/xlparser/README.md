# XLParser

<img width="1605" height="913" alt="image" src="https://github.com/user-attachments/assets/07510f2e-d5fc-4ce9-8182-d9cca43992ae" />

## One-Click Development Environment

[![Open in GitHub Codespaces](https://github.com/codespaces/badge.svg)](https://github.com/codespaces/new?hide_repo_select=true&ref=main&repo=Ivy-Interactive%2FIvy-Examples&machine=standardLinux32gb&devcontainer_path=.devcontainer%2Fxlparser%2Fdevcontainer.json&location=EuropeWest)

Click the badge above to open this XLParser demo in GitHub Codespaces with:
- **.NET 9.0** SDK pre-installed
- **XLParser tooling** ready to go
- **Zero local setup** required

## Created Using Ivy

Web application created with [Ivy-Framework](https://github.com/Ivy-Interactive/Ivy-Framework).

**Ivy** unifies UI and backend into one C# codebase, letting you build internal tools and dashboards quickly with LLM-assisted workflows.

## Interactive Example Using XLParser

This demo showcases Excel formula parsing and analysis powered by [XLParser](https://github.com/spreadsheetlab/XLParser) within an Ivy application.

**What this application demonstrates:**

- **Formula parsing**: Enter any Excel formula and parse it into its component tokens and structure.
- **Token visualization**: View parsed tokens, hierarchical display showing the formula's parse tree.
- **Token analysis**: Select any token to view its detailed properties and metadata, including:
  - Token type and classification
  - Binary/unary operation detection
  - Function identification (built-in, external, named)
  - Range and reference detection
  - Operator and parentheses analysis
- **Example formulas**: Quick access to common Excel formulas like SUM, IF, VLOOKUP, INDEX/MATCH for testing.
- **Real-time parsing**: Instant feedback on formula validity with clear error messages.

**Technical highlights:**

- Uses `ExcelFormulaParser` from XLParser library for robust formula parsing.
- Demonstrates parse tree traversal and token extraction.
- Shows hierarchical token display with depth-based indentation.
- Filters token metadata to show only relevant properties.
- Presents a split-pane layout with input controls and analysis display.

## How to Run

1. **Prerequisites**: .NET 9.0 SDK
2. **Navigate to the example**:
   ```bash
   cd packages-demos/xlparser
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
   cd packages-demos/xlparser
   ```
2. **Deploy to Ivy hosting**:
   ```bash
   ivy deploy
   ```
   This publishes the XLParser demo with one command.

## Learn More

- XLParser documentation: [github.com/spreadsheetlab/XLParser](https://github.com/spreadsheetlab/XLParser)
- Ivy documentation: [docs.ivy.app](https://docs.ivy.app)
