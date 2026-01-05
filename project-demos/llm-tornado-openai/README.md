# LlmTornado OpenAI Examples

## Description

LlmTornado OpenAI Examples is a web application demonstrating the [LlmTornado](https://llmtornado.ai) library capabilities for interacting with OpenAI models. It features multiple chat interfaces including simple streaming chat and AI agents with function calling/tools support.

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

This implementation creates an **LlmTornado OpenAI Examples** workspace that allows users to:

- **Simple Chat**: Basic conversation interface with real-time streaming responses
- **Agent with Tools**: AI agent with function calling capabilities (GetCurrentTime, Calculate, GetWeather)
- **OpenAI Model Selection**: Select from popular OpenAI models (gpt-4o, gpt-4o-mini, gpt-4-turbo, gpt-4, gpt-3.5-turbo)
- **Streaming Responses**: Real-time streaming of responses as they're generated with markdown formatting
- **Secure API Key Management**: Uses dotnet user-secrets for secure API key storage
- **Customizable Instructions**: Configure agent instructions for different use cases

**Technical Implementation:**

- **LlmTornado Library** for LLM model interactions and streaming
- **TornadoAgent** with function calling/tools support
- **OpenAI Integration** using TornadoApi client with API key authentication
- **Secure Configuration**: API keys stored using dotnet user-secrets
- **Custom Tool Definitions**: GetCurrentTime, Calculate, and GetWeather tools
- **Blade-based Navigation**: Seamless navigation between different examples
- **Streaming Support**: Real-time response streaming with UI updates every 100ms
- **Markdown Rendering**: Text.Markdown() for rich text formatting in chat messages
- **State Management**: React-like hooks (`UseState`, `UseEffect`) for reactive UI updates

## Examples

### Simple Chat

Basic conversation interface with streaming responses. Perfect for simple Q&A interactions with OpenAI models.

**Features:**
- Real-time streaming responses
- Markdown formatting support
- Works with any OpenAI model

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
2. **OpenAI API Key**: Get your API key from [platform.openai.com/api-keys](https://platform.openai.com/api-keys)

## How to Run

1. **Get your OpenAI API Key**:
   - Visit [platform.openai.com/api-keys](https://platform.openai.com/api-keys)
   - Create a new API key if you don't have one

2. **Navigate to the example**:

   ```bash
   cd project-demos/llm-tornado-openai
   ```

3. **Restore dependencies**:

   ```bash
   dotnet restore
   ```

4. **Set up OpenAI API Key using dotnet user-secrets**:

   ```bash
   # Set your OpenAI API key
   dotnet user-secrets set "OpenAI:ApiKey" "your-api-key-here"

   # Set your OpenAI model (recommended: gpt-4o-mini)
   dotnet user-secrets set "OpenAI:Model" "gpt-4o-mini"
   ```

5. **Run the application**:

   ```bash
   dotnet watch
   ```

6. **Open your browser** to the URL shown in the terminal (typically `http://localhost:5010`)


## How to Deploy

Deploy this example to Ivy's hosting platform:

1. **Navigate to the example**:

   ```bash
   cd project-demos/llm-tornado-openai
   ```

2. **Deploy to Ivy hosting**:

   ```bash
   ivy deploy
   ```

3. **Configure environment variables** in your deployment settings:
   - Set `OpenAI:ApiKey` to your OpenAI API key
   - Set `OpenAI:Model` to your preferred model (e.g., `gpt-4o-mini`)

This will deploy your LlmTornado OpenAI examples with a single command.

## Project Structure

```text
project-demos/llm-tornado-openai/
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
- OpenAI: [platform.openai.com](https://platform.openai.com)
- OpenAI API Documentation: [platform.openai.com/docs](https://platform.openai.com/docs)
- Ivy Documentation: [docs.ivy.app](https://docs.ivy.app)
- Ivy Framework: [github.com/Ivy-Interactive/Ivy-Framework](https://github.com/Ivy-Interactive/Ivy-Framework)

## Tags

AI, LLM, OpenAI, LlmTornado, Chat, Function Calling, Tools, Streaming, Agents, GPT-4, GPT-3.5, Markdown, User Secrets
