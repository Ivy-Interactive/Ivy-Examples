# Hello 

Web application created using [Ivy](https://github.com/Ivy-Interactive/Ivy). 

Ivy is a web framework for building interactive web applications using C# and .NET.

## Run

```
dotnet watch
```

## Deploy

```
ivy deploy
```

# OllamaSharp Demo

This project demonstrates how to integrate [Ollama](https://ollama.com/) with an Ivy application using the [OllamaSharp](https://github.com/awaescher/OllamaSharp) library.

## What is OllamaSharp?

The `OllamaSharp` is an Ivy application that provides a chat interface for interacting with local Large Language Models (LLMs) through Ollama. The application features:

- **Model Selection**: Browse and select from locally available Ollama models
- **Interactive Chat**: Real-time chat interface with streaming responses
- **Model Management**: Easy switching between different models
- **Connection Status**: Automatic detection of Ollama service availability

The sample demonstrates how to:
- Connect to a local Ollama API instance
- List available models
- Send chat messages and receive streaming responses
- Handle connection errors and service availability

## Prerequisites

### 1. Install Ollama

First, you need to install Ollama on your local machine:

#### Windows
1. Download Ollama from [https://ollama.com/download](https://ollama.com/download)
2. Run the installer
3. Ollama will automatically start as a service

#### macOS
```bash
# Using Homebrew
brew install ollama

# Or download from https://ollama.com/download
```

#### Linux
```bash
curl -fsSL https://ollama.com/install.sh | sh
```

### 2. Download Models

After installing Ollama, you need to download at least one model to use with the application:

```bash
# Download a popular model (this may take a while depending on model size)
ollama pull llama2

# Or try a smaller model for testing
ollama pull phi3

# List other available models
ollama list
```

### 3. Start Ollama Service

Ensure Ollama is running locally:

```bash
# Start Ollama (if not running as a service)
ollama serve
```

By default, Ollama runs on `http://localhost:11434`

## Running the Application

1. **Clone and navigate to the project**:
   ```bash
   cd ollamasharp
   ```

2. **Restore dependencies**:
   ```bash
   dotnet restore
   ```

3. **Run the application**:
   ```bash
   dotnet run
   ```

4. **Use the application**:
   - Click "Refresh Models" to load available Ollama models
   - Select a model from the list
   - Start chatting with the selected model
   - Use "Back to Models" to switch to a different model

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
3. Restart the application and click "Refresh Models"

## Dependencies

- **OllamaSharp**: .NET client library for Ollama API
- **Ivy Framework**: UI framework for building the chat interface

## Configuration

The application connects to Ollama on `http://localhost:11434` by default. You can modify the `Url` constant in `OllamaSharp.cs` to connect to a different Ollama instance if needed.
