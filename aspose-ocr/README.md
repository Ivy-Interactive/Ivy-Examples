# Aspose.OCR Image-to-Text

<img width="1908" height="899" alt="image" src="https://github.com/user-attachments/assets/29aa2aef-513a-4f7c-81f1-8e5597503b77" />

## One-Click Development Environment

[![Open in GitHub Codespaces](https://github.com/codespaces/badge.svg)](https://github.com/codespaces/new?hide_repo_select=true&ref=main&repo=Ivy-Interactive%2FIvy-Examples&machine=standardLinux32gb&devcontainer_path=.devcontainer%2Faspose-ocr%2Fdevcontainer.json&location=EuropeWest)

Click the badge above to open Ivy Examples repository in GitHub Codespaces with:
- **.NET 9.0** SDK pre-installed
- **Ready-to-run** development environment
- **No local setup** required

## Created Using Ivy

Web application created using [Ivy-Framework](https://github.com/Ivy-Interactive/Ivy-Framework).

**Ivy** - The ultimate framework for building internal tools with LLM code generation by unifying front-end and back-end into a single C# codebase. With Ivy, you can build robust internal tools and dashboards using C# and AI assistance based on your existing database.

Ivy is a web framework for building interactive web applications using C# and .NET.

## Interactive Example For OCR (Image To Text)

This example showcases extracting text from images using Aspose.OCR with a simple two-panel UI: upload on the left, recognized text on the right.

**What This Application Does:**

- **Upload Image**: Select an image file and run OCR
- **Text Extraction**: Recognize text from the uploaded image
- **Monospaced Viewer**: See output in a clean, plain-text viewer
- **Validation**: Basic size check with friendly error messages

**Technical Implementation:**

- Uses Aspose.OCR `AsposeOcr` with `OcrInput(InputType.SingleImage)`
- Adds uploaded image stream via `source.Add(stream)` and calls `Recognize`
- Shows the first `RecognitionResult.RecognitionText` in a `CodeInput`
- Single C# view (`Apps/ImageToTextApp.cs`) built with Ivy UI primitives

## How to Run

1. **Prerequisites**: .NET 9.0 SDK
2. **Navigate to the example**:
   ```bash
   cd aspose-ocr
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
   cd aspose-ocr
   ```
2. **Deploy to Ivy hosting**:
   ```bash
   ivy deploy
   ```
This will deploy your OCR application with a single command.

## Licensing & Trial Limitations

**Aspose.OCR for .NET** is a commercial library that can be used in trial (evaluation) mode without a license.

### Trial Mode Restrictions

When running without a license, the following limitations apply:

- If the recognized image contains **more than 300 characters**, only the **first 300 characters** are recognized.
- If the recognized image contains **less than 300 characters**, only the **first 60%** are recognized.

For more licensing options (metered, embedded resources, stream), see the [official licensing documentation](https://docs.aspose.com/ocr/net/licensing/).

## Learn More

- Aspose.OCR for .NET overview: [products.aspose.com/ocr/net](https://products.aspose.com/ocr/net/)
- Aspose.OCR Licensing: [docs.aspose.com/ocr/net/licensing](https://docs.aspose.com/ocr/net/licensing/)
- Ivy Documentation: [docs.ivy.app](https://docs.ivy.app)
