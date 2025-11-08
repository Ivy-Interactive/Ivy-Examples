# OllamaSharp

<img width="1918" height="916" alt="image" src="https://github.com/user-attachments/assets/cc07f9c5-3856-4310-a0d8-e89dc82d8cd2" />

<img width="1918" height="913" alt="image" src="https://github.com/user-attachments/assets/2f46f3af-3bcf-4883-9883-3e467ebc0b4b" />

<img width="1919" height="920" alt="image" src="https://github.com/user-attachments/assets/2afb8cad-3cb9-4d22-b0d4-8d13d3e3c47f" />


## One-Click Development Environment

[![Open in GitHub Codespaces](https://github.com/codespaces/badge.svg)](https://github.com/codespaces/new?hide_repo_select=true&ref=main&repo=Ivy-Interactive%2FIvy-Examples&machine=standardLinux32gb&devcontainer_path=.devcontainer%2Follamasharp%2Fdevcontainer.json&location=EuropeWest)

Click the badge above to open Ivy Examples repository in GitHub Codespaces with:
- **.NET 9.0** SDK pre-installed
- **Ready-to-run** development environment
- **No local setup** required

## Created Using Ivy

Web application created using [Ivy-Framework](https://github.com/Ivy-Interactive/Ivy-Framework).

**Ivy** - The ultimate framework for building internal tools with LLM code generation by unifying front-end and back-end into a single C# codebase. With Ivy, you can build robust internal tools and dashboards using C# and AI assistance based on your existing database.

Ivy is a web framework for building interactive web applications using C# and .NET.

## Interactive Example For Ollama Integration

This example demonstrates how to integrate [Ollama](https://ollama.com/) with an Ivy application using the [OllamaSharp](https://github.com/awaescher/OllamaSharp) library. OllamaSharp is a .NET client library for interacting with Ollama API, enabling you to run and interact with Large Language Models (LLMs) locally.

**What This Application Does:**

This specific implementation creates a **Chat Interface** application that allows users to:

- **Browse Models**: View and select from locally available Ollama models with automatic model discovery
- **Interactive Chat**: Real-time chat interface with streaming responses from selected LLM models
- **Model Management**: Switch between different models seamlessly with blade-based navigation
- **Connection Status**: Automatic detection and validation of Ollama service availability
- **Error Handling**: User-friendly error messages and toast notifications for connection issues
- **Streaming Responses**: Receive and display model responses in real-time as they're generated

**Technical Implementation:**

- Uses OllamaSharp's `OllamaApiClient` for robust API communication
- Implements blade-based navigation for smooth model selection and chat experience
- Handles streaming chat responses with async enumerable patterns
- Creates responsive UI with filtered list views for model browsing
- Supports automatic model loading and connection status checking
- Implements state management for chat messages and model selection

## Prerequisites

Before running this application, you need to set up Ollama:

### 1. Install Ollama

Download and install Ollama from [https://ollama.com/download](https://ollama.com/download) for your platform:

- **Windows**: Download the installer and run it
- **macOS**: Use Homebrew (`brew install ollama`) or download from the website
- **Linux**: Run `curl -fsSL https://ollama.com/install.sh | sh`

### 2. Download Models

After installing Ollama, download at least one model to use with the application:

```bash
# Download a popular model (this may take a while depending on model size)
ollama pull llama2

# Or try a smaller model for testing
ollama pull phi3

# List other available models
ollama list
```

### 3. Start Ollama Service

Ensure Ollama is running locally. By default, Ollama runs on `http://localhost:11434`.

```bash
# Start Ollama (usually runs automatically after installation)
ollama serve
```

## How to Run

1. **Prerequisites**: .NET 9.0 SDK and Ollama installed locally
2. **Navigate to the example**:
   ```bash
   cd ollamasharp
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
   cd ollamasharp
   ```
2. **Deploy to Ivy hosting**:
   ```bash
   ivy deploy
   ```
This will deploy your Ollama chat application with a single command.

## Troubleshooting

### "Ollama API is not running" Error

If you see this error:
1. Ensure Ollama is installed and running: `ollama serve`
2. Check if Ollama is accessible: `curl http://localhost:11434`
3. Verify you have at least one model downloaded: `ollama list`

### No Models Available

If no models appear in the list:
1. Download a model: `ollama pull llama2`
2. Verify models are installed: `ollama list`
3. Restart the application and wait for automatic model loading, or manually refresh

### Connection Issues

If you're unable to connect to Ollama:
1. Verify Ollama is running: Check if `ollama serve` is active
2. Check the default URL: The application connects to `http://localhost:11434` by default
3. Review firewall settings: Ensure localhost connections are allowed

## Learn More

- Ollama overview: [ollama.com](https://ollama.com)
- OllamaSharp library: [github.com/awaescher/OllamaSharp](https://github.com/awaescher/OllamaSharp)
- Ivy Documentation: [docs.ivy.app](https://docs.ivy.app)
