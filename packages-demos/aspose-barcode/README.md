# Aspose.BarCode Generator

<img width="1913" height="907" alt="image" src="https://github.com/user-attachments/assets/a96685ee-bedd-4e35-b37c-8528a3260d19" />

## One-Click Development Environment

[![Open in GitHub Codespaces](https://github.com/codespaces/badge.svg)](https://github.com/codespaces/new?hide_repo_select=true&ref=main&repo=Ivy-Interactive%2FIvy-Examples&machine=standardLinux32gb&devcontainer_path=.devcontainer%2Faspose-barcode%2Fdevcontainer.json&location=EuropeWest)

Click the badge above to open Ivy Examples repository in GitHub Codespaces with:
- **.NET 9.0** SDK pre-installed
- **Ready-to-run** development environment
- **No local setup** required

## Created Using Ivy

Web application created using [Ivy-Framework](https://github.com/Ivy-Interactive/Ivy-Framework).

**Ivy** - The ultimate framework for building internal tools with LLM code generation by unifying front-end and back-end into a single C# codebase. With Ivy, you can build robust internal tools and dashboards using C# and AI assistance based on your existing database.

Ivy is a web framework for building interactive web applications using C# and .NET.

## Interactive Example For Barcode Generation

This example showcases generating multiple barcode symbologies (QR, PDF417, Code128, DataMatrix, DotCode, ISBN) with adjustable sizes and instant preview.

**What This Application Does:**

- **Generate Barcodes**: Create barcodes from input text across popular symbologies
- **Adjust Size**: Choose from Small, Medium, Large presets (tuned X-Dimension)
- **Live Preview**: See a crisp, non-scaled preview
- **Download PNG**: One-click download of the generated barcode image

**Technical Implementation:**

- Uses Aspose.BarCode `BarcodeGenerator` with `SymbologyEncodeType`
- Sets `Parameters.Barcode.XDimension.Pixels` based on size presets for sharp output
- Generates Base64 PNG for the in-app preview and byte stream for download
- Single C# view (`Apps/BarcodeApp.cs`) built with Ivy UI primitives

## How to Run

1. **Prerequisites**: .NET 9.0 SDK
2. **Navigate to the example**:
   ```bash
   cd aspose-barcode
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
   cd aspose-barcode
   ```
2. **Deploy to Ivy hosting**:
   ```bash
   ivy deploy
   ```
This will deploy your QR code generation application with a single command.

## Learn More

- Aspose.BarCode for .NET overview: [products.aspose.com/barcode/net](https://products.aspose.com/barcode/net/)
- Ivy Documentation: [docs.ivy.app](https://docs.ivy.app)