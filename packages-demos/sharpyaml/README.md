# SharpYaml

<img width="1569" height="916" alt="image" src="https://github.com/user-attachments/assets/ff57a2eb-22db-4c2d-b42e-72dbdf83d3af" />

## One-Click Development Environment

[![Open in GitHub Codespaces](https://github.com/codespaces/badge.svg)](https://github.com/codespaces/new?hide_repo_select=true&ref=main&repo=Ivy-Interactive%2FIvy-Examples&machine=standardLinux32gb&devcontainer_path=.devcontainer%2Fsharpyaml%2Fdevcontainer.json&location=EuropeWest)

Open the project instantly in GitHub Codespaces with:
- **.NET 9.0** SDK pre-installed
- **Ready-to-run** development environment
- **No local setup** required

## Created Using Ivy

Web application created using [Ivy-Framework](https://github.com/Ivy-Interactive/Ivy-Framework).

**Ivy** brings front-end and back-end development together in a single C# codebase—perfect for rapidly building internal tools and data apps with LLM assistance.

## Interactive JSON → YAML Converter

This example demonstrates how to convert JSON payloads into YAML using the [SharpYaml](https://github.com/xoofx/SharpYaml) library inside an Ivy application.

**What You Can Do:**
- **Edit JSON Input**: Work inside a syntax-highlighted editor with copy support and sample data.
- **Convert with One Click**: Generate YAML instantly via the _Convert_ button.
- **Review YAML Output**: Copy the result, keeping the viewer disabled until a successful conversion occurs.
- **Handle Errors Gracefully**: Inline callouts surface JSON parsing and serialization issues before YAML is shown.
- **Responsive Layout**: Header, dual cards, and footer provide an approachable, documentation-ready experience.

## How to Run

1. **Prerequisites**: .NET 9.0 SDK
2. **Navigate to this demo**:
   ```bash
   cd packages-demos/sharpyaml
   ```
3. **Restore dependencies**:
   ```bash
   dotnet restore
   ```
4. **Run the application**:
   ```bash
   dotnet watch
   ```
5. **Open your browser** to the URL printed in the terminal (for example `http://localhost:5010`).

## How to Deploy

1. **Navigate to this demo**:
   ```bash
   cd packages-demos/sharpyaml
   ```
2. **Deploy to Ivy hosting**:
   ```bash
   ivy deploy
   ```

## Learn More
- SharpYaml library: <https://github.com/xoofx/SharpYaml>
- Ivy documentation: <https://docs.ivy.app>