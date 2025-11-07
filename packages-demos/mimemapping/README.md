# MimeMapping

<img width="1918" height="912" alt="image" src="https://github.com/user-attachments/assets/6ad697c5-b36f-41f6-9bc6-a18b7d13bbea" />

<img width="1563" height="909" alt="image" src="https://github.com/user-attachments/assets/469ac1e2-b09a-4611-954c-ec8cdf0dbe0c" />

<img width="1919" height="910" alt="image" src="https://github.com/user-attachments/assets/392a29f3-7208-4577-b75b-f98f8b88b7cd" />

## One-Click Development Environment

[![Open in GitHub Codespaces](https://github.com/codespaces/badge.svg)](https://github.com/codespaces/new?hide_repo_select=true&ref=main&repo=Ivy-Interactive%2FIvy-Examples&machine=standardLinux32gb&devcontainer_path=.devcontainer%2Fmimemapping%2Fdevcontainer.json&location=EuropeWest)

Click the badge above to open Ivy Examples repository in GitHub Codespaces with:
- **.NET 9.0** SDK pre-installed
- **Ready-to-run** development environment
- **No local setup** required

## Created Using Ivy

Web application created using [Ivy-Framework](https://github.com/Ivy-Interactive/Ivy-Framework).

**Ivy** - The ultimate framework for building internal tools with LLM code generation by unifying front-end and back-end into a single C# codebase. With Ivy, you can build robust internal tools and dashboards using C# and AI assistance based on your existing database.

Ivy is a web framework for building interactive web applications using C# and .NET.

## Interactive Example For MIME Type Detection

This example demonstrates MIME type detection and file extension mapping using the [MimeMapping library](https://github.com/zone117x/MimeMapping) integrated with Ivy. MimeMapping is a lightweight, fast, and comprehensive library for MIME type detection from file extensions.

**What This Application Does:**

This specific implementation creates a **MIME Type Discovery** application that allows users to:

- **Detect MIME Types**: Upload files or enter file names to instantly detect their MIME types
- **Browse Types**: Search through 1000+ supported MIME types with real-time filtering and pagination
- **Reverse Lookup**: Find all file extensions associated with a specific MIME type
- **Copy to Clipboard**: Easily copy detected MIME types and file extensions
- **Input Validation**: Validates MIME type format and handles unknown types gracefully
- **Interactive UI**: Split-panel layout with input controls and result display

**Technical Implementation:**

- Uses MimeMapping's `MimeUtility.GetMimeMapping()` for instant MIME type detection
- Implements `MimeUtility.GetExtensions()` for reverse MIME type to extension lookup
- Provides access to `MimeUtility.TypeMap` for browsing all supported types
- Handles file uploads with automatic MIME type detection
- Implements real-time search with pagination (8 items per page)
- Uses `UseEffect` hook for automatic page reset on search query changes
- Creates responsive split-panel layouts with Cards and Tables
- Supports copy-to-clipboard functionality for detected values

## How to Run

1. **Prerequisites**: .NET 9.0 SDK
2. **Navigate to the example**:
   ```bash
   cd mimemapping
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
   cd mimemapping
   ```
2. **Deploy to Ivy hosting**:
   ```bash
   ivy deploy
   ```
This will deploy your MIME type discovery application with a single command.

## Learn More

- MimeMapping library overview: [github.com/zone117x/MimeMapping](https://github.com/zone117x/MimeMapping)
- Ivy Documentation: [docs.ivy.app](https://docs.ivy.app)
