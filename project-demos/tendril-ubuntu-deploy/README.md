# tendril-ubuntu-deploy

One-click deploy of a **full Ubuntu 22.04 Desktop** environment to [Sliplane](https://sliplane.io), powered by Ivy.

## What you get

| Component | Details |
|---|---|
| OS | Ubuntu 22.04 LTS |
| Desktop | XFCE4 |
| Browser access | noVNC (HTTP — via Sliplane managed domain) |
| RDP access | xrdp on port 3389 (native RDP client) |
| IDE | JetBrains Rider (pre-installed at `/opt/rider`) |
| Dev tool | [Ivy Tendril](https://github.com/Ivy-Interactive/Ivy-Tendril) (`tendril` CLI) |

## Flow

```
Sign in with Sliplane
  ↓
Pick a server + enter a service name
  ↓
Set RDP username + password
  ↓
Deploy → Sliplane builds the image (~5-10 min first time)
  ↓
Get: browser URL (noVNC) + IP:3389 (RDP) + credentials
```

## Running the deploy wizard locally

```bash
cd project-demos/tendril-ubuntu-deploy
dotnet run
# Open http://localhost:5021
```

Sign in with Sliplane (OAuth) or set an API token in user-secrets:

```bash
dotnet user-secrets set "Sliplane:ApiToken" "your-token-here"
```

## Ubuntu Desktop Docker image

The image is built from `docker/Dockerfile.ubuntu-desktop` when Sliplane deploys the service.

**Runtime environment variables** (set by the wizard, can be changed in Sliplane later):

| Variable | Default | Description |
|---|---|---|
| `RDP_USER` | `developer` | Linux username inside the container |
| `RDP_PASSWORD` | — | Password (sent as a Sliplane secret) |
| `PORT` | `8080` | noVNC HTTP port (Sliplane health-check target) |

**Persistent storage:** a Sliplane volume is auto-created and mounted at `/home/<RDP_USER>`. Home directory survives container restarts.

## Connecting

### Browser (noVNC)
Open the managed domain shown in the status view — e.g. `https://ubuntu-dev.sliplane.app/`.  
Enter your password at the noVNC prompt.

### RDP (native client)
- **Windows:** built-in Remote Desktop Connection (`mstsc`)  
- **macOS:** [Microsoft Remote Desktop](https://apps.apple.com/app/microsoft-remote-desktop/id1295203466)  
- **Linux:** Remmina or any other RDP client

Address: `<server-ip>:3389`  
Username / password: the values you set in the wizard.

> **Firewall note:** Sliplane servers may need port 3389 open in the server's firewall settings for direct RDP access. noVNC (browser) always works via the managed domain.

## Customising the image

Fork [Ivy-Examples](https://github.com/Ivy-Interactive/Ivy-Examples), edit `docker/Dockerfile.ubuntu-desktop`, then point the wizard at your fork's URL.

To bump the Rider version, change the `ARG RIDER_VERSION` line in `Dockerfile.ubuntu-desktop`.
