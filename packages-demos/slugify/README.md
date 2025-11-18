# Slugify Slug Generator App

<img width="1430" height="752" alt="Screenshot 2025-11-18 at 15 07 08" src="https://github.com/user-attachments/assets/40cba7dd-79d1-4259-922b-fa5c28575f85" />


## One-Click Development Environment

[![Open in GitHub Codespaces](https://github.com/codespaces/badge.svg)](https://github.com/codespaces/new?hide_repo_select=true&ref=main&repo=Ivy-Interactive%2FIvy-Examples&machine=standardLinux32gb&devcontainer_path=.devcontainer%2Fslugify%2Fdevcontainer.json&location=EuropeWest)

Click the badge above to open Ivy Examples repository in GitHub Codespaces with:
- **.NET 9.0** SDK pre-installed
- **Ready-to-run** development environment
- **No local setup** required

## Created Using Ivy

Web application created using [Ivy-Framework](https://github.com/Ivy-Interactive/Ivy-Framework).

**Ivy** - The ultimate framework for building internal tools with LLM code generation by unifying front-end and back-end into a single C# codebase. With Ivy, you can build robust internal tools and dashboards using C# and AI assistance based on your existing database.

Ivy is a web framework for building interactive web applications using C# and .NET.

**Interactive Example For Slug Creation**

This example demonstrates slug generation operations using the Slugify.Core library￼ integrated with Ivy. Slugify.Core is a free and open-source C# library that converts text into SEO-friendly, URL-safe slugs with configurable options.

**What This Application Does:**

This specific implementation creates a Slug Generator application that allows users to:
	•	Generate SEO-Friendly Slugs: Enter any text to create clean, hyphenated slugs suitable for URLs
	•	Customizable Options: 
	  •	Force lowercase
	  •	Trim whitespace
	  •	Collapse consecutive dashes
	  •	Collapse whitespace 
	•	Interactive UI: See instant slug results as you generate them
	•	Error Handling: Displays meaningful error messages if slug generation fails

**Technical Implementation:**	

  •	Uses Slugify.Core 5.1.1 for fast, memory-efficient slug generation
	•	Implements Ivy’s reactive UseState and event-driven UI controls
	•	Provides toggle/select inputs for all configurable slug options
	•	Generates slugs in real-time and updates the UI dynamically
	•	Uses expandable panels to display generated slugs clearly
	•	Handles invalid inputs and displays user-friendly error messages

## How to Run
1. **Prerequisites**: .NET 9.0 SDK
2. **Navigate to the example**:
   ```bash
   cd slugify
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
   cd slugify
   ```
2. **Deploy to Ivy hosting**:
   ```bash
   ivy deploy
   ```
This will deploy your HTML parser application with a single command.

## Learn More

- Slugify for .NET overview: (https://github.com/ctolkien/Slugify)
- Ivy Documentation: [docs.ivy.app](https://docs.ivy.app)
