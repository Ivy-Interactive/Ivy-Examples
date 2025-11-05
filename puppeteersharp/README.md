# PuppeteerSharp

<img width="1919" height="911" alt="image" src="https://github.com/user-attachments/assets/f42c2a73-7a48-4271-8f2a-cd5932240683" />

## One-Click Development Environment

[![Open in GitHub Codespaces](https://github.com/codespaces/badge.svg)](https://github.com/codespaces/new?hide_repo_select=true&ref=main&repo=Ivy-Interactive%2FIvy-Examples&machine=standardLinux32gb&devcontainer_path=.devcontainer%2Fpuppeteersharp%2Fdevcontainer.json&location=EuropeWest)

Click the badge above to open Ivy Examples repository in GitHub Codespaces with:
- **.NET 9.0** SDK pre-installed
- **Ready-to-run** development environment
- **No local setup** required

## Created Using Ivy

Web application created using [Ivy-Framework](https://github.com/Ivy-Interactive/Ivy-Framework).

**Ivy** - The ultimate framework for building internal tools with LLM code generation by unifying front-end and back-end into a single C# codebase. With Ivy, you can build robust internal tools and dashboards using C# and AI assistance based on your existing database.

Ivy is a web framework for building interactive web applications using C# and .NET.

## Interactive Example For Website Rendering

This example demonstrates website rendering and screenshot generation using the [PuppeteerSharp library](https://github.com/hardkoded/puppeteer-sharp) integrated with Ivy. PuppeteerSharp is a .NET port of the popular Puppeteer library that provides a high-level API to control headless Chrome or Chromium browsers.

**What This Application Does:**

This specific implementation creates a **Website Renderer** application that allows users to:

- **Render Websites**: Enter any HTTP/HTTPS URL and render the website in a headless browser
- **Generate Screenshots**: Capture full-page screenshots of rendered websites
- **Generate PDFs**: Convert rendered websites into PDF documents (A4 format)
- **Preview Results**: View screenshots directly in the application interface
- **Download Files**: Save screenshots or PDFs with a convenient dropdown menu
- **Error Handling**: Receive toast notifications for validation errors and rendering failures
- **Loading States**: Visual feedback with loading indicators on the render button

**Technical Implementation:**

- Uses PuppeteerSharp's `Puppeteer.LaunchAsync()` to launch headless Chrome browser
- Navigates to URLs with proper redirect handling using `NavigationOptions` and `WaitUntilNavigation.Networkidle0`
- Generates screenshots using `ScreenshotDataAsync()` with full-page capture
- Generates PDFs using `PdfDataAsync()` with A4 format and background printing enabled
- Implements dual endpoint system (`/view/{id}` for preview, `/download/{id}` for downloads)
- Uses in-memory file storage (`MemoryFileStore`) for temporary file management
- Handles URL validation and provides user-friendly error messages
- Creates responsive split-panel layout with controls on the left and preview on the right
- Supports toast notifications for user feedback

## How to Run

1. **Prerequisites**: .NET 9.0 SDK
2. **Navigate to the example**:
   ```bash
   cd puppeteersharp
   ```
3. **Restore dependencies**:
   ```bash
   dotnet restore
   ```
4. **Run the application**:
   ```bash
   dotnet watch
   ```
   Note: On first run, PuppeteerSharp will automatically download the required Chromium browser. This may take a few minutes depending on your internet connection.
5. **Open your browser** to the URL shown in the terminal (typically `http://localhost:5010`)

## How to Deploy

Deploy this example to Ivy's hosting platform:

1. **Navigate to the example**:
   ```bash
   cd puppeteersharp
   ```
2. **Deploy to Ivy hosting**:
   ```bash
   ivy deploy
   ```
This will deploy your website renderer application with a single command.

## Learn More

- PuppeteerSharp for .NET overview: [github.com/hardkoded/puppeteer-sharp](https://github.com/hardkoded/puppeteer-sharp)
- Ivy Documentation: [docs.ivy.app](https://docs.ivy.app)
