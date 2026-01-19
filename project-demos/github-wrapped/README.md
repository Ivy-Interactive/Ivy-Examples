# GitHub Wrapped 2025

## Description

GitHub Wrapped 2025 is a web application for visualizing and analyzing your GitHub coding activity from 2025. It displays your commits, pull requests, languages, repositories, and contribution statistics in an engaging, personalized slideshow format with animated visualizations and dynamic insights.

## One-Click Development Environment

[![Open in GitHub Codespaces](https://github.com/codespaces/badge.svg)](https://github.com/codespaces/new?hide_repo_select=true&ref=main&repo=Ivy-Interactive%2FIvy-Examples&machine=standardLinux32gb&devcontainer_path=.devcontainer%2Fgithub-wrapped%2Fdevcontainer.json&location=EuropeWest)

Click the badge above to open Ivy Examples repository in GitHub Codespaces with:
- **.NET 10.0** SDK pre-installed
- **Ready-to-run** development environment
- **No local setup** required

## Features

- **GitHub OAuth Authentication** - Secure login with your GitHub account
- **6 Interactive Slides**:
  1. **Welcome** - Personalized greeting that sets the storytelling tone (no stats, just anticipation)
  2. **Commits** - Animated commit counts with monthly breakdown and personalized insights
  3. **Pull Requests** - PR statistics with engaging narratives and animated numbers
  4. **Languages** - Top 7 programming languages with bar chart visualization (based on code bytes)
  5. **Repositories** - Clickable repository cards showing commit percentages and stats
  6. **Summary** - Dynamic status assignment, two-column layout with statistics, developer status, and top language cards
- **Animations** - Count-up animations for numbers, staggered card reveals
- **Wrapped-Style Design** - Engaging, personalized narratives instead of dry statistics
- **Stepper Navigation** - Easy navigation between slides with progress tracking
- **Footer Navigation** - Previous/Next buttons in footer with Share button on the last slide
- **Share Functionality** - Download and share your GitHub Wrapped summary as a PNG image
- **Interactive Elements** - Clickable repository cards that open GitHub pages
- **Responsive UI** - Clean, centered design with maximum width constraints

## Prerequisites

1. **.NET 10.0 SDK** or later
2. **Ivy Framework** - This project uses local project references to Ivy Framework
   - Ensure you have the Ivy Framework cloned locally at: `C:\git\Ivy-Interactive\Ivy-Framework`
3. **GitHub OAuth App** - You'll need to create a GitHub OAuth application (see setup below)

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
dotnet user-secrets set "GitHub:ClientId" "YOUR_CLIENT_ID"
dotnet user-secrets set "GitHub:ClientSecret" "YOUR_CLIENT_SECRET"
dotnet user-secrets set "GitHub:RedirectUri" "YOUR_REDIRECT_URI"
```

### 3. Run the Application

1. **Navigate to the project directory**:
   ```bash
   cd project-demos/github-wrapped
   ```

2. **Restore dependencies**:
   ```bash
   dotnet restore
   ```

3. **Run the application**:
   ```bash
   dotnet watch
   ```

4. **Open your browser** to the URL shown in the terminal (typically `http://localhost:5000`)

5. **Authenticate**:
   - Click the login button in the navigation bar
   - You'll be redirected to GitHub for authorization
   - After authorizing, you'll be redirected back to the app
   - Your GitHub Wrapped 2025 will be displayed

## How It Works

1. **Authentication**: Users log in with their GitHub account using OAuth
2. **Data Fetching**: The app fetches data from GitHub API:
   - User repositories with detailed language statistics (bytes per language)
   - Commits from 2025 (filtered by date)
   - Pull requests created and merged in 2025
   - Language statistics based on actual code bytes (not just repository primary language)
3. **Aggregation**: Statistics are calculated:
   - Monthly commit breakdown
   - Top 7 languages by code bytes (more accurate than commit-based counting)
   - Most active repositories with commit percentages
   - Contribution streak (longest consecutive days with commits)
   - Dynamic user status based on activity patterns
4. **Display**: Results are shown in a beautiful slideshow interface with:
   - Animated number count-ups
   - Personalized narratives and insights
   - Dynamic status assignment (Code Master, Productivity Champion, etc.)
   - Interactive elements (clickable repository cards)
   - Wrapped-style storytelling instead of raw statistics
   - Footer navigation with Previous/Next buttons
   - Share functionality to download summary as PNG image

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
- **Ivy.Charts** - Bar chart visualization for language statistics
- **GitHub REST API** - Data fetching (repositories, commits, pull requests, languages)
- **SkiaSharp** - Server-side image generation for shareable summary images
- **SkiaSharp.Extended.Svg** - SVG rendering in generated images
- **JobScheduler** - Coordinated animations for number count-ups
- **.NET 10.0** - Runtime platform

## Key Improvements

- **Better UI Structure**: Improved layout with footer navigation and enhanced visual hierarchy
- **Share Functionality**: Generate and download your GitHub Wrapped summary as a beautiful PNG image
- **Enhanced Summary Slide**: Redesigned two-column layout with statistics card, developer status card with trophy icon, and top language card
- **Accurate Language Statistics**: Uses actual code bytes from GitHub API instead of repository primary language
- **Top 7 Languages**: Shows more comprehensive language breakdown
- **Animated Numbers**: Smooth count-up animations for all statistics
- **Personalized Narratives**: Dynamic, engaging text based on user's actual activity patterns
- **Dynamic Status System**: Assigns statuses like "Code Master", "Productivity Champion", "Collaboration Hero" based on metrics
- **Interactive Repositories**: Clickable cards that open repository pages on GitHub
- **Wrapped-Style Welcome**: Storytelling approach that builds anticipation instead of showing stats immediately
- **GitHub API Optimizations**: Improved data fetching for commits, pull requests, and contribution days

## API Rate Limits

The GitHub API has rate limits. For authenticated requests, you get 5,000 requests per hour. The app is optimized to minimize API calls by:
- Fetching repositories with pagination (up to 10 pages, 100 repos per page)
- Limiting commit fetching to repositories with activity in 2025
- Fetching detailed language statistics only for active repositories
- Caching fetched data during navigation between slides
- Using efficient API endpoints (e.g., `/repos/{owner}/{repo}/languages` for language bytes)

## Deploy

Deploy this application to Ivy's hosting platform:

```bash
cd project-demos/github-wrapped
ivy deploy
```

## Learn More

- **Ivy Framework**: [github.com/Ivy-Interactive/Ivy-Framework](https://github.com/Ivy-Interactive/Ivy-Framework)
- **Ivy Documentation**: [docs.ivy.app](https://docs.ivy.app)

## Tags

GitHub, Wrapped, OAuth, Authentication, Statistics, Analytics, Data Visualization, Ivy Framework, C#, .NET

