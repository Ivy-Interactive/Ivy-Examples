# AWS Constructs

<img width="1915" height="909" alt="image" src="https://github.com/user-attachments/assets/a057e99e-ba5e-416d-b2b0-514e4f280cbb" />

## One-Click Development Environment

[![Open in GitHub Codespaces](https://github.com/codespaces/badge.svg)](https://github.com/codespaces/new?hide_repo_select=true&ref=main&repo=Ivy-Interactive%2FIvy-Examples&machine=standardLinux32gb&devcontainer_path=.devcontainer%2Fconstructs%2Fdevcontainer.json&location=EuropeWest)

Click the badge above to open Ivy Examples repository in GitHub Codespaces with:
- **.NET 9.0** SDK pre-installed
- **Ready-to-run** development environment
- **No local setup** required

## Created Using Ivy

Web application created using [Ivy-Framework](https://github.com/Ivy-Interactive/Ivy-Framework).

**Ivy** - The ultimate framework for building internal tools with LLM code generation by unifying front-end and back-end into a single C# codebase. With Ivy, you can build robust internal tools and dashboards using C# and AI assistance based on your existing database.

Ivy is a web framework for building interactive web applications using C# and .NET.

## Interactive Example For AWS Constructs

This example demonstrates building composable configuration models using the [AWS Constructs library](https://github.com/aws/constructs) integrated with Ivy. Constructs are classes that define "pieces of system state" and can be composed together to form higher-level building blocks.

**What This Application Does:**

- **Build Construct Trees**: Create hierarchical construct structures with parent-child relationships
- **Interactive Tree Navigation**: Filter and view subtrees by specifying parent paths
- **Dynamic Node Addition**: Add new child constructs at runtime with custom IDs
- **ASCII Tree Visualization**: View the construct tree structure in a clean ASCII format
- **Tree Management**: Reset to canonical structure or expand/collapse large trees

**Technical Implementation:**

- Uses AWS Constructs .NET package with jsii bridge to Node.js runtime
- Implements `RootConstruct` as the root node with no parent
- Creates canonical tree structure with Demo, ChildA, ChildB, and Nested constructs
- Provides interactive UI for tree manipulation and visualization
- Single C# view (`Apps/ConstructsApp.cs`) built with Ivy UI primitives

## How to Run

1. **Prerequisites**: .NET 9.0 SDK
2. **Navigate to the example**:
   ```bash
   cd constructs
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
   cd constructs
   ```
2. **Deploy to Ivy hosting**:
   ```bash
   ivy deploy
   ```
This will deploy your AWS Constructs interactive demo with a single command.

## Learn More

- AWS Constructs GitHub repository: [github.com/aws/constructs](https://github.com/aws/constructs)
- Ivy Documentation: [docs.ivy.app](https://docs.ivy.app)