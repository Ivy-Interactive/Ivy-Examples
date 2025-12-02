# AI Agent Workspace

## Description

AI Agent Workspace is a web application for creating and interacting with customizable AI agents. Built with Microsoft Agent Framework and Ollama, it features a blade-based navigation for managing multiple agent personas with configurable tools.

## One-Click Development Environment

[![Open in GitHub Codespaces](https://github.com/codespaces/badge.svg)](https://github.com/codespaces/new?hide_repo_select=true&ref=main&repo=Ivy-Interactive%2FIvy-Examples&machine=standardLinux32gb&devcontainer_path=.devcontainer%2Fms-agent-framework%2Fdevcontainer.json&location=EuropeWest)

Click the badge above to open Ivy Examples repository in GitHub Codespaces with:
- **.NET 9.0** SDK pre-installed
- **Ready-to-run** development environment
- **No local setup** required

## Created Using Ivy

Web application created using [Ivy-Framework](https://github.com/Ivy-Interactive/Ivy-Framework).

**Ivy** - The ultimate framework for building internal tools with LLM code generation by unifying front-end and back-end into a single C# codebase.

## Features

**What This Application Does:**

- **Agent Management**: Create, edit, duplicate, and delete AI agent personas
- **Preset Agents**: Four ready-to-use agents (Creative Writer, Data Analyst, Code Assistant, Research Assistant)
- **Custom Agents**: Create your own agents with custom instructions and tool configurations
- **Tool Integration**: Agents can use Calculator, DateTime, and Web Search tools
- **Blade Navigation**: Intuitive master-detail interface for managing agents and chat
- **Tool Visualization**: See which tools the agent uses in real-time during conversations

**Technical Implementation:**

- Microsoft Agent Framework with Ollama for local AI models
- OllamaSharp integration for IChatClient compatibility
- Custom tool definitions for Calculator, DateTime, and Web Search
- Blade-based navigation pattern for seamless UX
- Real-time tool invocation display in chat responses

## Preset Agents

| Agent | Description | Tools |
|-------|-------------|-------|
| **Creative Writer** | Crafts stories, poetry, and creative content | DateTime |
| **Data Analyst** | Expert in calculations and analytical thinking | Calculator, DateTime |
| **Code Assistant** | Programming expert for coding and debugging | Calculator, DateTime |
| **Research Assistant** | Researches topics with web search | Calculator, DateTime, WebSearch |

## Tools

| Tool | Description |
|------|-------------|
| **Calculator** | Mathematical operations: add, subtract, multiply, divide, sqrt, power, percentage, abs, round |
| **DateTime** | Date/time functions: current time, day of week, days between dates, formatting |
| **Web Search** | Search the web using Bing Search API (requires API key) |

## Prerequisites

1. **.NET 9.0 SDK** or later
2. **Ollama** installed and running: [ollama.ai](https://ollama.ai)
3. **Ollama Model**: Install a model (e.g., `ollama pull phi3`)
4. **Bing Search API Key** (optional): For web search functionality

## How to Run

1. **Install and start Ollama**:
   ```bash
   # Download from https://ollama.ai
   # Start Ollama service
   ollama serve
   ```

2. **Pull a model** (in another terminal):
   ```bash
   ollama pull phi3
   # Or use another model like llama3, mistral, etc.
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

7. **Configure Ollama**: Click the Settings button and configure Ollama URL and model name

## How to Deploy

Deploy this example to Ivy's hosting platform:

1. **Navigate to the example**:
   ```bash
   cd project-demos/ms-agent-framework
   ```

2. **Deploy to Ivy hosting**:
   ```bash
   ivy deploy
   ```

3. **Configure environment variables** in your deployment settings:
   - Set `OLLAMA_URL` to your Ollama server URL (default: `http://localhost:11434`)
   - Set `OLLAMA_MODEL` to your preferred model (default: `phi3`)
   - Optionally set `BING_API_KEY` for web search functionality

## Environment Variables

| Variable | Required | Description |
|----------|----------|-------------|
| `OLLAMA_URL` | No | Ollama server URL (default: `http://localhost:11434`) |
| `OLLAMA_MODEL` | No | Ollama model name (default: `phi3`) |
| `BING_API_KEY` | No | Bing Search API key for web search tool |

## Project Structure

```
project-demos/microsoft_agent_framework/
├── Apps/
│   └── AgentWorkspaceExample.cs    # Main app with UseBlades
├── Views/
│   ├── AgentListView.cs            # Blade 1: Agent list
│   ├── AgentSettingsView.cs        # Blade 2: Agent configuration
│   └── AgentChatView.cs            # Blade 3: Chat interface
├── Tools/
│   ├── CalculatorTool.cs           # Math operations
│   ├── DateTimeTool.cs             # Date/time functions
│   └── WebSearchTool.cs            # Web search
├── Models/
│   └── AgentConfiguration.cs       # Agent config model
├── Services/
│   └── AgentManager.cs             # Agent management service
├── Program.cs
├── GlobalUsings.cs
└── MicrosoftAgentFramework.csproj
```

## Learn More

- Microsoft Agent Framework: [learn.microsoft.com/agent-framework](https://learn.microsoft.com/en-us/agent-framework/overview/agent-framework-overview)
- Ollama: [ollama.ai](https://ollama.ai)
- OllamaSharp: [github.com/tdh8316/OllamaSharp](https://github.com/tdh8316/OllamaSharp)
- Ivy Documentation: [docs.ivy.app](https://docs.ivy.app)
- Ivy Framework: [github.com/Ivy-Interactive/Ivy-Framework](https://github.com/Ivy-Interactive/Ivy-Framework)

## Tags

AI, Agents, Ollama, Microsoft Agent Framework, Chat, Function Calling, Tools, Multi-Agent, LLM, Local AI

