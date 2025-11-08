# QR Code Generator

<img width="1914" height="913" alt="image" src="https://github.com/user-attachments/assets/1df75bc9-a5e0-438b-b785-5abc2388b191" />

## One-Click Development Environment

[![Open in GitHub Codespaces](https://github.com/codespaces/badge.svg)](https://github.com/codespaces/new?hide_repo_select=true&ref=main&repo=Ivy-Interactive%2FIvy-Examples&machine=standardLinux32gb&devcontainer_path=.devcontainer%2Fqrcoder%2Fdevcontainer.json&location=EuropeWest)

Click the badge above to open Ivy Examples repository in GitHub Codespaces with:
- **.NET 9.0** SDK pre-installed
- **Ready-to-run** development environment
- **No local setup** required

## Created Using Ivy

Web application created using [Ivy-Framework](https://github.com/Ivy-Interactive/Ivy-Framework).

**Ivy** - The ultimate framework for building internal tools with LLM code generation by unifying front-end and back-end into a single C# codebase. With Ivy, you can build robust internal tools and dashboards using C# and AI assistance based on your existing database.

Ivy is a web framework for building interactive web applications using C# and .NET.

## Interactive Example For QR code generation

This example demonstrates QR code generation using the [QRCoder library](https://github.com/codebude/QRCoder) integrated with Ivy. QRCoder is a pure C# Open Source.

**What This Application Does:**

This specific implementation creates a **Profile Creator** application that allows users to:

- **Create User Profiles**: Fill out a form with personal information (name, email, phone, LinkedIn, GitHub)
- **Generate vCard QR Codes**: Automatically creates contact cards (vCard format) encoded in QR codes
- **Share Contact Information**: Users can scan the generated QR code to automatically add the contact to their phone
- **Real-time Form Validation**: Validates email format and URL patterns for LinkedIn/GitHub profiles
- **Interactive UI**: Split-panel layout with form on the left and QR code preview on the right

**Technical Implementation:**

- Uses QRCoder's vCard payload generator for structured contact data
- Generates Base64-encoded PNG images for web display
- Implements form validation with custom rules
- Creates resizable panel layout for optimal user experience
- Handles form submission with loading states and error display

## How to Run

1. **Prerequisites**: .NET 9.0 SDK
2. **Navigate to the example**:
   ```bash
   cd qrcoder
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
   cd qrcoder
   ```
2. **Deploy to Ivy hosting**:
   ```bash
   ivy deploy
   ```
This will deploy your QR code generation application with a single command.

## For more details, see the [Ivy Documentation](https://docs.ivy.app)