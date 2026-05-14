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

# VNC protocol limits passwords to 8 characters; RDP/PAM uses the full password.
VNC_PASSWORD="${RDP_PASSWORD:0:8}"

echo "[entrypoint] Ubuntu Desktop starting — user=${RDP_USER}, noVNC port=${PORT}"

# ─── 1. Start websockify IMMEDIATELY ─────────────────────────────────────────
# This must be first so Sliplane's HTTP health check on PORT passes right away.
# websockify serves the noVNC HTML files via HTTP even before VNC is up;
# WebSocket→VNC proxying will work once VNC starts below.
echo "[entrypoint] Starting noVNC HTTP server on port ${PORT}…"
websockify --web /usr/share/novnc "${PORT}" "localhost:${VNC_PORT}" &
NOVNC_PID=$!

# ─── 2. Create / update Linux user ───────────────────────────────────────────
if id "${RDP_USER}" &>/dev/null; then
    echo "[entrypoint] User '${RDP_USER}' exists — refreshing password."
else
    echo "[entrypoint] Creating user '${RDP_USER}'."
    useradd --create-home --shell /bin/bash "${RDP_USER}"
fi
echo "${RDP_USER}:${RDP_PASSWORD}" | chpasswd
# Passwordless sudo — convenience for dev environment
echo "${RDP_USER} ALL=(ALL) NOPASSWD:ALL" > "/etc/sudoers.d/${RDP_USER}"
chmod 440 "/etc/sudoers.d/${RDP_USER}"

HOME_DIR="/home/${RDP_USER}"

# ─── 3. XFCE session + user shell environment ────────────────────────────────
echo "startxfce4" > "${HOME_DIR}/.xsession"
chmod 755 "${HOME_DIR}/.xsession"
chown "${RDP_USER}:${RDP_USER}" "${HOME_DIR}/.xsession"

cat >> "${HOME_DIR}/.bashrc" << 'EOF'
export DOTNET_ROOT=/usr/share/dotnet
export PATH="$PATH:/root/.dotnet/tools:/opt/rider/bin"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1
EOF
chown "${RDP_USER}:${RDP_USER}" "${HOME_DIR}/.bashrc"

# ─── 4. VNC password file ────────────────────────────────────────────────────
VNC_DIR="${HOME_DIR}/.vnc"
mkdir -p "${VNC_DIR}"
printf '%s' "${VNC_PASSWORD}" | vncpasswd -f > "${VNC_DIR}/passwd"
chmod 600 "${VNC_DIR}/passwd"
chown -R "${RDP_USER}:${RDP_USER}" "${VNC_DIR}"

# ─── 5. dbus (required by XFCE) ──────────────────────────────────────────────
mkdir -p /run/dbus
dbus-daemon --system --fork 2>/dev/null || true

# ─── 6. Start TigerVNC server ────────────────────────────────────────────────
echo "[entrypoint] Starting TigerVNC on display ${VNC_DISPLAY} (port ${VNC_PORT})…"
su - "${RDP_USER}" -c \
    "vncserver ${VNC_DISPLAY} -geometry ${GEOMETRY} -depth ${DEPTH} -localhost -rfbport ${VNC_PORT}" \
    2>&1 || true

# ─── 7. Start xrdp ───────────────────────────────────────────────────────────
echo "[entrypoint] Starting xrdp on port 3389…"
/usr/sbin/xrdp-sesman --config /etc/xrdp/sesman.ini 2>/dev/null &
sleep 1
/usr/sbin/xrdp --nodaemon 2>&1 &
XRDP_PID=$!

# ─── 8. Connection info ───────────────────────────────────────────────────────
echo ""
echo "╔══════════════════════════════════════════════════════╗"
echo "║          Ubuntu Desktop is ready                    ║"
echo "╠══════════════════════════════════════════════════════╣"
echo "║  Browser (noVNC):  http://<server-ip>:${PORT}       ║"
echo "║  RDP client:       <server-ip>:3389                 ║"
echo "║  Username:         ${RDP_USER}                      ║"
echo "║  RDP password:     ${RDP_PASSWORD}                  ║"
echo "║  noVNC password:   ${VNC_PASSWORD} (max 8 chars)    ║"
echo "╚══════════════════════════════════════════════════════╝"
echo ""

# ─── 9. Keep container alive ─────────────────────────────────────────────────
trap 'echo "[entrypoint] Shutting down…"; \
      kill ${NOVNC_PID} ${XRDP_PID} 2>/dev/null; \
      su - "${RDP_USER}" -c "vncserver -kill ${VNC_DISPLAY}" 2>/dev/null || true' \
     SIGTERM SIGINT

wait ${NOVNC_PID}
