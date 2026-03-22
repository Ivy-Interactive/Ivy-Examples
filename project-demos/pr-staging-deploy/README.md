# PR Staging Deploy

## Description

PR Staging Deploy is a backend service for deploying docs and samples to Sliplane for each pull request. It provides a UI to manage PR deployments, listens to GitHub webhooks for automatic deploy/redeploy/delete, and runs a background job to remove deployments older than 7 days.

## Live Demo

[![Live Demo](https://img.shields.io/badge/Live%20Demo-PR%20Staging%20Deploy-blue?style=for-the-badge)](https://ivy-pr-staging-deploy.sliplane.app)

## Built With Ivy

This application is powered by [Ivy Framework](https://github.com/Ivy-Interactive/Ivy-Framework).

**Ivy** unifies front-end and back-end development in C#, enabling rapid internal tool development with AI-assisted workflows, typed components, and reactive UI primitives.

## Features

- **UI** — PR list with Deploy/Delete buttons, status icons, Docs/Samples URLs
- **GitHub Webhook** — `POST /webhook`:
  - `pull_request` opened/reopened → auto-deploy
  - `pull_request` synchronize → redeploy
  - `pull_request` closed → deployment kept (no immediate delete)
  - `issue_comment` with `/deploy` → deploy on command
- **Auto-cleanup** — background job runs hourly; removes deployments only when **both** ExpiryDays passed (since deploy start) **and** PR is closed

## Configuration

### Option 1: User Secrets (Recommended for Local Development)

```bash
cd project-demos/pr-staging-deploy

# GitHub (required for /deploy comments and expiry cleanup)
dotnet user-secrets set "GitHub:Owner" "your-org"
dotnet user-secrets set "GitHub:Repo" "your-repo"
dotnet user-secrets set "GitHub:Token" "ghp_your_token"

# Sliplane (required for deploy/delete)
dotnet user-secrets set "Sliplane:ApiToken" "api_rw_org_xxx"
dotnet user-secrets set "Sliplane:ProjectId" "project_xxx"
dotnet user-secrets set "Sliplane:ServerId" "server_xxx"

# Staging (optional – defaults shown)
dotnet user-secrets set "Staging:SamplesRepo" "https://github.com/your-org/your-repo"
dotnet user-secrets set "Staging:DocsRepo" "https://github.com/your-org/your-repo"
dotnet user-secrets set "Staging:SamplesDockerContext" "."
dotnet user-secrets set "Staging:DocsDockerContext" "."
dotnet user-secrets set "Staging:SamplesDockerfile" ".github/docker/Dockerfile.samples"
dotnet user-secrets set "Staging:DocsDockerfile" ".github/docker/Dockerfile.docs"
dotnet user-secrets set "Staging:ExpiryDays" "7"

# Webhook (optional – for auto-deploy on PR open/update/close)
dotnet user-secrets set "GitHub:WebhookSecret" "your_webhook_secret"

# Optional: comma-separated GitHub logins (case-insensitive). If empty, no user-based restriction.
# Auto-deploy (PR open / push): only if the PR author is in this list.
# /deploy comment: only if the comment author is in this list (PR author may be anyone).
dotnet user-secrets set "GitHub:DeployAllowedUsers" "alice,bob"
```

### Who can trigger deploy (webhooks)

| Event | Who must be on the list (when `DeployAllowedUsers` is set) |
|--------|-----------------------------------------------------------|
| PR opened / reopened / synchronize | **PR author** (whose PR gets auto-deploy) |
| Comment `/deploy` | **Comment author** (so a maintainer on the list can deploy someone else’s PR) |

If `DeployAllowedUsers` is **empty**, GitHub user checks are skipped (same as before). Closing a PR is **not** gated by this list. The in-app **Deploy** buttons are not tied to GitHub identity — restrict the app URL separately if needed.

### Option 2: Environment Variables (for Sliplane Deployment)

When deploying to Sliplane, set these environment variables:

```
GitHub__Owner=your-org
GitHub__Repo=your-repo
GitHub__Token=ghp_xxx
Sliplane__ApiToken=api_rw_org_xxx
Sliplane__ProjectId=project_xxx
Sliplane__ServerId=server_xxx
Staging__SamplesRepo=https://github.com/your-org/your-repo
Staging__DocsRepo=https://github.com/your-org/your-repo
Staging__SamplesDockerContext=.
Staging__DocsDockerContext=.
Staging__SamplesDockerfile=.github/docker/Dockerfile.samples
Staging__DocsDockerfile=.github/docker/Dockerfile.docs
Staging__ExpiryDays=7
GitHub__DeployAllowedUsers=alice,bob
```

### GitHub Webhook (Optional)

Webhook notifies the server; deploy is triggered only by `/deploy` comment or UI (no auto-deploy on PR open):

1. GitHub → Settings → Webhooks → Add webhook
2. **Payload URL**: `https://your-domain.com/webhook`
3. **Content type**: `application/json`
4. **Secret**: `openssl rand -hex 32`
5. **Events**: Pull requests, Issue comments
6. Add to config: `GitHub:WebhookSecret`

- **`/deploy`** in PR comment → deploy (requires `GitHub:Token`)
- **PR closed** → deployment stays until ExpiryDays + closed PR (cleanup job removes it)

**Auto-deploy not working?** Check:
1. **GitHub** → Webhooks → Recent Deliveries: did `pull_request` get **200**? (401 = secret mismatch)
2. **Sliplane logs**: look for `"PR #X opened"` and `"Deploy result"` — if missing, webhook may not reach the app
3. **Env vars** on Sliplane: `Sliplane__ApiToken`, `Sliplane__ProjectId`, `Sliplane__ServerId` must be set
4. **Staging repos**: `Staging__SamplesRepo` and `Staging__DocsRepo` must point to the repo where the PR branch exists (e.g. same repo as the webhook)

**Troubleshooting 401:** If GitHub shows "Invalid HTTP Response: 401", the webhook secret doesn't match. Either:
- Remove `GitHub__WebhookSecret` from Sliplane (temporarily disables verification), or
- Regenerate the secret and set the same value in both GitHub and Sliplane.

## How to Run Locally

1. **Prerequisites:** .NET 10.0 SDK, GitHub token, Sliplane API token
2. **Navigate to the project:**
   ```bash
   cd project-demos/pr-staging-deploy
   ```
3. **Restore dependencies:**
   ```bash
   dotnet restore
   ```
4. **Configure credentials** (see Option 1 above):
   ```bash
   dotnet user-secrets set "GitHub:Owner" "your-org"
   dotnet user-secrets set "GitHub:Repo" "your-repo"
   dotnet user-secrets set "GitHub:Token" "ghp_xxx"
   dotnet user-secrets set "Sliplane:ApiToken" "api_xxx"
   dotnet user-secrets set "Sliplane:ProjectId" "project_xxx"
   dotnet user-secrets set "Sliplane:ServerId" "server_xxx"
   ```
5. **Start the app:**
   ```bash
   dotnet watch
   ```
6. **Open your browser** to the URL shown in the terminal (typically `http://localhost:5010`)

## Deploy to Ivy Hosting

1. **Navigate to the project:**
   ```bash
   cd project-demos/pr-staging-deploy
   ```
2. **Deploy:**
   ```bash
   ivy deploy
   ```
3. **Configure environment variables** in your Sliplane deployment (see Option 2 above).

## Learn More

- [Sliplane](https://sliplane.io) — Deploy and manage containers
- [GitHub API](https://docs.github.com/en/rest) — Pull requests, webhooks
- [Ivy documentation](https://docs.ivy.app)

## Tags

PR Staging, Sliplane, GitHub, Deploy, Docs, Samples, Ivy Framework, C#, .NET
