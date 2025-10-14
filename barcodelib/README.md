# BarcodeLib 

<img width="1913" height="905" alt="image" src="https://github.com/user-attachments/assets/84f10da6-c31b-4040-ad86-4ce72b769ee1" />

## One-Click Development Environment

[![Open in GitHub Codespaces](https://github.com/codespaces/badge.svg)](https://github.com/codespaces/new?hide_repo_select=true&ref=main&repo=Ivy-Interactive%2FIvy-Examples&machine=standardLinux32gb&devcontainer_path=.devcontainer%2Fbarcodelib%2Fdevcontainer.json&location=EuropeWest)

Click the badge above to open Ivy Examples repository in GitHub Codespaces with:
- **.NET 9.0** SDK pre-installed
- **Ready-to-run** development environment
- **No local setup** required

## Created Using Ivy

Web application created using [Ivy-Framework](https://github.com/Ivy-Interactive/Ivy-Framework).

**Ivy** - The ultimate framework for building internal tools with LLM code generation by unifying front-end and back-end into a single C# codebase. With Ivy, you can build robust internal tools and dashboards using C# and AI assistance based on your existing database.

Ivy is a web framework for building interactive web applications using C# and .NET.

## Interactive Example For Barcode Generation

This example demonstrates barcode generation using the [BarcodeLib library](https://github.com/barnhill/barcodelib) integrated with Ivy. BarcodeLib is a pure C# library for generating various barcode symbologies.

**What This Application Does:**

This specific implementation creates a **Barcode Generator** application that allows users to:

- **Generate Barcodes**: Create barcodes from input text across multiple symbologies (UPC-A, EAN-13, Code128, Code39, Interleaved 2 of 5, ITF-14)
- **Toggle Labels**: Enable or disable barcode labels for better readability
- **Live Preview**: See a crisp, non-scaled preview of the generated barcode
- **Download PNG**: One-click download of the generated barcode image
- **Interactive UI**: Split-panel layout with controls on the left and barcode preview on the right

**Technical Implementation:**

- Uses BarcodeLib's `Barcode` class with `BarcodeStandard.Type` enumeration
- Generates Base64-encoded PNG images for web display using SkiaSharp
- Implements dropdown selection for different barcode types
- Creates toggle button for label inclusion
- Handles barcode generation with fixed dimensions (300×120 pixels)
- Single C# view (`Apps/BarcodeLibApp.cs`) built with Ivy UI primitives

## How to Run

1. **Prerequisites**: .NET 9.0 SDK
2. **Navigate to the example**:
   ```bash
   cd barcodelib
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
   cd barcodelib
   ```
2. **Deploy to Ivy hosting**:
   ```bash
   ivy deploy
   ```
This will deploy your barcode generation application with a single command.

## Learn More

- BarcodeLib GitHub repository: [github.com/barnhill/barcodelib](https://github.com/barnhill/barcodelib)
- Ivy Documentation: [docs.ivy.app](https://docs.ivy.app)