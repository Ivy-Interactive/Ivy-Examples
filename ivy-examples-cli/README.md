# Ivy Examples CLI

**Ivy Examples CLI** is the repo’s terminal companion: one **`ivy-examples`** command bundles shortcuts for Ivy-related work—fewer one-off scripts, more piping and automation. Run **`ivy-examples --help`** to see what ships today; the list grows with the examples we maintain. Command output is **YAML**.

## Features

- **Sliplane** — Control API for projects, servers, services, metrics, logs, domains, registry credentials, OAuth clients
- **Tendril** — Deploy instances and read status against a Tendril base URL
- **NuGet** — Ivy-related NuGet metrics: download counts, stars, summaries, and other statistics from the Ivy metrics pipeline (not a generic nuget.org browser)
- **Config** — `~/.ivy/config.json`; keys resolve from flags → env → prompt

> If another app needs CLI coverage, you can add a new command here when the need arises—the codebase is set up so new branches and commands drop in without ceremony.

## Command reference

Secrets and URLs: the CLI asks when needed and can **save** them to `~/.ivy/config.json`, or you set them with `ivy-examples config set` (see **Config**).

### Sliplane

**Needs:** A **Sliplane API secret** (and sometimes an org id for older tokens)—configure once; it can be stored in config so you are not prompted every run.

| Path | Actions |
|------|---------|
| `ivy-examples sliplane` | `me` |
| `ivy-examples sliplane projects` | `list`, `create`, `update`, `delete` |
| `ivy-examples sliplane servers` | `list`, `get`, `create`, `delete`, `rescale`, `metrics`, `volumes`, `create-volume` |
| `ivy-examples sliplane services` | `list`, `get`, `create`, `update`, `delete`, `pause`, `unpause`, `deploy`, `logs`, `metrics`, `events`, `add-domain`, `remove-domain` |
| `ivy-examples sliplane credentials` | `list`, `get`, `create`, `update`, `delete` |
| `ivy-examples sliplane oauth` | `list`, `get`, `update`, `users` |

Combine **Path** + **Actions** (e.g. `ivy-examples sliplane projects list`). See `ivy-examples sliplane --help` for flags.

### Tendril

**Needs:** **Tendril** URL (and API secret if your instance requires it). **Sliplane** secret is also required for every command here—same idea as the Sliplane section.

| Command | Description |
|---------|-------------|
| `ivy-examples tendril deploy` | Deploy a Tendril instance |
| `ivy-examples tendril status` | Service status |
| `ivy-examples tendril servers` | Servers available for deploy |
| `ivy-examples tendril projects` | Projects available for deploy |

### NuGet

**Needs:** Usually nothing—defaults hit the Ivy metrics host. If you use another instance, set its URL/secret once (config or prompt).

| Command | Description |
|---------|-------------|
| `ivy-examples nuget summary` | Overall stats |
| `ivy-examples nuget stars` | Star counts per package |
| `ivy-examples nuget starred` | Starred packages |
| `ivy-examples nuget unstarred` | Unstarred packages |
| `ivy-examples nuget downloads` | Download counts |
| `ivy-examples nuget downloads-history` | Downloads over time |

### Config

**Needs:** None—edits `~/.ivy/config.json` only.

| Command | Purpose |
|---------|---------|
| `ivy-examples config list` | Show saved keys (values may be masked) |
| `ivy-examples config get` | Read one key |
| `ivy-examples config set` | Save a key (values other branches reuse: Sliplane, Tendril, NuGet-stats, etc.) |
| `ivy-examples config unset` | Remove one key |
| `ivy-examples config clear` | Delete the whole file |

## Installation

### From this repo

```bash
cd ivy-examples-cli/src
dotnet pack -c Release -o ../nupkg
dotnet tool install -g --add-source ../nupkg Ivy.Examples.Cli
ivy-examples --help
```

### Development

```bash
cd ivy-examples-cli/src
dotnet run -- --help
```

## Updating the installed tool

If you installed with **From this repo** above, rebuild the package and refresh the global tool after code changes:

```bash
cd ivy-examples-cli/src
dotnet pack -c Release -o ../nupkg
dotnet tool update -g --add-source ../nupkg Ivy.Examples.Cli
```

