# QuestPDF

<img width="1914" height="922" alt="image" src="https://github.com/user-attachments/assets/dbcdae9c-b012-4f33-8cb9-4439eb6c8321" />

## One-Click Development Environment

[![Open in GitHub Codespaces](https://github.com/codespaces/badge.svg)](https://github.com/codespaces/new?hide_repo_select=true&ref=main&repo=Ivy-Interactive%2FIvy-Examples&machine=standardLinux32gb&devcontainer_path=.devcontainer%2Fquestpdf%2Fdevcontainer.json&location=EuropeWest)

Click the badge above to open Ivy Examples repository in GitHub Codespaces with:
- **.NET 9.0** SDK pre-installed
- **Ready-to-run** development environment
- **No local setup** required

## Created Using Ivy

Web application created using [Ivy-Framework](https://github.com/Ivy-Interactive/Ivy-Framework).

**Ivy** - The ultimate framework for building internal tools with LLM code generation by unifying front-end and back-end into a single C# codebase. With Ivy, you can build robust internal tools and dashboards using C# and AI assistance based on your existing database.

Ivy is a web framework for building interactive web applications using C# and .NET.

## Interactive PDF Generation Application

This example demonstrates PDF document generation capabilities using the [QuestPDF library](https://github.com/QuestPDF/QuestPDF) integrated with Ivy. QuestPDF is a modern .NET library for generating PDF documents with a fluent API and comprehensive layout engine.

**What This Application Does:**

This specific implementation creates a **PDF Generator** application that allows users to:

- **Edit Markdown Content**: Write and edit markdown-formatted content in a code editor with syntax highlighting
- **Configure PDF Settings**: Customize page size (A4, Letter), orientation (Portrait, Landscape), and margins (15, 30, 50)
- **Insert Markdown Snippets**: Quickly insert common markdown elements (Headings, Bullets, Numbered lists, Quotes, Checkboxes, Tables)
- **Real-Time Preview**: See a live preview of the generated PDF as you type and make changes
- **Download PDF**: Download the generated PDF document with a single click
- **Markdown Support**: Full markdown rendering including headings, lists, tables, links, checkboxes, and more

**Technical Implementation:**

- Uses QuestPDF's fluent API for PDF document generation
- Implements markdown parsing and rendering to PDF format
- Creates interactive UI with real-time preview using base64-encoded PDF data URLs
- Handles state management with reactive updates using Ivy's `UseEffect` hook
- Supports embedded resources for default content templates
- Uses QuestPDF's layout engine for proper page structure with headers and footers
- Implements responsive two-column layout (editor + preview)

**Key Features:**

- **Live Preview**: Real-time PDF preview updates automatically as you edit
- **Markdown Editor**: Full-featured code editor with markdown syntax highlighting
- **Flexible Configuration**: Customize page size, orientation, and margins
- **Quick Insert**: One-click insertion of common markdown elements
- **Professional Output**: Generated PDFs include headers, footers, and proper formatting
- **Clean UI**: Modern split-pane interface with clear visual hierarchy

## How to Run

1. **Prerequisites**: .NET 9.0 SDK
2. **Navigate to the example**:
   ```bash
   cd packages-demos/questpdf
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
   cd packages-demos/questpdf
   ```
2. **Deploy to Ivy hosting**:
   ```bash
   ivy deploy
   ```
This will deploy your PDF generator application with a single command.

## Learn More

- QuestPDF library overview: [github.com/QuestPDF/QuestPDF](https://github.com/QuestPDF/QuestPDF)
- Ivy Documentation: [docs.ivy.app](https://docs.ivy.app)
