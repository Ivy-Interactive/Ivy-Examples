# Sliplane Manage

## Description

Sliplane Manage is a web application for managing your [Sliplane](https://sliplane.io) infrastructure: servers, projects, and services. It uses the Sliplane Control API to list and manage resources, deploy services from Git repositories or Docker images, view logs and events, and perform pause/resume and delete operations—all through an interactive UI built with the Ivy framework.

## Features

- **Overview** – Dashboard with summary cards (Servers, Projects, Services) and counts; use the sidebar to jump to each section.
- **Servers** – List servers with metrics (CPU, memory), view volumes in a table, open server details in a sheet; delete with confirmation; floating “Add server” button linking to Sliplane.
- **Projects** – List projects as cards with service counts; click a project to open a dialog with services table; row actions: View, Edit, Logs, Events (each opens a sheet).
  - **View** – Full read-only service details (Basic, Deployment, Network, Domains) with footer actions: Edit, Pause/Resume, Delete.
  - **Edit** – Sheet to PATCH service (name, deployment, cmd, healthcheck, env vars).
  - **Logs** – Sheet with service logs (code block).
  - **Events** – Sheet with events table (Time, Type, Message).
  - **Create service** – Floating “Add project” panel and full Create service sheet (Basic, Deployment, Network, Optional, Env, Volumes) with server/volume selection.
- **Add project** – Floating panel button (bottom-right) opens a dialog to create a new project (name only; API: `POST /projects`).
- **Services** – Select project and server via AsyncSelect; list services with status; create service (full sheet), open service details (Edit, Pause/Resume, Delete), view logs.

## Prerequisites

1. **.NET 10.0 SDK** or later  
2. **Ivy Framework** – The project uses local project references to the Ivy Framework.  
   Ensure the framework is available (e.g. cloned at `C:\git\Ivy-Interactive\Ivy-Framework` or as configured in the solution).  
3. **Sliplane API access** – Either:
   - **Sliplane OAuth** – Sign in with a Sliplane account (Ivy.Auth.Sliplane), or  
   - **API token** – Set `Sliplane:ApiToken` in configuration or user secrets.

## Setup

### 1. Configure API token (optional)

If you are not using Sliplane OAuth, provide your Sliplane API token:

```bash
cd project-demos/sliplane-manage
dotnet user-secrets init
dotnet user-secrets set "Sliplane:ApiToken" "YOUR_SLIPLANE_API_TOKEN"
```

You can also set `Sliplane:ApiToken` in `appsettings.json` or environment variables.

### 2. Run the application

1. **Go to the project directory**:
   ```bash
   cd project-demos/sliplane-manage
   ```

2. **Restore dependencies**:
   ```bash
   dotnet restore
   ```

3. **Run the app**:
   ```bash
   dotnet watch
   ```

4. **Open the URL** shown in the terminal (e.g. `http://localhost:5000`).

5. **Authenticate** – Sign in with Sliplane (OAuth) or rely on `Sliplane:ApiToken` if configured.

## How it works

1. **Authentication** – The app uses Ivy.Auth.Sliplane for OAuth or reads `Sliplane:ApiToken` from configuration.  
2. **API client** – `SliplaneApiClient` calls the Sliplane Control API (`https://ctrl.sliplane.io/v0`) for projects, servers, services, logs, events, volumes, and create/update/delete/pause/unpause operations.  
3. **Views** – Sidebar apps (Overview, Servers, Projects, Services) each render a view that loads data via `UseEffect` / `UseQuery`, and use sheets and dialogs for details, edit, create, logs, and events.  
4. **State** – Lists are refreshed after create/update/delete; service details and logs/events load when the corresponding sheet is opened.

## Architecture

```
SliplaneManage/
├── Apps/
│   ├── SliplaneOverviewApp.cs    # Overview hub (cards)
│   ├── SliplaneServersApp.cs      # Servers app
│   ├── SliplaneProjectsApp.cs    # Projects app
│   ├── SliplaneServicesApp.cs     # Services app
│   └── Views/
│       ├── ServersView.cs         # Server list, details sheet, volumes
│       ├── ProjectsView.cs       # Project cards, service table, View/Edit/Logs/Events/Create sheets
│       └── ServicesView.cs       # Service list, create/details/edit sheets
├── Models/
│   └── SliplaneModels.cs         # DTOs (projects, servers, services, logs, events, etc.)
├── Services/
│   └── SliplaneApiClient.cs       # HTTP client for Sliplane Control API
├── Program.cs                     # Server, auth, chrome, apps
└── AGENTS.md                      # Context for AI agents (Ivy + project structure)
```

## Technologies used

- **Ivy** – UI framework for interactive C#/.NET web apps  
- **Ivy.Auth.Sliplane** – Sliplane OAuth authentication  
- **Sliplane Control API** – REST API for projects, servers, services, logs, events, volumes  
- **.NET 10.0** – Runtime and SDK  

## Deploy

Deploy to Ivy hosting from the project directory:

```bash
cd project-demos/sliplane-manage
ivy deploy
```

## Learn more

- **Ivy Framework**: [github.com/Ivy-Interactive/Ivy-Framework](https://github.com/Ivy-Interactive/Ivy-Framework)  
- **Ivy docs**: [docs.ivy.app](https://docs.ivy.app)  
- **Sliplane**: [sliplane.io](https://sliplane.io)  

> **Note**  
> This project includes an `AGENTS.md` file with context for AI agents (e.g. Cursor, Copilot) about the Ivy framework and this repo’s structure.

## Tags

Sliplane, DevOps, Servers, Projects, Services, Ivy Framework, C#, .NET, API, OAuth
