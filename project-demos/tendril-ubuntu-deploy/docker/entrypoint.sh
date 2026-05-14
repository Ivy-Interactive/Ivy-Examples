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

echo "[entrypoint] Starting Ubuntu Desktop (noVNC on :${PORT}, xrdp on :3389)"
echo "[entrypoint] User: ${RDP_USER} | noVNC geometry: ${GEOMETRY}"

# ─── 1. Create / update user ─────────────────────────────────────────────────
if id "${RDP_USER}" &>/dev/null; then
    echo "[entrypoint] User '${RDP_USER}' already exists — updating password."
else
    echo "[entrypoint] Creating user '${RDP_USER}'."
    useradd --create-home --shell /bin/bash --groups sudo "${RDP_USER}"
fi
echo "${RDP_USER}:${RDP_PASSWORD}" | chpasswd

# Allow passwordless sudo for the user (dev environment convenience)
echo "${RDP_USER} ALL=(ALL) NOPASSWD:ALL" > "/etc/sudoers.d/${RDP_USER}"
chmod 440 "/etc/sudoers.d/${RDP_USER}"

# ─── 2. XFCE session file for xrdp + VNC ────────────────────────────────────
HOME_DIR="/home/${RDP_USER}"
echo "startxfce4" > "${HOME_DIR}/.xsession"
chmod 755 "${HOME_DIR}/.xsession"
chown "${RDP_USER}:${RDP_USER}" "${HOME_DIR}/.xsession"

# Ensure dotnet tools are on PATH for the user
echo 'export PATH="$PATH:/root/.dotnet/tools:/opt/rider/bin"' >> "${HOME_DIR}/.bashrc"
chown "${RDP_USER}:${RDP_USER}" "${HOME_DIR}/.bashrc"

# ─── 3. VNC password file ────────────────────────────────────────────────────
mkdir -p "${HOME_DIR}/.vnc"
chown "${RDP_USER}:${RDP_USER}" "${HOME_DIR}/.vnc"
# tigervnc reads the password via stdin; vncpasswd -f writes the hashed file
echo "${RDP_PASSWORD}" | vncpasswd -f > "${HOME_DIR}/.vnc/passwd"
chmod 600 "${HOME_DIR}/.vnc/passwd"
chown "${RDP_USER}:${RDP_USER}" "${HOME_DIR}/.vnc/passwd"

# ─── 4. Start VNC server (tigervnc) ─────────────────────────────────────────
echo "[entrypoint] Starting VNC server on display ${VNC_DISPLAY}…"
su - "${RDP_USER}" -c \
    "vncserver ${VNC_DISPLAY} \
        -geometry ${GEOMETRY} \
        -depth ${DEPTH} \
        -localhost \
        -rfbport ${VNC_PORT} \
        2>&1"
VNC_PID=$!

# Give VNC a moment to initialise
sleep 3

# ─── 5. Start noVNC (browser-based access) ───────────────────────────────────
echo "[entrypoint] Starting noVNC on port ${PORT}…"
websockify --web /usr/share/novnc "${PORT}" "localhost:${VNC_PORT}" 2>&1 &
NOVNC_PID=$!

# ─── 6. Start xrdp (RDP access) ──────────────────────────────────────────────
echo "[entrypoint] Starting xrdp on port 3389…"
# xrdp needs dbus
dbus-daemon --system --fork 2>/dev/null || true
service xrdp start 2>&1 || xrdp 2>&1 &

# ─── 7. Print connection info ─────────────────────────────────────────────────
echo ""
echo "╔══════════════════════════════════════════════════════════════╗"
echo "║          Ubuntu Desktop is ready                             ║"
echo "╠══════════════════════════════════════════════════════════════╣"
echo "║  Browser (noVNC):  http://<server-ip>:${PORT}               ║"
echo "║  RDP client:       <server-ip>:3389                         ║"
echo "║  Username:         ${RDP_USER}                              ║"
echo "║  Password:         ${RDP_PASSWORD}                          ║"
echo "╚══════════════════════════════════════════════════════════════╝"
echo ""

# ─── 8. Keep container alive ──────────────────────────────────────────────────
# Forward signals to children so Docker stop works cleanly.
trap 'echo "[entrypoint] Shutting down…"; kill ${VNC_PID} ${NOVNC_PID} 2>/dev/null; service xrdp stop 2>/dev/null' SIGTERM SIGINT

wait ${NOVNC_PID}
