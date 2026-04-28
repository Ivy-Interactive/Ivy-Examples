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
  - `pull_request` closed → delete staging services for that PR
  - `issue_comment` with `/deploy` → deploy on command
- **Auto-cleanup** — background job runs hourly; removes deployments only when **both** ExpiryDays passed (since deploy start) **and** PR is closed
- **PR comments (optional)** — after deploy, posts or updates **one** comment with Docs/Samples links (uses `GitHub:PrCommentToken`, or `GitHub:Token` if unset)
- **PR comment reactions (optional)** — bot can react with `rocket` to `/deploy` comments when configured

## Configuration

### Option 1: User Secrets (recommended for local development)

```bash
cd project-demos/pr-staging-deploy

# GitHub — legacy single-repo (simplest): PRs live in this repo
dotnet user-secrets set "GitHub:Owner" "your-org"
dotnet user-secrets set "GitHub:Repo" "your-repo"
dotnet user-secrets set "GitHub:Token" "ghp_your_token"

# Sliplane (required for deploy/delete)
dotnet user-secrets set "Sliplane:ApiToken" "api_rw_org_xxx"
dotnet user-secrets set "Sliplane:ProjectId" "project_xxx"
dotnet user-secrets set "Sliplane:ServerId" "server_xxx"

# Staging — optional (defaults shown). Omit Docs/Samples URLs to skip that target.
dotnet user-secrets set "Staging:SamplesRepo" "https://github.com/your-org/your-repo"
dotnet user-secrets set "Staging:DocsRepo" "https://github.com/your-org/your-repo"
dotnet user-secrets set "Staging:SamplesDockerContext" "."
dotnet user-secrets set "Staging:DocsDockerContext" "."
dotnet user-secrets set "Staging:SamplesDockerfile" ".github/docker/Dockerfile.samples"
dotnet user-secrets set "Staging:DocsDockerfile" ".github/docker/Dockerfile.docs"
dotnet user-secrets set "Staging:ExpiryDays" "7"

# Optional: first segment of Sliplane service names — default "ivy" → names start with "ivy-staging-…".
# Set your own prefix per deployment (e.g. "tendril" → "tendril-staging-…-docs-…").
# dotnet user-secrets set "Staging:DeploymentKey" "tendril"

# Webhook (optional — auto-deploy on PR open/update/close)
dotnet user-secrets set "GitHub:WebhookSecret" "your_webhook_secret"

# Optional: comma-separated GitHub logins. PR auto-deploy and `/deploy` can be restricted.
dotnet user-secrets set "GitHub:DeployAllowedUsers" "alice,bob"

# Optional: PAT for PR comments (Issues API). If omitted, GitHub:Token is used.
dotnet user-secrets set "GitHub:PrCommentToken" "ghp_xxx"
```

If **`Repos`** is empty, the app builds one staging entry from **`GitHub:Owner`**, **`GitHub:Repo`**, and **`Staging:*`** as above.

### Multi-repo (`Repos[]`)

For several GitHub repos in one app, use a **`Repos`** array (JSON in `appsettings` or flat keys like `Repos:0:Owner`, `Repos:0:Repo`, `Repos:0:Docs:Repo`, …). Each row can enable Docs only, Samples only, or both. See comments in code and `StagingRepoConfig` for the full shape.

### Option 2: Environment variables (Sliplane)

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
# Optional — prefix before "-staging-" in service names (default ivy). Example: tendril → tendril-staging-…
# Staging__DeploymentKey=tendril
GitHub__WebhookSecret=your_webhook_secret
GitHub__DeployAllowedUsers=alice,bob
GitHub__PrCommentToken=ghp_xxx
```

Legacy keys above apply when **`Repos__0__*`** is not set; otherwise prefer **`Repos__0__Owner`**, **`Repos__0__Repo`**, **`Repos__0__Docs__*`** / **`Repos__0__Samples__*`**, etc.

### Service names (full pattern)

One slug at the start only:

`{slug}-staging-docs-{prNumber}` and `{slug}-staging-samples-{prNumber}`.

- **`slug`** = **`Staging:DeploymentKey`** (sanitized) when that value is set — shared by every repo in this app.
- If **`Staging:DeploymentKey`** is **not** set, **`slug`** = each entry’s **`Repos[].Key`** (or legacy sanitized **`GitHub:Repo`**).

Examples:

- `Staging:DeploymentKey` = `ivy-tendril` → **`ivy-tendril-staging-docs-1`** (not `ivy-tendril-staging-ivy-tendril-docs-1`).
- No deployment key, repo key `my-app` → **`my-app-staging-samples-42`**.

Older deployments may still use the previous `{deploymentKey}-staging-{repoKey}-docs-{pr}` shape; the app still recognizes those until you recreate services.

### Who can trigger deploy (webhooks)

| Event | Who must be on the list (when `DeployAllowedUsers` is set) |
|--------|-----------------------------------------------------------|
| PR opened / reopened / synchronize | **PR author** |
| Comment `/deploy` | **Comment author** |

If `DeployAllowedUsers` is **empty**, user checks are skipped.

### GitHub Webhook

1. GitHub → Settings → Webhooks → Add webhook  
2. **Payload URL**: `https://your-domain.com/webhook`  
3. **Content type**: `application/json`  
4. **Secret**: match `GitHub:WebhookSecret`  
5. **Events**: Pull requests, Issue comments  

**Troubleshooting:** 401 on delivery usually means the webhook secret does not match the app config.

## How to Run Locally

1. **Prerequisites:** .NET 10.0 SDK, GitHub token, Sliplane API token  
2. **Navigate:** `cd project-demos/pr-staging-deploy`  
3. **Restore:** `dotnet restore`  
4. **Configure** — Option 1 above (`dotnet user-secrets set …`)  
5. **Run:** `dotnet watch`  
6. Open the URL from the terminal (often `http://localhost:5010`)

## Deploy to Ivy Hosting

```bash
cd project-demos/pr-staging-deploy
ivy deploy
```

Set environment variables on Sliplane (Option 2).

## Learn More

- [Sliplane](https://sliplane.io)
- [GitHub API](https://docs.github.com/en/rest)
- [Ivy documentation](https://docs.ivy.app)

## Tags

PR Staging, Sliplane, GitHub, Deploy, Docs, Samples, Ivy Framework, C#, .NET
