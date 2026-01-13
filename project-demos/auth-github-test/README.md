# GitHub Authentication Test

A demonstration application showcasing GitHub OAuth authentication integration with the [Ivy Framework](https://github.com/Ivy-Interactive/Ivy).

This project demonstrates how to:
- Configure GitHub OAuth authentication in an Ivy application
- Display authenticated user information with a modern UI
- Fetch and display GitHub repositories using the GitHub API
- Implement search functionality with real-time filtering
- Handle authentication state and user sessions

## Features

- **GitHub OAuth Integration** - Secure authentication via GitHub
- **User Profile Display** - Shows authenticated user's avatar, name, email, and ID
- **Repository Management** - Displays user's GitHub repositories with detailed information
- **Search Functionality** - Real-time search to filter repositories by name or language
- **Modern UI Components** - Uses Sheet component for repository display and ToDetails for structured data
- **Smart Data Display** - Automatically hides empty fields and zero values for cleaner presentation
- **Real-time State Management** - React-like hooks for managing authentication state

## Prerequisites

1. **.NET 10.0 SDK** or later
2. **Ivy Framework** - This project uses local project references to Ivy Framework
   - Ensure you have the Ivy Framework cloned locally at: `C:\git\Ivy-Interactive\Ivy-Framework`
3. **GitHub OAuth App** - You'll need to create a GitHub OAuth application (see setup below)

## Setup

### 1. Create GitHub OAuth App

1. Go to [GitHub Developer Settings](https://github.com/settings/developers)
2. Click **"New OAuth App"**
3. Fill in the application details:
   - **Application name**: `Ivy GitHub Auth Test` (or any name you prefer)
   - **Homepage URL**: `http://localhost:5010` (or your app URL)
   - **Authorization callback URL**: `http://localhost:5010/ivy/webhook`
     - **Important**: The callback URL must exactly match (including protocol, port, and path)
4. Click **"Register application"**
5. Copy the **Client ID** and generate a **Client Secret**

### 2. Configure Application Secrets

Add your GitHub OAuth credentials using User Secrets (recommended for development):

```powershell
cd project-demos/auth-github-test
dotnet user-secrets set "GitHub:RedirectUri" "your-redirect-uri-here"
dotnet user-secrets set "GitHub:ClientId" "your-client-id-here"
dotnet user-secrets set "GitHub:ClientSecret" "your-client-secret-here"
```

Alternatively, you can create an `appsettings.json` file:

```json
{
  "GitHub": {
    "ClientId": "your-client-id-here",
    "ClientSecret": "your-client-secret-here",
    "RedirectUri": "your-redirect-uri-here"  
  }
}
```

## Run

1. **Navigate to the project directory**:
   ```bash
   cd project-demos/auth-github-test
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

5. **Authenticate**:
   - Click the login button in the navigation bar
   - You'll be redirected to GitHub for authorization
   - After authorizing, you'll be redirected back to the app
   - Your profile information will be displayed

6. **View Repositories**:
   - Click the "Repositories" button to open a sheet with your GitHub repositories
   - Use the search box to filter repositories by name or programming language
   - Click on any repository card to open it in GitHub
   - Repository details include language badge, stars, forks, and last updated date

## Deploy

Deploy this application to Ivy's hosting platform:

```bash
cd project-demos/auth-github-test
ivy deploy
```

## Project Structure

```
auth-github-test/
├── Apps/
│   └── TestAuthApp.cs      # Main application view with authentication UI
├── Program.cs               # Application entry point and configuration
├── GlobalUsings.cs          # Global using statements
├── Auth.GitHub.Test.csproj  # Project file
└── README.md               # This file
```

## Learn More

- **Ivy Framework**: [github.com/Ivy-Interactive/Ivy-Framework](https://github.com/Ivy-Interactive/Ivy-Framework)
- **Ivy Documentation**: [docs.ivy.app](https://docs.ivy.app)

## Tags

Authentication, OAuth, GitHub, Security, User Management, Ivy Framework, C#, .NET
