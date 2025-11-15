# Opper.ai Chat Example

An AI-powered chat application built with [Ivy Framework](https://github.com/Ivy-Interactive/Ivy-Framework) and [Opper.ai](https://docs.opper.ai).

## Overview

This example demonstrates how to integrate Opper.ai's powerful AI capabilities into an Ivy application using a custom C# client library (OpperDotNet). Since Opper.ai doesn't have an official C# SDK, this example includes a complete implementation of the Opper.ai API client.

## Features

### OpperDotNet Library

A complete C# client library for Opper.ai with:

- **OpperClient**: Main client with API key authentication and HTTP communication
- **OpperCallRequest**: Type-safe request model supporting all Opper.ai call parameters
- **OpperCallResponse**: Response model with structured output support
- **OpperException**: Custom exception handling for API errors
- Async/await pattern for non-blocking API calls
- Support for custom models, instructions, and JSON schema output
- Comprehensive error handling and validation

### Chat Application

- **Real-time AI Chat**: Interactive chat interface powered by Opper.ai
- **Conversation Context**: Maintains conversation history for contextual responses
- **Model Selection**: Supports any Opper.ai model via environment variable
- **Error Handling**: Graceful error handling with user-friendly messages
- **Modern UI**: Built with Ivy's reactive UI components

## Prerequisites

1. **.NET 9.0 SDK** or later
2. **Opper.ai API Key**: Get yours at [platform.opper.ai](https://platform.opper.ai/settings/api-keys)

## Setup

### 1. Set Environment Variables

Set your Opper.ai API key as an environment variable:

**Windows (PowerShell):**
```powershell
$env:OPPER_API_KEY="your-api-key-here"
```

**Windows (Command Prompt):**
```cmd
set OPPER_API_KEY=your-api-key-here
```

**Linux/macOS:**
```bash
export OPPER_API_KEY="your-api-key-here"
```

**Optional: Specify a custom model:**
```bash
export OPPER_MODEL="gpt-4"  # or any Opper.ai supported model
```

### 2. Navigate to Example Directory

```bash
cd packages-demos/opperai
```

### 3. Restore Dependencies

```bash
dotnet restore
```

### 4. Run the Application

```bash
dotnet watch
```

The application will start and display a URL (typically `http://localhost:5010`). Open this URL in your browser.

## Project Structure

```
opperai/
├── OpperDotNet/              # OpperDotNet client library
│   ├── OpperClient.cs        # Main API client
│   ├── OpperCallRequest.cs   # Request models
│   ├── OpperCallResponse.cs  # Response models
│   └── OpperException.cs     # Custom exceptions
├── Apps/
│   └── OpperaiChatExample.cs # Chat UI application
├── OpperaiExample.csproj     # Project configuration
├── Program.cs                # Application entry point
├── GlobalUsings.cs           # Global imports
└── README.md                 # This file
```

## Using OpperDotNet in Your Own Projects

You can use the OpperDotNet library in your own C# projects:

### Basic Usage

```csharp
using OpperDotNet;

// Initialize client
var client = new OpperClient("your-api-key");

// Simple call
var response = await client.CallAsync(
    name: "myTask",
    instructions: "Extract the main topic from the text",
    input: "The article discusses climate change and renewable energy."
);

Console.WriteLine(response.Message);
```

### Advanced Usage with Structured Output

```csharp
using OpperDotNet;

var client = new OpperClient("your-api-key");

// Define JSON schema for structured output
var schema = new
{
    type = "object",
    properties = new
    {
        topic = new { type = "string" },
        sentiment = new { type = "string" },
        keywords = new
        {
            type = "array",
            items = new { type = "string" }
        }
    },
    required = new[] { "topic", "sentiment", "keywords" }
};

var request = new OpperCallRequest
{
    Name = "analyzeText",
    Instructions = "Analyze the text and extract topic, sentiment, and keywords",
    Input = "This amazing product revolutionized our workflow!",
    OutputSchema = schema,
    Model = "gpt-4" // Optional: specify model
};

var response = await client.CallAsync(request);

// Access structured JSON output
var jsonPayload = response.JsonPayload;
```

### Error Handling

```csharp
try
{
    var response = await client.CallAsync(request);
    Console.WriteLine(response.Message);
}
catch (OpperException ex)
{
    Console.WriteLine($"API Error: {ex.Message}");
    if (ex.StatusCode.HasValue)
        Console.WriteLine($"Status Code: {ex.StatusCode}");
}
```

## Deployment

Deploy to Ivy's hosting platform:

```bash
cd packages-demos/opperai
ivy deploy
```

Make sure to configure your `OPPER_API_KEY` environment variable in your deployment settings.

## How It Works

1. **Initialization**: The app reads the `OPPER_API_KEY` from environment variables
2. **User Input**: User types a message in the chat interface
3. **Context Building**: The app builds conversation context from message history
4. **API Call**: OpperClient sends a request to Opper.ai with instructions and context
5. **Response Display**: The AI response is displayed in the chat interface
6. **History Update**: Conversation history is updated for future context

## API Reference

### OpperClient

```csharp
// Constructor
public OpperClient(string apiKey, string? baseUrl = null)

// Simple call
public Task<OpperCallResponse> CallAsync(
    string name,
    string instructions,
    string input,
    string? model = null,
    CancellationToken cancellationToken = default)

// Advanced call
public Task<OpperCallResponse> CallAsync(
    OpperCallRequest request,
    CancellationToken cancellationToken = default)
```

### OpperCallRequest Properties

- `Name`: Task identifier for tracking
- `Instructions`: Instructions for the AI model
- `Input`: The text/data to process
- `Model`: Optional model to use (defaults to Opper's default)
- `OutputSchema`: Optional JSON schema for structured output

### OpperCallResponse Properties

- `Message`: Text response from the AI
- `JsonPayload`: Structured JSON output (when schema provided)
- `Uuid`: Unique call identifier
- `Model`: Model used for the call
- `DurationMs`: Call duration in milliseconds
- `InputTokens`: Input tokens used
- `OutputTokens`: Output tokens used

## Learn More

- **Opper.ai Documentation**: [docs.opper.ai](https://docs.opper.ai)
- **Opper.ai Platform**: [platform.opper.ai](https://platform.opper.ai)
- **Ivy Framework**: [github.com/Ivy-Interactive/Ivy-Framework](https://github.com/Ivy-Interactive/Ivy-Framework)
- **Ivy Documentation**: [docs.ivy.app](https://docs.ivy.app)

## License

This example is part of the Ivy Examples repository and follows the same license.

