# GitHub

<img width="1917" height="909" alt="image" src="https://github.com/user-attachments/assets/c93323e1-3625-45ac-a185-4c98dce3239e" />

## One-Click Development Environment

[![Open in GitHub Codespaces](https://github.com/codespaces/badge.svg)](https://github.com/codespaces/new?hide_repo_select=true&ref=main&repo=Ivy-Interactive%2FIvy-Examples&machine=standardLinux32gb&devcontainer_path=.devcontainer%2Fgithub%2Fdevcontainer.json&location=EuropeWest)

Click the badge above to open Ivy Examples repository in GitHub Codespaces with:
- **.NET 9.0** SDK pre-installed
- **Ready-to-run** development environment
- **No local setup** required

## Created Using Ivy

Web application created using [Ivy-Framework](https://github.com/Ivy-Interactive/Ivy-Framework).

**Ivy** - The ultimate framework for building internal tools with LLM code generation by unifying front-end and back-end into a single C# codebase. With Ivy, you can build robust internal tools and dashboards using C# and AI assistance based on your existing database.

Ivy is a web framework for building interactive web applications using C# and .NET.

## Interactive GitHub User Statistics

This example demonstrates GitHub user statistics retrieval using the [GitHub REST API](https://docs.github.com/en/rest) integrated with Ivy. The application fetches comprehensive user data and displays it in an elegant, interactive interface.

**What This Application Does:**

This specific implementation creates a **GitHub User Analytics** application that allows users to:

- **Search GitHub Users**: Enter any GitHub username to fetch their profile and statistics
- **View User Profile**: Display user avatar, name, username, and basic profile information
- **Analyze Statistics**: Show comprehensive stats including stars, commits, pull requests, and issues
- **Mock Data Testing**: Use 'example' username for instant demo with sample data
- **Real-time API Integration**: Fetch live data from GitHub's REST API
- **Interactive UI**: Split-panel layout with search controls on the left and user stats on the right
- **Toast Notifications**: Success and error feedback for all operations
- **Copy to Clipboard**: Easy copying of usernames and names

**Technical Implementation:**

- Uses GitHub REST API endpoints for user data and statistics
- Implements mock data system for testing and demonstration
- Creates responsive card-based layout with avatar display
- Handles API rate limiting and error scenarios gracefully
- Supports both real GitHub usernames and mock data ('example')
- Generates comprehensive user statistics including:
  - Total stars earned across repositories
  - Commits made in the last year
  - Pull requests created
  - Issues opened
  - Repositories contributed to
  - Public repositories count
  - Followers and following counts

## How to Run

1. **Prerequisites**: .NET 9.0 SDK
2. **Navigate to the example**:
   ```bash
   cd github
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
   cd github
   ```
2. **Deploy to Ivy hosting**:
   ```bash
   ivy deploy
   ```
This will deploy your GitHub stats application with a single command.

## Usage Examples

**Try these usernames:**
- `torvalds` - Linux creator Linus Torvalds
- `octocat` - GitHub's mascot
- `example` - Mock data for testing
- Any real GitHub username

**Features:**
- **Real-time data**: Fetches live statistics from GitHub
- **Mock testing**: Use 'example' for instant demo
- **Error handling**: Graceful handling of invalid usernames
- **Responsive design**: Works on desktop and mobile
- **Copy functionality**: Click to copy usernames and names

## Learn More

- GitHub REST API Documentation: [docs.github.com/en/rest](https://docs.github.com/en/rest)
- Ivy Documentation: [docs.ivy.app](https://docs.ivy.app)
- GitHub API Rate Limiting: [docs.github.com/en/rest/overview/rate-limits](https://docs.github.com/en/rest/overview/rate-limits)