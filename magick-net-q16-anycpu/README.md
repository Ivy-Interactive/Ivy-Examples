# Magick.NET-Q16-AnyCPU 

<img width="1913" height="908" alt="image" src="https://github.com/user-attachments/assets/7a62ffd8-d731-4cde-ad48-5ecc0d3ef1d8" />

## One-Click Development Environment

[![Open in GitHub Codespaces](https://github.com/codespaces/badge.svg)](https://github.com/codespaces/new?hide_repo_select=true&ref=main&repo=Ivy-Interactive%2FIvy-Examples&machine=standardLinux32gb&devcontainer_path=.devcontainer%2Fmagick-net-q16-anycpu%2Fdevcontainer.json&location=EuropeWest)

Click the badge above to open Ivy Examples repository in GitHub Codespaces with:
- **.NET 9.0** SDK pre-installed
- **Ready-to-run** development environment
- **No local setup** required

## Created Using Ivy

Web application created using [Ivy-Framework](https://github.com/Ivy-Interactive/Ivy-Framework).

**Ivy** - The ultimate framework for building internal tools with LLM code generation by unifying front-end and back-end into a single C# codebase. With Ivy, you can build robust internal tools and dashboards using C# and AI assistance based on your existing database.

Ivy is a web framework for building interactive web applications using C# and .NET.

## Interactive Example For Image Processing

This example demonstrates comprehensive image processing capabilities using [Magick.NET-Q16-AnyCPU](https://github.com/dlemstra/Magick.NET) integrated with Ivy. Magick.NET is a powerful .NET wrapper for the ImageMagick image processing library, providing extensive image manipulation features.

**What This Application Does:**

This implementation creates a **Digital Image Alchemy** studio that allows users to:

- **Upload Images**: Support for all common image formats (JPEG, PNG, GIF, BMP, WebP, and more)
- **Apply Image Effects**: Transform images with 16+ powerful effects:
  - **Resize**: Custom dimensions with aspect ratio preservation
  - **Blur**: Adjustable blur radius for soft focus effects
  - **Sharpen**: Enhance image clarity and detail
  - **Brightness**: Adjust image brightness levels
  - **Contrast**: Control image contrast
  - **Saturation**: Modify color saturation
  - **Hue Shift**: Change color tones and hues
  - **Rotate**: Rotate images by any angle
  - **Flip**: Flip images horizontally or vertically

- **Format Conversion**: Export processed images in multiple formats:
  - PNG (lossless)
  - JPEG (with quality control)
  - WebP (modern format with quality control)
  - BMP (bitmap)
  - GIF (animated images)

- **Quality Control**: Adjust compression quality for JPEG and WebP formats
- **Real-time Preview**: See processed images before downloading
- **Image Information**: View original and processed image dimensions, formats, and file sizes
- **Error Handling**: Client-side toast notifications for validation and processing errors

**Technical Implementation:**

- Uses Magick.NET's `MagickImage` class for powerful image processing
- Implements file upload handling with automatic format detection
- Creates downloadable processed images with format conversion
- Supports multiple output formats with quality settings
- Provides real-time image preview using data URIs
- Handles errors with user-friendly toast notifications
- Responsive split-panel layout with controls and preview

## How to Run

1. **Prerequisites**: .NET 9.0 SDK
2. **Navigate to the example**:
   ```bash
   cd magick-net-q16-anycpu
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
   cd magick-net-q16-anycpu
   ```
2. **Deploy to Ivy hosting**:
   ```bash
   ivy deploy
   ```
This will deploy your image processing studio with a single command.

## Learn More

- Magick.NET overview: [github.com/dlemstra/Magick.NET](https://github.com/dlemstra/Magick.NET)
- ImageMagick official site: [imagemagick.org](https://imagemagick.org)
- Ivy Documentation: [docs.ivy.app](https://docs.ivy.app)
