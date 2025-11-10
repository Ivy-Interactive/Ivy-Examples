# SmartFormat.NET

<img width="1917" height="913" alt="image" src="https://github.com/user-attachments/assets/a7308f0e-a579-4712-b25e-7b8fd2d6c5c0" />

## One-Click Development Environment

[![Open in GitHub Codespaces](https://github.com/codespaces/badge.svg)](https://github.com/codespaces/new?hide_repo_select=true&ref=main&repo=Ivy-Interactive%2FIvy-Examples&machine=standardLinux32gb&devcontainer_path=.devcontainer%2Fsmartformat-net%2Fdevcontainer.json&location=EuropeWest)

Click the badge above to open this SmartFormat.NET demo in GitHub Codespaces with:
- **.NET 9.0** SDK pre-installed
- **SmartFormat.NET tooling** ready to go
- **Zero local setup** required

## Created Using Ivy

Web application created with [Ivy-Framework](https://github.com/Ivy-Interactive/Ivy-Framework).

**Ivy** unifies UI and backend into one C# codebase, letting you build internal tools and dashboards quickly with LLM-assisted workflows.

## Interactive Example Using SmartFormat.NET

This demo showcases dynamic string formatting powered by [SmartFormat.NET](https://github.com/axuno/SmartFormat) within an Ivy application.

**What this application demonstrates:**

- **Rich templating UI**: Compose format templates, preview results, and tweak inputs in real time.
- **Named and positional placeholders**: Showcases nested properties, list formatting, conditional sections, and pluralization.
- **Reusable templates**: Save, load, and manage templates for common business messages and notifications.
- **Validation and errors**: Highlights SmartFormat parsing/formatting errors with helpful messages.
- **Live preview panel**: Instantly renders formatted output as you edit templates or data.

**Technical highlights:**

- Uses `SmartFormatter` with Ivy state containers for reactive formatting.
- Demonstrates custom formatters and source extensions.
- Shows how to serialize templates for persistence and sharing.
- Includes sample datasets for invoices, shipping notifications, and user onboarding flows.
- Presents a split-pane layout with editor controls and preview display.

## How to Run

1. **Prerequisites**: .NET 9.0 SDK
2. **Navigate to the example**:
   ```bash
   cd packages-demos/smartformat-net
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
   cd packages-demos/smartformat-net
   ```
2. **Deploy to Ivy hosting**:
   ```bash
   ivy deploy
   ```
   This publishes the SmartFormat.NET demo with one command.

## Learn More

- SmartFormat.NET documentation: [github.com/axuno/SmartFormat](https://github.com/axuno/SmartFormat)
- Ivy documentation: [docs.ivy.app](https://docs.ivy.app)
