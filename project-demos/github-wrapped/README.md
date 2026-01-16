# GitHub Wrapped 2025

A Spotify Wrapped-style application for GitHub that displays your 2025 coding activity in an engaging slideshow format.

## Features

- 🔐 **GitHub OAuth Authentication** - Secure login with your GitHub account
- 📊 **6 Interactive Slides**:
  1. **Welcome** - Greeting with key stats overview
  2. **Commits** - Total commits with monthly breakdown
  3. **Pull Requests** - PRs created, merged, and merge rate
  4. **Languages** - Top 5 programming languages used
  5. **Repositories** - Most active repositories by commit count
  6. **Summary** - Overall 2025 highlights and contribution streak
- 🎯 **Stepper Navigation** - Easy navigation between slides
- 📱 **Responsive UI** - Clean, minimal design

## Prerequisites

- .NET 10.0 SDK
- GitHub OAuth Application credentials

## Setup

### 1. Create a GitHub OAuth Application

1. Go to [GitHub Developer Settings](https://github.com/settings/developers)
2. Click "New OAuth App"
3. Fill in the details:
   - **Application name**: GitHub Wrapped 2025
   - **Homepage URL**: `http://localhost:5000`
   - **Authorization callback URL**: `http://localhost:5000/auth/github/callback`
4. Click "Register application"
5. Note down your **Client ID** and generate a **Client Secret**

### 2. Configure User Secrets

```bash
cd project-demos/github-wrapped
dotnet user-secrets init
dotnet user-secrets set "Auth:GitHub:ClientId" "YOUR_CLIENT_ID"
dotnet user-secrets set "Auth:GitHub:ClientSecret" "YOUR_CLIENT_SECRET"
```

### 3. Run the Application

```bash
dotnet run
```

The application will open in your browser. Click the login button in the top right corner to authenticate with GitHub.

## How It Works

1. **Authentication**: Users log in with their GitHub account using OAuth
2. **Data Fetching**: The app fetches data from GitHub API:
   - User repositories
   - Commits from 2025 (filtered by date)
   - Pull requests created in 2025
   - Language statistics
3. **Aggregation**: Statistics are calculated:
   - Monthly commit breakdown
   - Top languages by commit count
   - Most active repositories
   - Contribution streak (longest consecutive days)
4. **Display**: Results are shown in a beautiful slideshow interface using the Stepper widget

## Architecture

```
GitHubWrapped/
├── Apps/
│   ├── GitHubWrappedApp.cs          # Main application with Stepper
│   └── Views/                        # Individual slide components
│       ├── WelcomeSlide.cs
│       ├── CommitsSlide.cs
│       ├── PullRequestsSlide.cs
│       ├── LanguagesSlide.cs
│       ├── RepositoriesSlide.cs
│       └── SummarySlide.cs
├── Models/
│   └── GitHubStats.cs                # Data models
├── Services/
│   └── GitHubStatsService.cs         # GitHub API integration
├── Program.cs                         # Application entry point
└── GlobalUsings.cs                    # Global using directives
```

## Technologies Used

- **Ivy Framework** - UI framework for building interactive applications
- **Ivy.Auth.GitHub** - GitHub OAuth authentication
- **GitHub REST API** - Data fetching
- **.NET 10.0** - Runtime platform

## API Rate Limits

The GitHub API has rate limits. For authenticated requests, you get 5,000 requests per hour. The app is optimized to minimize API calls by:
- Limiting repository fetching to the 50 most recent repos
- Limiting commit fetching to 3 pages per repository
- Caching fetched data during navigation

## License

Part of the Ivy-Examples repository.
