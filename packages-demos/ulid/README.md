# ULID

<img width="1919" height="915" alt="image" src="https://github.com/user-attachments/assets/0e9900a4-49c2-42be-9392-82a590c9a00b" />

<img width="1915" height="911" alt="image" src="https://github.com/user-attachments/assets/8e23fb99-f80e-43f3-821b-1eaf29e6a1f0" />

## One-Click Development Environment

[![Open in GitHub Codespaces](https://github.com/codespaces/badge.svg)](https://github.com/codespaces/new?hide_repo_select=true&ref=main&repo=Ivy-Interactive%2FIvy-Examples&machine=standardLinux32gb&devcontainer_path=.devcontainer%2Fulid%2Fdevcontainer.json&location=EuropeWest)

Click the badge above to open Ivy Examples repository in GitHub Codespaces with:
- **.NET 9.0** SDK pre-installed
- **Ready-to-run** development environment
- **No local setup** required

## Created Using Ivy

Web application created using [Ivy-Framework](https://github.com/Ivy-Interactive/Ivy-Framework).

**Ivy** - The ultimate framework for building internal tools with LLM code generation by unifying front-end and back-end into a single C# codebase. With Ivy, you can build robust internal tools and dashboards using C# and AI assistance based on your existing database.

Ivy is a web framework for building interactive web applications using C# and .NET.

## Interactive Example For ULID Operations

This example demonstrates ULID (Universally Unique Lexicographically Sortable Identifier) generation and parsing using the [Ulid library](https://github.com/Cysharp/Ulid) integrated with Ivy. ULID is a 26-character string that combines timestamp and randomness, providing a unique identifier that is sortable by creation time.

**What This Application Does:**

This specific implementation creates a **ULID Generator and Parser** application that allows users to:

- **Generate ULIDs**: Create new ULIDs with a single click, each containing a timestamp and random component
- **Parse ULIDs**: Validate and extract information from existing ULID strings
- **View Timestamps**: Display both UTC and local timestamps extracted from ULIDs
- **Copy to Clipboard**: Easily copy ULID values and timestamps using built-in copy buttons
- **Interactive Feedback**: Receive success and error notifications via toast messages and callouts
- **Structured Display**: View generated and parsed ULIDs in organized code blocks with labels
- **Mode Selection**: Switch between Generate and Parse modes using a dropdown selector

**Technical Implementation:**

- Uses Ulid library's `Ulid.NewUlid()` for generating unique identifiers
- Implements `Ulid.TryParse()` for robust ULID validation and parsing
- Extracts timestamp information using `ulid.Time` property
- Displays timestamps in ISO 8601 format (O format string)
- Provides structured UI with Code blocks and copy functionality
- Implements toast notifications for user feedback
- Uses Success, Error, and Info callouts for different states
- Handles empty input validation with appropriate error messages
- Creates responsive card layout with fractional width sizing

**Key Features:**

- **Generate Mode**: 
  - One-click ULID generation
  - Display generated ULID in a copyable code block
  - Show UTC timestamp with copy button
  - Success toast notification on generation

- **Parse Mode**:
  - Input field for ULID string
  - Parse button with full-width styling
  - Display parsed ULID information in structured format
  - Show UTC and Local timestamps
  - Success callout on successful parsing
  - Error callout for invalid ULIDs
  - Info callout with instructions

## How to Run

1. **Prerequisites**: .NET 9.0 SDK
2. **Navigate to the example**:
   ```bash
   cd packages-demos/ulid
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
   cd packages-demos/ulid
   ```
2. **Deploy to Ivy hosting**:
   ```bash
   ivy deploy
   ```
This will deploy your ULID generator and parser application with a single command.

## Learn More

- Ulid library for .NET overview: [github.com/Cysharp/Ulid](https://github.com/Cysharp/Ulid)
- Ivy Documentation: [docs.ivy.app](https://docs.ivy.app)
