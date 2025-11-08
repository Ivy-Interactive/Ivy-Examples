# FuzzySharp

<img width="1914" height="904" alt="image" src="https://github.com/user-attachments/assets/21b9a1ea-182d-40aa-af5e-89d2e008fe06" />

## One-Click Development Environment

[![Open in GitHub Codespaces](https://github.com/codespaces/badge.svg)](https://github.com/codespaces/new?hide_repo_select=true&ref=main&repo=Ivy-Interactive%2FIvy-Examples&machine=standardLinux32gb&devcontainer_path=.devcontainer%2Ffuzzysharp%2Fdevcontainer.json&location=EuropeWest)

Click the badge above to open Ivy Examples repository in GitHub Codespaces with:
- **.NET 9.0** SDK pre-installed
- **Ready-to-run** development environment
- **No local setup** required

## Created Using Ivy

Web application created using [Ivy-Framework](https://github.com/Ivy-Interactive/Ivy-Framework).

**Ivy** - The ultimate framework for building internal tools with LLM code generation by unifying front-end and back-end into a single C# codebase. With Ivy, you can build robust internal tools and dashboards using C# and AI assistance based on your existing database.

Ivy is a web framework for building interactive web applications using C# and .NET.

## Interactive Example For Fuzzy Text Search

This example demonstrates intelligent text search using the [FuzzySharp library](https://github.com/JakeBond/FuzzySharp) integrated with Ivy. FuzzySharp is a powerful .NET library that provides fuzzy string matching capabilities, allowing you to find similar strings even with typos, misspellings, or partial matches.

**What This Application Does:**

This specific implementation creates a **Fuzzy Search** application that allows users to:

- **Intelligent Search**: Find items even with typos, misspellings, or partial text
- **Real-time Results**: Get instant search results as you type
- **Similarity Scoring**: See percentage match scores for each result
- **Visual Results**: Results displayed as colorful badges with similarity percentages
- **Interactive Examples**: Built-in examples to demonstrate fuzzy search capabilities
- **Two-Panel Layout**: Search interface on the left, results on the right

**Technical Implementation:**

- Uses FuzzySharp's `ExtractTop` method for intelligent string matching
- Implements real-time search with reactive state management
- Creates responsive badge-based result display with similarity scores
- Handles dynamic result rendering with automatic empty field removal
- Supports fuzzy matching against a comprehensive dataset of fruits and products
- Provides interactive examples for common search scenarios

## How to Run

1. **Prerequisites**: .NET 9.0 SDK
2. **Navigate to the example**:
   ```bash
   cd fuzzysharp
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
   cd fuzzysharp
   ```
2. **Deploy to Ivy hosting**:
   ```bash
   ivy deploy
   ```
This will deploy your fuzzy search application with a single command.

## Learn More

- FuzzySharp for .NET overview: [github.com/JakeBond/FuzzySharp](https://github.com/JakeBond/FuzzySharp)
- Ivy Documentation: [docs.ivy.app](https://docs.ivy.app)