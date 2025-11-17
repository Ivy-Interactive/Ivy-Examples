# OpperAI Chat

<img width="1917" height="910" alt="image" src="https://github.com/user-attachments/assets/c552502c-87da-439c-ba6c-c0aa5575f44c" />

<img width="1919" height="918" alt="image" src="https://github.com/user-attachments/assets/4ca84406-2a93-4f7d-b125-155586a4aede" />

## One-Click Development Environment

[![Open in GitHub Codespaces](https://github.com/codespaces/badge.svg)](https://github.com/codespaces/new?hide_repo_select=true&ref=main&repo=Ivy-Interactive%2FIvy-Examples&machine=standardLinux32gb&devcontainer_path=.devcontainer%2Fopperai%2Fdevcontainer.json&location=EuropeWest)

Click the badge above to open Ivy Examples repository in GitHub Codespaces with:
- **.NET 9.0** SDK pre-installed
- **Ready-to-run** development environment
- **No local setup** required

## Created Using Ivy

Web application created using [Ivy-Framework](https://github.com/Ivy-Interactive/Ivy-Framework).

**Ivy** - The ultimate framework for building internal tools with LLM code generation by unifying front-end and back-end into a single C# codebase. With Ivy, you can build robust internal tools and dashboards using C# and AI assistance based on your existing database.

Ivy is a web framework for building interactive web applications using C# and .NET.

## Interactive Example For AI Chat with Opper.ai

This example demonstrates AI-powered chat functionality using [Opper.ai](https://docs.opper.ai) integrated with Ivy. Since Opper.ai doesn't have an official C# SDK, this example includes a complete implementation of the Opper.ai API client (OpperDotNet).

**What This Application Does:**

This implementation creates an **AI Chat Application** that allows users to:

- **Interactive AI Chat**: Real-time conversation interface powered by Opper.ai models
- **Model Selection**: Choose from any available Opper.ai model (e.g., Claude, GPT-4)
- **Conversation Context**: Maintains conversation history for contextual responses
- **Custom Instructions**: Configure AI behavior and response formatting
- **Mathematical Expressions**: Supports LaTeX notation for math rendering (`$\sqrt{-1}$`, `$$i = \sqrt{-1}$$`)
- **Markdown Support**: AI responses render with proper Markdown formatting
- **API Key Management**: Secure API key configuration with validation
- **Error Handling**: Graceful error handling with user-friendly messages

**Technical Implementation:**

- Custom **OpperDotNet** C# client library for Opper.ai API
- `OpperClient` with async/await pattern for non-blocking API calls
- Type-safe request/response models (`OpperCallRequest`, `OpperCallResponse`)
- Support for custom models, instructions, and JSON schema output
- `HeaderLayout` with fixed header and scrollable chat content
- React Rules of Hooks compliant code structure
- LaTeX and Markdown rendering for AI responses

## Prerequisites

1. **.NET 9.0 SDK** or later
2. **Opper.ai API Key**: Get yours at [platform.opper.ai/settings/api-keys](https://platform.opper.ai/settings/api-keys)

## How to Run

1. **Navigate to the example**:
   ```bash
   cd project-demos/opperai
   ```

2. **Restore dependencies**:
   ```bash
   dotnet restore
   ```

3. **Run the application**:
   ```bash
   dotnet watch
   ```

4. **Open your browser** to the URL shown in the terminal (typically `http://localhost:5010`)

## How to Deploy

Deploy this example to Ivy's hosting platform:

1. **Navigate to the example**:
   ```bash
   cd project-demos/opperai
   ```

2. **Deploy to Ivy hosting**:
   ```bash
   ivy deploy
   ```

3. **Configure environment variables** in your deployment settings:
   - Set `OPPER_API_KEY` to your Opper.ai API key
   - Optionally set `OPPER_MODEL` to specify a default model

This will deploy your AI chat application with a single command.

## Using OpperDotNet in Your Own Projects

The OpperDotNet library can be used independently in your C# projects:

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
var request = new OpperCallRequest
{
    Name = "analyzeText",
    Instructions = "Analyze the text and extract topic, sentiment, and keywords",
    Input = "This amazing product revolutionized our workflow!",
    OutputSchema = new
    {
        type = "object",
        properties = new
        {
            topic = new { type = "string" },
            sentiment = new { type = "string" },
            keywords = new { type = "array", items = new { type = "string" } }
        },
        required = new[] { "topic", "sentiment", "keywords" }
    },
    Model = "aws/claude-3.5-sonnet-eu"
};

var response = await client.CallAsync(request);
var jsonPayload = response.JsonPayload;
```

## Learn More

- Opper.ai Documentation: [docs.opper.ai](https://docs.opper.ai)
- Opper.ai Platform: [platform.opper.ai](https://platform.opper.ai)
- Ivy Documentation: [docs.ivy.app](https://docs.ivy.app)
- Ivy Framework: [github.com/Ivy-Interactive/Ivy-Framework](https://github.com/Ivy-Interactive/Ivy-Framework)
