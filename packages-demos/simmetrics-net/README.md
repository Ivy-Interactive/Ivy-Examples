# SimMetrics.Net

<img width="1917" height="908" alt="image" src="https://github.com/user-attachments/assets/d1508c0d-59f9-4e2f-9b24-e8ebd970720b" />

## One-Click Development Environment

[![Open in GitHub Codespaces](https://github.com/codespaces/badge.svg)](https://github.com/codespaces/new?hide_repo_select=true&ref=main&repo=Ivy-Interactive%2FIvy-Examples&machine=standardLinux32gb&devcontainer_path=.devcontainer%2Fsimmetrics-net%2Fdevcontainer.json&location=EuropeWest)

Click the badge above to open this example in GitHub Codespaces with:
- **.NET 9.0** SDK pre-installed
- **Ready-to-run** Ivy environment
- **No local setup** required

## Created With Ivy

Web application built using [Ivy-Framework](https://github.com/Ivy-Interactive/Ivy-Framework).

**Ivy** is the internal tools framework that merges UI and backend logic in a single C# project, letting you ship full-stack apps with the help of AI-assisted workflows.

## Interactive Example: String Similarity Playground

This demo showcases the [SimMetrics.Net](https://github.com/StefH/SimMetrics.Net) library inside an Ivy application. SimMetrics.Net provides a comprehensive collection of string metrics for measuring similarity and distance.

**What this application offers:**

- **Configurable Metric Selection** – choose from edit, token, q-gram, block, and vector based algorithms (Levenshtein, Jaro-Winkler, Cosine, Jaccard, and more).
- **Live Similarity Scoring** – type a name and instantly see similarity percentages for a set of randomly generated names.
- **Detailed Metric Insights** – view short and long descriptions for each metric to understand how it works.
- **Two-Panel Layout** – inputs on the left, results and guidance on the right for a clear workflow.
- **Validation Feedback** – inline validation keeps the results in sync when fields are empty or invalid.

**Technical highlights:**

- Uses Ivy `UseState` and `UseEffect` hooks to recompute similarity when the input name or metric changes.
- Renders tabular results with formatted percentage scores using Ivy table builders.
- Leverages SimMetrics.Net `AbstractStringMetric` implementations through a factory dictionary.
- Random data provided by `Bogus` for realistic sample names.
- Built entirely in a single C# view (`Apps/SimMetricsNetApp.cs`).

## How to Run Locally

1. **Prerequisites**: .NET 9.0 SDK
2. **Navigate to the example**:
   ```bash
   cd packages-demos/simmetrics-net
   ```
3. **Restore dependencies**:
   ```bash
   dotnet restore
   ```
4. **Run the app with hot reload**:
   ```bash
   dotnet watch
   ```
5. **Open your browser** to the URL printed in the console (typically `http://localhost:5010`).

## How to Deploy

Deploy this example to Ivy hosting:

1. **Navigate to the example**:
   ```bash
   cd packages-demos/simmetrics-net
   ```
2. **Deploy in one command**:
   ```bash
   ivy deploy
   ```

## Learn More

- SimMetrics.Net GitHub repository: [github.com/StefH/SimMetrics.Net](https://github.com/StefH/SimMetrics.Net)
- Ivy documentation: [docs.ivy.app](https://docs.ivy.app)