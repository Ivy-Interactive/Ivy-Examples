# IbanNet

<img width="1915" height="909" alt="image" src="https://github.com/user-attachments/assets/8ed0a63c-47bf-46be-88c3-4283f5b8a2ea" />

## One-Click Development Environment

[![Open in GitHub Codespaces](https://github.com/codespaces/badge.svg)](https://github.com/codespaces/new?hide_repo_select=true&ref=main&repo=Ivy-Interactive%2FIvy-Examples&machine=standardLinux32gb&devcontainer_path=.devcontainer%2Fibannet%2Fdevcontainer.json&location=EuropeWest)

Click the badge above to open Ivy Examples repository in GitHub Codespaces with:
- **.NET 9.0** SDK pre-installed
- **Ready-to-run** development environment
- **No local setup** required

## Created Using Ivy

Web application created using [Ivy-Framework](https://github.com/Ivy-Interactive/Ivy-Framework).

**Ivy** - The ultimate framework for building internal tools with LLM code generation by unifying front-end and back-end into a single C# codebase. With Ivy, you can build robust internal tools and dashboards using C# and AI assistance based on your existing database.

Ivy is a web framework for building interactive web applications using C# and .NET.

## Interactive Example For IBAN Operations

This example demonstrates IBAN (International Bank Account Number) operations using the [IbanNet library](https://github.com/skwasjer/IbanNet) integrated with Ivy. IbanNet is a comprehensive .NET library for validating, parsing, and generating IBANs according to ISO 13616 standards.

**What This Application Does:**

This specific implementation creates an **IBAN Management** application that allows users to:

- **Generate Sample IBANs**: Create valid IBANs for any of 126+ supported countries
- **Validate IBANs**: Check IBAN structure, length, and checksum validation
- **Parse IBAN Details**: Extract structured information from IBANs including country, bank ID, and branch ID
- **Country Selection**: Searchable dropdown with all supported countries
- **Structured Display**: Use ToDetails() extension for clean, interactive data presentation
- **Copy Functionality**: Copy obfuscated IBANs to clipboard for security
- **Real-time Validation**: Instant feedback on IBAN validity with detailed breakdown

**Technical Implementation:**

- Uses IbanNet's `IbanValidator` for comprehensive IBAN validation
- Implements `IbanParser` for extracting structured IBAN components
- Leverages `IbanGenerator` for creating valid sample IBANs
- Supports 126+ countries with automatic country name resolution
- Creates responsive split-panel layout with generation and validation
- Implements ToDetails() extension for structured data display
- Handles form submission with toast notifications and state management
- Provides obfuscated IBAN display for security-conscious operations

## How to Run

1. **Prerequisites**: .NET 9.0 SDK
2. **Navigate to the example**:
   ```bash
   cd ibannet
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
   cd ibannet
   ```
2. **Deploy to Ivy hosting**:
   ```bash
   ivy deploy
   ```
This will deploy your IBAN management application with a single command.

## Learn More

- IbanNet for .NET overview: [github.com/skwasjer/IbanNet](https://github.com/skwasjer/IbanNet)
- Ivy Documentation: [docs.ivy.app](https://docs.ivy.app)