# AI Agent Workspace

## Description

AI Agent Workspace is a web application for creating and interacting with customizable AI agents using Microsoft Agent Framework and Ollama. It features a blade-based navigation for managing multiple agent personas with configurable tools, dynamic model discovery, and real-time tool invocation visualization.

https://github.com/user-attachments/assets/765c9bf6-05e8-4eb9-acad-ced624f818bd

## One-Click Development Environment

[![Open in GitHub Codespaces](https://github.com/codespaces/badge.svg)](https://github.com/codespaces/new?hide_repo_select=true&ref=main&repo=Ivy-Interactive%2FIvy-Examples&machine=standardLinux32gb&devcontainer_path=.devcontainer%2Fmicrosoft_agent_framework%2Fdevcontainer.json&location=EuropeWest)

Click the badge above to open Ivy Examples repository in GitHub Codespaces with:
- **.NET 10.0** SDK pre-installed
- **Ready-to-run** development environment
- **No local setup** required

## Created Using Ivy

Web application created using [Ivy-Framework](https://github.com/Ivy-Interactive/Ivy-Framework).

**Ivy** - The ultimate framework for building internal tools with LLM code generation by unifying front-end and back-end into a single C# codebase. With Ivy, you can build robust internal tools and dashboards using C# and AI assistance based on your existing database.

Ivy is a web framework for building interactive web applications using C# and .NET.

## Interactive Example For AI Agents with Microsoft Agent Framework

This example demonstrates AI agent functionality using [Microsoft Agent Framework](https://learn.microsoft.com/en-us/agent-framework/overview/agent-framework-overview) integrated with [Ollama](https://ollama.ai) for local AI models. The application showcases function calling, tool integration, and multi-agent management.

**What This Application Does:**

This implementation creates an **AI Agent Workspace** that allows users to:

- **Agent Management**: Create, edit, duplicate, and delete AI agent personas with custom instructions
- **Preset Agents**: Four ready-to-use agents (Creative Writer, Data Analyst, Code Assistant, Research Assistant)
- **Dynamic Model Discovery**: Automatically detects and lists available Ollama models from your local installation
- **Tool Integration**: Agents can use Calculator, DateTime, and Web Search tools with function calling
- **Blade Navigation**: Intuitive master-detail interface for managing agents and chat
- **Tool Visualization**: See which tools the agent uses in real-time during conversations
- **Streaming Responses**: Real-time streaming of agent responses as they're generated
- **Model Selection**: Search and select from available Ollama models with live filtering

**Technical Implementation:**

- **Microsoft Agent Framework** with `ChatClientAgent` for agent orchestration
- **OllamaSharp** integration using `OllamaApiClient` as `IChatClient` implementation
- **Dynamic Model Loading**: Automatic discovery of available models via `ListLocalModelsAsync()`
- **Custom Tool Definitions**: `AITool` implementations for Calculator, GetCurrentTime, and SearchWeb
- **Blade-based Navigation**: Master-detail pattern with `UseBlades` for seamless UX
- **Real-time Tool Invocation**: Display of tool calls and results in chat interface
- **State Management**: React-like hooks (`UseState`, `UseEffect`) for reactive UI updates

## Preset Agents

| Agent | Description | Tools |
|-------|-------------|-------|
| **Creative Writer** | Crafts stories, poetry, and creative content with research capabilities | GetCurrentTime, SearchWeb, Calculate |
| **Data Analyst** | Expert in calculations and analytical thinking | Calculate, GetCurrentTime, SearchWeb |
| **Code Assistant** | Programming expert for coding and debugging | Calculate, GetCurrentTime, SearchWeb |
| **Research Assistant** | Researches topics with web search and fact-checking | Calculate, GetCurrentTime, SearchWeb |

## Tools

| Tool | Description |
|------|-------------|
| **Calculate** | Mathematical operations: arithmetic (+, -, *, /), sqrt, sin, cos, tan, log, power (^), and complex expressions |
| **GetCurrentTime** | Date/time functions: current time with timezone support, day of week, date formatting |
| **SearchWeb** | Web search using Bing Search API (requires API key) for current information and facts |

## Prerequisites

1. **.NET 10.0 SDK** or later
2. **Ollama** installed and running: [ollama.ai](https://ollama.ai)
3. **Ollama Model**: Install at least one model (e.g., `ollama pull phi3` or `ollama pull llama3`)
4. **Bing Search API Key** (optional): For web search functionality - get yours at [Azure Portal](https://portal.azure.com)

## How to Run

1. **Install and start Ollama**:
   ```bash
   # Download from https://ollama.ai
   # Start Ollama service (usually runs automatically)
   ollama serve
   ```

2. **Pull a model** (in another terminal):
   ```bash
   ollama pull phi3
   # Or use another model like llama3, mistral, gemma, etc.
   ```

3. **Navigate to the example**:
   ```bash
   cd project-demos/microsoft_agent_framework
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

7. **Configure Ollama** (if needed): Click the Settings button and configure Ollama URL and model name. The application will automatically discover available models.

## How to Deploy

Deploy this example to Ivy's hosting platform:

1. **Navigate to the example**:
   ```bash
   cd project-demos/microsoft_agent_framework
   ```

2. **Deploy to Ivy hosting**:
   ```bash
   ivy deploy
   ```

3. **Configure environment variables** in your deployment settings:
   - Set `OLLAMA_URL` to your Ollama server URL (default: `http://localhost:11434`)
   - Set `OLLAMA_MODEL` to your preferred model (default: `llama2`)
   - Optionally set `BING_API_KEY` for web search functionality

This will deploy your AI agent workspace with a single command.

## Project Structure

```
project-demos/microsoft_agent_framework/
├── Apps/
│   └── AgentWorkspaceExample.cs    # Main app with UseBlades and dynamic model loading
├── Views/
│   ├── AgentListView.cs            # Blade 1: Agent list management
│   ├── AgentSettingsView.cs        # Blade 2: Agent configuration
│   └── AgentChatView.cs            # Blade 3: Chat interface with tool visualization
├── Services/
│   ├── AgentManager.cs             # Agent management service with streaming support
│   └── AgentTools.cs               # Static tools (Calculate, GetCurrentTime, SearchWeb)
├── Models/
│   └── AgentConfiguration.cs       # Agent configuration model
├── Program.cs
├── GlobalUsings.cs
└── MicrosoftAgentFramework.csproj
```

## Learn More

- Microsoft Agent Framework: [learn.microsoft.com/agent-framework](https://learn.microsoft.com/en-us/agent-framework/overview/agent-framework-overview)
- Ollama: [ollama.ai](https://ollama.ai)
- Ivy Documentation: [docs.ivy.app](https://docs.ivy.app)
- Ivy Framework: [github.com/Ivy-Interactive/Ivy-Framework](https://github.com/Ivy-Interactive/Ivy-Framework)

## Tags

AI, Agents, Ollama, Microsoft Agent Framework, Chat, Function Calling, Tools, Multi-Agent, LLM, Local AI, Streaming, Tool Invocation
