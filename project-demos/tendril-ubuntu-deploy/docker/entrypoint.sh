#!/bin/bash
set -e

# ─── Runtime defaults ────────────────────────────────────────────────────────
RDP_USER="${RDP_USER:-developer}"
RDP_PASSWORD="${RDP_PASSWORD:-TendrilRDP!1}"
PORT="${PORT:-8080}"
VNC_DISPLAY=":1"
VNC_PORT="5901"
GEOMETRY="${VNC_GEOMETRY:-1920x1080}"
DEPTH="${VNC_DEPTH:-24}"

# VNC passwords are limited to 8 chars by the protocol; truncate silently.
VNC_PASSWORD="${RDP_PASSWORD:0:8}"

echo "[entrypoint] Starting Ubuntu Desktop"
echo "[entrypoint] User: ${RDP_USER} | noVNC port: ${PORT} | Geometry: ${GEOMETRY}"

# ─── 1. Create / update user ─────────────────────────────────────────────────
if id "${RDP_USER}" &>/dev/null; then
    echo "[entrypoint] User '${RDP_USER}' exists — updating password."
else
    echo "[entrypoint] Creating user '${RDP_USER}'."
    useradd --create-home --shell /bin/bash "${RDP_USER}"
fi

echo "${RDP_USER}:${RDP_PASSWORD}" | chpasswd

# Passwordless sudo for convenience in a dev environment
echo "${RDP_USER} ALL=(ALL) NOPASSWD:ALL" > "/etc/sudoers.d/${RDP_USER}"
chmod 440 "/etc/sudoers.d/${RDP_USER}"

HOME_DIR="/home/${RDP_USER}"

# ─── 2. Session files ─────────────────────────────────────────────────────────
echo "startxfce4" > "${HOME_DIR}/.xsession"
chmod 755 "${HOME_DIR}/.xsession"
chown "${RDP_USER}:${RDP_USER}" "${HOME_DIR}/.xsession"

# Make dotnet tools and Rider available in the user's shell
cat >> "${HOME_DIR}/.bashrc" << 'EOF'
export DOTNET_ROOT=/usr/share/dotnet
export PATH="$PATH:/root/.dotnet/tools:/opt/rider/bin"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1
EOF
chown "${RDP_USER}:${RDP_USER}" "${HOME_DIR}/.bashrc"

# ─── 3. VNC password file ─────────────────────────────────────────────────────
VNC_DIR="${HOME_DIR}/.vnc"
mkdir -p "${VNC_DIR}"
# vncpasswd -f reads from stdin and writes hashed 8-char VNC password to stdout
printf '%s' "${VNC_PASSWORD}" | vncpasswd -f > "${VNC_DIR}/passwd"
chmod 600 "${VNC_DIR}/passwd"
chown -R "${RDP_USER}:${RDP_USER}" "${VNC_DIR}"

# ─── 4. Start dbus (required by XFCE) ────────────────────────────────────────
mkdir -p /run/dbus
dbus-daemon --system --fork 2>/dev/null || true

# ─── 5. Start VNC server as the desktop user ─────────────────────────────────
echo "[entrypoint] Starting TigerVNC on display ${VNC_DISPLAY} (port ${VNC_PORT})…"
su - "${RDP_USER}" -c \
    "vncserver ${VNC_DISPLAY} -geometry ${GEOMETRY} -depth ${DEPTH} -localhost -rfbport ${VNC_PORT} 2>&1" \
    || true   # vncserver exits 0 after forking; "|| true" guards against strict exit codes

# Wait for Xvnc to accept connections
for i in $(seq 1 10); do
    if su - "${RDP_USER}" -c "DISPLAY=${VNC_DISPLAY} xdpyinfo" &>/dev/null 2>&1; then
        echo "[entrypoint] VNC ready."
        break
    fi
    echo "[entrypoint] Waiting for VNC ($i/10)…"
    sleep 1
done

# ─── 6. Start noVNC (browser access) ─────────────────────────────────────────
echo "[entrypoint] Starting noVNC on port ${PORT}…"
websockify --web /usr/share/novnc "${PORT}" "localhost:${VNC_PORT}" &
NOVNC_PID=$!

# ─── 7. Start xrdp (native RDP client access) ────────────────────────────────
echo "[entrypoint] Starting xrdp on port 3389…"
/usr/sbin/xrdp-sesman --config /etc/xrdp/sesman.ini &
sleep 1
/usr/sbin/xrdp --nodaemon &
XRDP_PID=$!

# ─── 8. Print connection info ─────────────────────────────────────────────────
echo ""
echo "╔══════════════════════════════════════════════════════╗"
echo "║          Ubuntu Desktop is starting up              ║"
echo "╠══════════════════════════════════════════════════════╣"
echo "║  Browser (noVNC):  http://<server-ip>:${PORT}       ║"
echo "║  RDP client:       <server-ip>:3389                 ║"
echo "║  Username:         ${RDP_USER}                      ║"
echo "║  RDP password:     ${RDP_PASSWORD}                  ║"
echo "║  VNC password:     ${VNC_PASSWORD} (max 8 chars)    ║"
echo "╚══════════════════════════════════════════════════════╝"
echo ""

# ─── 9. Keep container alive ──────────────────────────────────────────────────
trap 'echo "[entrypoint] Shutting down…"; kill ${NOVNC_PID} ${XRDP_PID} 2>/dev/null; \
      su - "${RDP_USER}" -c "vncserver -kill ${VNC_DISPLAY}" 2>/dev/null || true' \
     SIGTERM SIGINT

wait ${NOVNC_PID}
