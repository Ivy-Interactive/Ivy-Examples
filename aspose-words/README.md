# Aspose.Words

<img width="1911" height="909" alt="image" src="https://github.com/user-attachments/assets/f6f6a15d-2a45-4ffd-84d2-8c4d1c26789f" />

<img width="1917" height="907" alt="image" src="https://github.com/user-attachments/assets/fc83bd04-4c3c-46b1-97c2-db341cdd139d" />

## One-Click Development Environment

[![Open in GitHub Codespaces](https://github.com/codespaces/badge.svg)](https://github.com/codespaces/new?hide_repo_select=true&ref=main&repo=Ivy-Interactive%2FIvy-Examples&machine=standardLinux32gb&devcontainer_path=.devcontainer%2Faspose-words%2Fdevcontainer.json&location=EuropeWest)

Click the badge above to open Ivy Examples repository in GitHub Codespaces with:
- **.NET 9.0** SDK pre-installed
- **Ready-to-run** development environment
- **No local setup** required

## Created Using Ivy

Web application created using [Ivy-Framework](https://github.com/Ivy-Interactive/Ivy-Framework).

**Ivy** - The ultimate framework for building internal tools with LLM code generation by unifying front-end and back-end into a single C# codebase. With Ivy, you can build robust internal tools and dashboards using C# and AI assistance based on your existing database.

Ivy is a web framework for building interactive web applications using C# and .NET.

## Interactive Example For Word Document Generation

This example demonstrates professional Word document generation using [Aspose.Words for .NET](https://products.aspose.com/words/net/) integrated with Ivy. Aspose.Words is a powerful document processing library that enables creation, manipulation, and conversion of Word documents without requiring Microsoft Word.

**What This Application Does:**

This specific implementation creates a **Document Generator** application that allows users to:

- **Generate Professional Documents**: Choose from pre-built templates (letters, reports, invoices, form letters)
- **Real-time Document Creation**: Generate Word documents on-demand with loading indicators
- **Download DOCX Files**: Export generated documents in Microsoft Word format
- **Rich Formatting Support**: Create documents with headers, footers, tables, and styled text
- **Template-Based Approach**: Select from multiple document types optimized for different business needs
- **Custom Text to DOCX**: Instantly convert any custom text into a Word document with a simple text input interface
**Technical Implementation:**

- Uses Aspose.Words DocumentBuilder API for document construction
- Implements state management for tracking generated documents per template
- Creates formatted tables with data, headers, and styling
- Generates documents with professional headers and footers
- Supports mail merge placeholders for form letters
- Provides type-safe file downloads with proper MIME types

## How to Run

1. **Prerequisites**: .NET 9.0 SDK
2. **Navigate to the example**:
   ```bash
   cd aspose-words
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
   cd aspose-words
   ```
2. **Deploy to Ivy hosting**:
   ```bash
   ivy deploy
   ```
This will deploy your Word document generation application with a single command.

## Learn More

- **Aspose.Words for .NET overview**: [products.aspose.com/words/net](https://products.aspose.com/words/net)
- **Ivy Documentation**: [docs.ivy.app](https://docs.ivy.app)
