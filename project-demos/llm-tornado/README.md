# LlmTornado

## Description

LlmTornado Examples is a web application demonstrating the [LlmTornado](https://llmtornado.ai) library capabilities for interacting with LLM models. It features multiple chat interfaces including simple streaming chat and AI agents with function calling/tools support.

## One-Click Development Environment

[![Open in GitHub Codespaces](https://github.com/codespaces/badge.svg)](https://github.com/codespaces/new?hide_repo_select=true&ref=main&repo=Ivy-Interactive%2FIvy-Examples&machine=standardLinux32gb&devcontainer_path=.devcontainer%2Fllm-tornado%2Fdevcontainer.json&location=EuropeWest)

Click the badge above to open Ivy Examples repository in GitHub Codespaces with:

- **.NET 10.0** SDK pre-installed
- **Ready-to-run** development environment
- **No local setup** required

## Created Using Ivy

Web application created using [Ivy-Framework](https://github.com/Ivy-Interactive/Ivy-Framework).

**Ivy** - The ultimate framework for building internal tools with LLM code generation by unifying front-end and back-end into a single C# codebase. With Ivy, you can build robust internal tools and dashboards using C# and AI assistance based on your existing database.

Ivy is a web framework for building interactive web applications using C# and .NET.

## Interactive Examples for LlmTornado Library

This example demonstrates various capabilities of the [LlmTornado](https://llmtornado.ai) library for interacting with LLM models. The application showcases streaming responses, function calling with tools, and agent-based interactions.

**What This Application Does:**

This implementation creates an **LlmTornado Examples** workspace that allows users to:

- **Simple Chat**: Basic conversation interface with real-time streaming responses
- **Agent with Tools**: AI agent with function calling capabilities (GetCurrentTime, Calculate, GetWeather)
- **Dynamic Model Discovery**: Automatically discovers available Ollama models from your local installation
- **Streaming Responses**: Real-time streaming of responses as they're generated with markdown formatting
- **Model Selection**: Select and switch between different Ollama models with live filtering
- **Customizable Instructions**: Configure agent instructions for different use cases

**Technical Implementation:**

- **LlmTornado Library** for LLM model interactions and streaming
- **TornadoAgent** with function calling/tools support
- **Ollama Integration** using TornadoApi client
- **Dynamic Model Loading**: Automatic discovery via Ollama API `/api/tags` endpoint
- **Custom Tool Definitions**: GetCurrentTime, Calculate, and GetWeather tools
- **Blade-based Navigation**: Seamless navigation between different examples
- **Streaming Support**: Real-time response streaming with UI updates every 100ms
- **Markdown Rendering**: Text.Markdown() for rich text formatting in chat messages
- **State Management**: React-like hooks (`UseState`, `UseEffect`) for reactive UI updates

## Examples

### Simple Chat

Basic conversation interface with streaming responses. Perfect for simple Q&A interactions with any Ollama model.

**Features:**
- Real-time streaming responses
- Markdown formatting support
- Works with any Ollama model

### Agent with Tools

AI agent with function calling capabilities. The agent can use tools to perform actions like getting the current time, performing calculations, and getting weather information.

**Features:**
- Function calling / tools support
- Customizable agent instructions
- Three built-in tools:
  - **GetCurrentTime**: Get current date and time (with timezone support)
  - **Calculate**: Perform math operations (add, subtract, multiply, divide)
  - **GetWeather**: Get weather information for any city
- Streaming responses with tool execution
- Settings dialog for editing agent instructions

**Note:** Not all models support function calling. The application will display a helpful message if the selected model doesn't support tools.

## Tools

| Tool | Description |
|------|-------------|
| **GetCurrentTime** | Get the current date and time with optional timezone support (e.g., UTC, Eastern Standard Time) |
| **Calculate** | Perform mathematical calculations: add, subtract, multiply, or divide two numbers |
| **GetWeather** | Get weather information for a specific city (simulated data for demonstration) |

## Prerequisites

1. **.NET 10.0 SDK** or later
2. **Ollama** installed and running: [ollama.ai](https://ollama.ai)
3. **Ollama Model**: Install at least one model (e.g., `ollama pull llama3.2:1b` or `ollama pull llama3.2`)

## How to Run

1. **Install and start Ollama**:

   ```bash
   # Download from https://ollama.ai
   # Start Ollama service (usually runs automatically)
   ollama serve
   ```

2. **Pull a model** (in another terminal):

   ```bash
   ollama pull llama3.2:1b
   # Or use another model like llama3.2, mistral, gemma, etc.
   ```

3. **Navigate to the example**:

   ```bash
   cd project-demos/llm-tornado
   ```

4. **Restore dependencies**:

   ```bash
   dotnet restore
   ```

5. **Run the application**:

   ```bash
   dotnet watch
   ```

6. **Open your browser** to the URL shown in the terminal (typically `http://localhost:5010`)

7. **Configure Ollama** (if needed): Set the Ollama URL (default: `http://localhost:11434`) and select a model from the dropdown. The application will automatically discover available models.

## How to Deploy

Deploy this example to Ivy's hosting platform:

1. **Navigate to the example**:

   ```bash
   cd project-demos/llm-tornado
   ```

2. **Deploy to Ivy hosting**:

   ```bash
   ivy deploy
   ```

3. **Configure environment variables** in your deployment settings:
   - Set `OLLAMA_URL` to your Ollama server URL (default: `http://localhost:11434`)
   - Set `OLLAMA_MODEL` to your preferred model (default: `llama3.2:1b`)

This will deploy your LlmTornado examples with a single command.

## Project Structure

```text
project-demos/llm-tornado/
├── Apps/
│   ├── LlmTornadoApp.cs          # Main app with UseBlades and model selection
│   ├── SimpleChatBlade.cs        # Simple chat interface with streaming
│   └── AgentChatBlade.cs         # Agent chat with tools and function calling
├── Program.cs
├── GlobalUsings.cs
└── LlmTornadoExample.csproj
```

## Learn More

- LlmTornado: [llmtornado.ai](https://llmtornado.ai)
- Ollama: [ollama.ai](https://ollama.ai)
- Ivy Documentation: [docs.ivy.app](https://docs.ivy.app)
- Ivy Framework: [github.com/Ivy-Interactive/Ivy-Framework](https://github.com/Ivy-Interactive/Ivy-Framework)

## Tags

AI, LLM, Ollama, LlmTornado, Chat, Function Calling, Tools, Streaming, Agents, Local AI, Markdown
