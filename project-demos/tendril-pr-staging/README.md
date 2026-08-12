# Tendril PR Staging

Ivy app that automatically deploys a [Tendril](https://github.com/Ivy-Interactive/Ivy-Tendril) instance on [Sliplane](https://sliplane.io) for every pull request opened on a GitHub repository. When a PR is opened, a dedicated Tendril staging service is created with Claude Code pre-configured and ready to use. A comment with the staging URL is posted to the PR. When the PR is closed or merged, the service is automatically deleted.

## Built With Ivy

This application is powered by [Ivy Framework](https://github.com/Ivy-Interactive/Ivy-Framework).

**Ivy** unifies front-end and back-end development in C#, enabling rapid internal tool development with AI-assisted workflows, typed components, and reactive UI primitives.

## Features

- **UI** — PR list with Deploy/Delete buttons, status, expiry, and direct links to the PR and staging Tendril
- **GitHub Webhook** — `POST /webhook/github`:
  - `pull_request` opened/reopened → auto-deploy
  - `pull_request` synchronize → redeploy on new commits
  - `pull_request` closed → delete staging service
  - `issue_comment` with `/deploy` or `/publish` → deploy on command
- **Auto-cleanup** — background job removes expired deployments whose PR is confirmed closed
- **PR comments** — posts or updates a comment in the PR with the staging Tendril URL
- **Claude Code ready** — each staging service gets `ANTHROPIC_AUTH_TOKEN` and `ANTHROPIC_BASE_URL` injected automatically

## Configuration

### Option 1: User Secrets (recommended for local development)

```bash
cd project-demos/tendril-pr-staging

# GitHub
dotnet user-secrets set "GitHub:Owner" "ArtemLazarchuk"
dotnet user-secrets set "GitHub:Repo" "Ivy-Tendril"
dotnet user-secrets set "GitHub:Token" "ghp_..."
dotnet user-secrets set "GitHub:WebhookSecret" "your_webhook_secret"

# Sliplane
dotnet user-secrets set "Sliplane:ApiToken" "api_rw_org_..."
dotnet user-secrets set "Sliplane:ProjectId" "project_..."
dotnet user-secrets set "Sliplane:ServerId" "server_..."

# Tendril — injected into each staging service
dotnet user-secrets set "Tendril:AnthropicAuthToken" "sk-..."
dotnet user-secrets set "Tendril:AnthropicBaseUrl" "https://llmproxy.ivy.app"
dotnet user-secrets set "Tendril:AuthUsername" "admin"
dotnet user-secrets set "Tendril:AuthPassword" "..."
```

### Option 2: Environment variables (Sliplane)

```
GitHub__Owner=ArtemLazarchuk
GitHub__Repo=Ivy-Tendril
GitHub__Token=ghp_...
GitHub__WebhookSecret=your_webhook_secret
Sliplane__ApiToken=api_rw_org_...
Sliplane__ProjectId=project_...
Sliplane__ServerId=server_...
Tendril__AnthropicAuthToken=sk-...
Tendril__AnthropicBaseUrl=https://llmproxy.ivy.app
Tendril__AuthUsername=admin
Tendril__AuthPassword=...
```

### Secret reference

| Key | Required | Description |
|-----|----------|-------------|
| `GitHub:Owner` | ✅ | GitHub organization or username |
| `GitHub:Repo` | ✅ | GitHub repository name |
| `GitHub:Token` | ✅ | Personal access token (`repo` scope) — posts PR comments and checks PR state |
| `GitHub:WebhookSecret` | ✅ | Secret used to verify incoming webhook requests from GitHub |
| `Sliplane:ApiToken` | ✅ | Sliplane API token — creates and deletes staging services |
| `Sliplane:ProjectId` | ✅ | Sliplane project where staging services are created |
| `Sliplane:ServerId` | ✅ | Sliplane server where staging services are deployed |
| `Tendril:AnthropicAuthToken` | ✅ | Anthropic proxy token — passed as `ANTHROPIC_AUTH_TOKEN` to each staging Tendril |
| `Tendril:AnthropicBaseUrl` | ✅ | Anthropic proxy URL — passed as `ANTHROPIC_BASE_URL` to each staging Tendril |
| `Tendril:AuthUsername` | ✅ | Login username for each staging Tendril instance |
| `Tendril:AuthPassword` | ✅ | Login password for each staging Tendril instance |
| `Tendril:AnthropicApiKey` | ❌ | Standard Anthropic API key — use instead of `AnthropicAuthToken` if not using a proxy |
| `Staging:ExpiryDays` | ❌ | Days before a staging service expires (default: `7`) |
| `Staging:PreDeployDelayMs` | ❌ | Delay in ms before deploying, to let GitHub finish processing the PR (default: `1200`) |
| `GitHub:DeployAllowedUsers` | ❌ | Comma-separated GitHub logins allowed to trigger deploys (default: all) |

## GitHub Webhook Setup

1. Go to your GitHub repository → **Settings → Webhooks → Add webhook**
2. **Payload URL:** `https://your-service.sliplane.app/webhook/github`
3. **Content type:** `application/json`
4. **Secret:** same value as `GitHub:WebhookSecret`
5. **Events:** select **Pull requests** and **Issue comments**

## How to Run Locally

1. **Prerequisites:** .NET 10 SDK, GitHub token, Sliplane API token
2. **Navigate:** `cd project-demos/tendril-pr-staging`
3. **Configure** — see Option 1 above
4. **Run:** `dotnet watch`

## Learn More

- [Ivy Framework](https://github.com/Ivy-Interactive/Ivy-Framework)
- [Ivy Documentation](https://docs.ivy.app)
- [Ivy on YouTube](https://www.youtube.com/@ivy-interactive)
- [Tendril](https://github.com/Ivy-Interactive/Ivy-Tendril)
- [Sliplane](https://sliplane.io)

## Tags

Tendril, PR Staging, Sliplane, GitHub, Deploy, Claude Code, Ivy Framework, C#, .NET
