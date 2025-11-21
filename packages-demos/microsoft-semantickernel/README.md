# Microsoft.SemanticKernel

<img width="1918" height="917" alt="image" src="https://github.com/user-attachments/assets/d3d5c7d5-8a34-4325-bb6d-e2ec655d0f46" />

## One-Click Development Environment

[![Open in GitHub Codespaces](https://github.com/codespaces/badge.svg)](https://github.com/codespaces/new?hide_repo_select=true&ref=main&repo=Ivy-Interactive%2FIvy-Examples&machine=standardLinux32gb&devcontainer_path=.devcontainer%2Fmicrosoft-semantickernel%2Fdevcontainer.json&location=EuropeWest)

Launch a ready-to-code workspace with:
- **.NET 9.0** SDK pre-installed
- **Ivy tooling** available out of the box
- **Zero local setup** required

## Built With Ivy

This web application is powered by [Ivy-Framework](https://github.com/Ivy-Interactive/Ivy-Framework).

**Ivy** unifies front-end and back-end development in C#, enabling rapid internal tool development with AI-assisted workflows, typed components, and reactive UI primitives.

## AI-Powered Action Item Extraction

This demo showcases how to integrate [Microsoft Semantic Kernel](https://github.com/microsoft/semantic-kernel) with OpenAI to intelligently extract action items from any text using AI within an Ivy application.

### Features

- **Intelligent Text Analysis** – Uses GPT-4o-mini to understand context and extract actionable items
- **Flexible Input** – Works with meeting notes, emails, documents, or any text containing action items
- **Clean Output** – Displays extracted action items in a formatted, copyable code block
- **Real-time Processing** – Extract action items on-demand with a single button click
- **Error Handling** – Gracefully handles API errors and quota limits with user-friendly messages

### Configuration

The app reads the OpenAI API key from environment variables:
- `APIKEY` – Your OpenAI API key (required)

## Setting Up the API Key

Before running the application, you need to configure your OpenAI API key.

### Step 1: Get Your OpenAI API Key

1. **Sign up or log in** to [OpenAI Platform](https://platform.openai.com/)
2. Navigate to **API keys** section
3. Click **Create new secret key**
4. Copy your API key (starts with `sk-...`)

> **Important:** Never publish API keys in public repositories or share them with unauthorized parties.

### Step 2: Configure the API Key

For better security, use environment variables instead of storing the key in code.

**Windows PowerShell:**
```powershell
$env:APIKEY = "sk-your-api-key-here"
```

**Windows Command Prompt:**
```cmd
set APIKEY=sk-your-api-key-here
```

**Linux/macOS:**
```bash
export APIKEY="sk-your-api-key-here"
```

After setting the environment variable, run the app in the same console:
```bash
dotnet watch
```

## How to Run Locally

1. **Prerequisites:** .NET 9.0 SDK and an OpenAI API key
2. **Navigate to the project:**
   ```bash
   cd packages-demos/microsoft-semantickernel
   ```
3. **Set your API key** (see Step 2 above)
4. **Restore dependencies:**
   ```bash
   dotnet restore
   ```
5. **Start the app:**
   ```bash
   dotnet watch
   ```
6. **Open your browser** to the URL shown in the terminal (typically `http://localhost:5010`)

## Usage

1. **Enter your text** in the "Input Text" card – paste meeting notes, emails, or any text containing action items
2. **Click "Update tasks"** to extract action items using AI
3. **View results** in the "Extracted Action Items" card – formatted list with copy button
4. **Copy action items** using the copy button in the code block

## Deploy to Ivy Hosting

1. **Navigate to the example**
   ```bash
   cd packages-demos/microsoft-semantickernel
   ```
2. **Deploy**
   ```bash
   ivy deploy
   ```

## Learn More

- Microsoft Semantic Kernel: [github.com/microsoft/semantic-kernel](https://github.com/microsoft/semantic-kernel)
- OpenAI API Documentation: [platform.openai.com/docs](https://platform.openai.com/docs)
- Ivy documentation: [docs.ivy.app](https://docs.ivy.app)
