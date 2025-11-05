# RestSharp

<img width="1277" height="771" alt="image" src="https://github.com/user-attachments/assets/4a5301c0-3867-43ff-8ec2-49a455b49627" />

<img width="1199" height="831" alt="image" src="https://github.com/user-attachments/assets/32880c79-311a-4bfd-9955-7238b6a5bcd7" />

<img width="1201" height="679" alt="image" src="https://github.com/user-attachments/assets/b3ea40c6-a791-410b-bde9-2d8a9a9a73d2" />


## One-Click Development Environment

[![Open in GitHub Codespaces](https://github.com/codespaces/badge.svg)](https://github.com/codespaces/new?hide_repo_select=true&ref=main&repo=Ivy-Interactive%2FIvy-Examples&machine=standardLinux32gb&devcontainer_path=.devcontainer%2Frestsharp%2Fdevcontainer.json&location=EuropeWest)

Click the badge above to open Ivy Examples repository in GitHub Codespaces with:
- **.NET 9.0** SDK pre-installed
- **Ready-to-run** development environment
- **No local setup** required

## Created Using Ivy

Web application created using [Ivy-Framework](https://github.com/Ivy-Interactive/Ivy-Framework).

**Ivy** - The ultimate framework for building internal tools with LLM code generation by unifying front-end and back-end into a single C# codebase. With Ivy, you can build robust internal tools and dashboards using C# and AI assistance based on your existing database.

Ivy is a web framework for building interactive web applications using C# and .NET.

## Interactive Example For REST API Testing

This example demonstrates REST API testing and HTTP request handling using the [RestSharp library](https://github.com/restsharp/RestSharp) integrated with Ivy. RestSharp is a simple REST and HTTP API client for .NET that makes it easy to interact with RESTful services.

**What This Application Does:**

This specific implementation creates a **REST API Client** that allows users to:

- **Send HTTP Requests**: Supports GET, POST, PUT, PATCH, and DELETE methods
- **Test REST APIs**: Interact with RESTful APIs like `restful-api.dev` for testing
- **View Responses**: See formatted JSON responses with syntax highlighting
- **Manage Request Body**: Edit JSON request bodies with a code editor for POST/PUT/PATCH requests
- **Handle Resource IDs**: Automatically handle resource IDs for operations on specific objects
- **Format JSON**: Toggle JSON formatting for better readability
- **Status Indicators**: Visual feedback with success/error callouts based on HTTP status codes
- **Interactive UI**: Clean card-based layout with Request and Response sections

**Technical Implementation:**

- Uses RestSharp's `RestClient` and `RestRequest` for HTTP operations
- Supports all standard HTTP methods (GET, POST, PUT, PATCH, DELETE)
- Automatic URL construction with resource IDs
- JSON request/response handling with System.Text.Json
- Code input components with JSON language support
- Real-time status code display with color-coded callouts
- Automatic example JSON generation for POST/PUT/PATCH methods
- Disabled state management for request body when not applicable

**Features:**

- **Dynamic URL Building**: Automatically appends resource IDs to URLs for DELETE/PUT/PATCH operations
- **Method-Specific UI**: Request body editor is enabled/disabled based on selected HTTP method
- **JSON Formatting**: Toggle between formatted and raw JSON responses
- **Error Handling**: Displays error messages and status codes for failed requests
- **Example Data**: Pre-populated JSON examples for quick testing

## How to Run

1. **Prerequisites**: .NET 9.0 SDK
2. **Navigate to the example**:
   ```bash
   cd restsharp
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
   cd restsharp
   ```
2. **Deploy to Ivy hosting**:
   ```bash
   ivy deploy
   ```
This will deploy your REST API testing application with a single command.

## Learn More

- RestSharp for .NET overview: [github.com/restsharp/RestSharp](https://github.com/restsharp/RestSharp)
- RestSharp Migration Guide: [restsharp.dev/migration](https://restsharp.dev/migration/)
- Ivy Documentation: [docs.ivy.app](https://docs.ivy.app)
