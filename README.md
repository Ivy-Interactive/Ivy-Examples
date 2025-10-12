# Ivy Examples

[![Stars](https://img.shields.io/github/stars/Ivy-Interactive/Ivy-Examples?style=flat-square)](https://github.com/Ivy-Interactive/Ivy-Examples/stargazers)
[![Forks](https://img.shields.io/github/forks/Ivy-Interactive/Ivy-Examples?style=flat-square)](https://github.com/Ivy-Interactive/Ivy-Examples/network/members)
[![License](https://img.shields.io/github/license/Ivy-Interactive/Ivy-Examples?style=flat-square)](LICENSE)
[![Contributors](https://img.shields.io/github/contributors/Ivy-Interactive/Ivy-Examples?style=flat-square)](https://github.com/Ivy-Interactive/Ivy-Examples/graphs/contributors)

[Documentation](https://docs.ivy.app) | [Samples](https://samples.ivy.app) | [Current Sprint](https://github.com/orgs/Ivy-Interactive/projects/8) | [Roadmap](https://github.com/orgs/Ivy-Interactive/projects/7) | [Examples](https://github.com/Ivy-Interactive/Ivy-Examples)

A comprehensive collection of real-world examples showcasing the power and versatility of the [Ivy Framework](https://github.com/Ivy-Interactive/Ivy-Framework) integrated with popular .NET packages and libraries.

**Ivy** is the ultimate framework for building internal tools with LLM code generation by unifying front-end and back-end into a single C# codebase. With Ivy, you can build robust internal tools and dashboards using C# and AI assistance based on your existing database.

## What is Ivy?

Ivy is a revolutionary web framework that allows you to build interactive web applications using only C# and .NET. No JavaScript, no separate frontend framework - just pure C# from database to UI.

### Key Features

- **Full-Stack C#**: Write your entire application in C#
- **LLM Integration**: Build applications with AI code generation
- **Hot Reload**: See changes instantly during development
- **Web-First**: Deploy anywhere with modern web standards
- **Rich UI**: Create beautiful, interactive user interfaces
- **Responsive**: Works seamlessly on desktop and mobile

## Examples Collection

This repository contains **50+ working examples** that demonstrate how to integrate Ivy with popular .NET packages and libraries. Each example is a complete, runnable application showcasing real-world usage patterns.

### Featured Examples

| Category | Examples | Description |
|----------|----------|-------------|
| **UI & Visualization** | `barcodelib`, `qrcoder`, `questpdf` | Generate barcodes, QR codes, and PDFs |
| **Data Processing** | `closedxml`, `miniexcel`, `csvhelper` | Excel manipulation and CSV processing |
| **Text & Search** | `fuzzysharp`, `simmetrics-net`, `HtmlAgilityPack` | String matching and HTML parsing |
| **Web & APIs** | `restsharp`, `openai`, `github` | HTTP clients and API integrations |
| **Date & Time** | `nodatime`, `fluentdatetime`, `cronos` | Advanced date/time handling |
| **Data Generation** | `bogus`, `fastmember`, `humanizer` | Mock data and object manipulation |
| **Security & Auth** | `jwt`, `stripe-net`, `ibannet` | Authentication and financial processing |
| **AI & ML** | `microsoft-semantickernel`, `ollamasharp` | AI integration and machine learning |
| **Document Processing** | `aspose-words`, `aspose-ocr`, `aspose-barcode` | Document manipulation and OCR |

### Complete Applications

Some examples go beyond simple demonstrations and showcase complete business applications:

- **`crm-vc`** - A full CRM system with 40+ components
- **`rental-back-office`** - Property rental management system
- **`dnsclient`** - Network diagnostics tool with multiple forms

## Quick Start

### Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) or later
- Any code editor (Visual Studio, VS Code, JetBrains Rider)

### Running an Example

1. **Clone the repository**:

   ```bash
   git clone https://github.com/Ivy-Interactive/Ivy-Examples.git
   cd Ivy-Examples
   ```

2. **Choose an example** (e.g., `helloworld`):

   ```bash
   cd helloworld
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

That's it! You now have a running Ivy application.

## Docker Support

Most examples include Docker support for easy deployment:

```bash
cd <example-name>
docker build -t ivy-example .
docker run -p 5010:5010 ivy-example
```

## One-Click Development Environment

Many examples support GitHub Codespaces for instant development:

[![Open in GitHub Codespaces](https://github.com/codespaces/badge.svg)](https://github.com/codespaces/new?hide_repo_select=true&ref=main&repo=Ivy-Interactive%2FIvy-Examples&machine=standardLinux32gb&location=EuropeWest)

Click the badge above to open any example in a fully configured development environment with:

- **.NET 9.0** SDK pre-installed
- **Ready-to-run** development environment
- **No local setup** required

## Deployment

Deploy any example to Ivy's hosting platform with a single command:

```bash
cd <example-name>
ivy deploy
```

This will deploy your application with automatic SSL, custom domains, and global CDN.

## Example Structure

Each example follows a consistent structure:

```txt
example-name/
├── Apps/              # Ivy application components
├── Connections/       # Database connections (if needed)
├── Models/           # Data models (if needed)
├── Services/         # Business logic (if needed)
├── Program.cs        # Application entry point
├── GlobalUsings.cs   # Global using statements
├── *.csproj         # Project file
├── Dockerfile       # Docker configuration
└── README.md        # Example-specific documentation
```

## Contributing

We welcome contributions! Whether you want to:

- Fix bugs in existing examples
- Add new examples showcasing different packages
- Improve documentation
- Enhance UI/UX of examples

Please read our [Contributing Guidelines](CONTRIBUTING.md) to get started.

### Example Ideas

Looking for inspiration? Here are some examples we'd love to see:

- **Machine Learning**: ML.NET, TensorFlow.NET
- **Gaming**: MonoGame, Unity integrations
- **IoT**: Device communication and monitoring
- **Blockchain**: Ethereum, Bitcoin integrations

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Star History

If you find this repository helpful, please consider giving it a star!

---
