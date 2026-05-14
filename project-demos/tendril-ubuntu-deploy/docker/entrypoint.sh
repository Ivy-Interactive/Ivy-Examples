#!/bin/bash
# No set -e: every step handles its own errors so websockify stays alive regardless.

# ─── Runtime defaults ────────────────────────────────────────────────────────
RDP_USER="${RDP_USER:-developer}"
RDP_PASSWORD="${RDP_PASSWORD:-TendrilRDP!1}"
PORT="${PORT:-8080}"
VNC_DISPLAY=":1"
VNC_PORT="5901"
GEOMETRY="${VNC_GEOMETRY:-1920x1080}"
DEPTH="${VNC_DEPTH:-24}"

echo "[entrypoint] Ubuntu Desktop starting — user=${RDP_USER}, port=${PORT}"

# ─── 1. Start websockify IMMEDIATELY ─────────────────────────────────────────
# Must be first — Sliplane checks HTTP on PORT right after container starts.
# websockify serves noVNC HTML via HTTP even before VNC is up.
# VNC uses -SecurityTypes None (localhost-only), so no VNC password needed.
echo "[entrypoint] Starting noVNC on port ${PORT}..."
websockify --web /usr/share/novnc "${PORT}" "localhost:${VNC_PORT}" &
NOVNC_PID=$!

# ─── 2. Create / update user ─────────────────────────────────────────────────
if id "${RDP_USER}" &>/dev/null; then
    echo "[entrypoint] User '${RDP_USER}' exists — refreshing password."
else
    echo "[entrypoint] Creating user '${RDP_USER}'."
    useradd --create-home --shell /bin/bash "${RDP_USER}" 2>/dev/null || true
fi
echo "${RDP_USER}:${RDP_PASSWORD}" | chpasswd 2>/dev/null || true
echo "${RDP_USER} ALL=(ALL) NOPASSWD:ALL" > "/etc/sudoers.d/${RDP_USER}" 2>/dev/null || true
chmod 440 "/etc/sudoers.d/${RDP_USER}" 2>/dev/null || true

HOME_DIR="/home/${RDP_USER}"

# ─── 3. Session + shell environment ──────────────────────────────────────────
echo "startxfce4" > "${HOME_DIR}/.xsession" 2>/dev/null || true
chmod 755 "${HOME_DIR}/.xsession" 2>/dev/null || true
chown "${RDP_USER}:${RDP_USER}" "${HOME_DIR}/.xsession" 2>/dev/null || true

# Desktop shortcuts (copy from skel if not already there)
mkdir -p "${HOME_DIR}/Desktop" 2>/dev/null || true
for f in /etc/skel/Desktop/*.desktop; do
    [ -f "$f" ] || continue
    dest="${HOME_DIR}/Desktop/$(basename "$f")"
    [ -f "$dest" ] || cp "$f" "$dest" 2>/dev/null || true
done
chmod +x "${HOME_DIR}/Desktop/"*.desktop 2>/dev/null || true
chown -R "${RDP_USER}:${RDP_USER}" "${HOME_DIR}/Desktop" 2>/dev/null || true

# PATH for dotnet tools in user's shell (idempotent)
if ! grep -q "dotnet/tools" "${HOME_DIR}/.bashrc" 2>/dev/null; then
    cat >> "${HOME_DIR}/.bashrc" << 'EOF'
export DOTNET_ROOT=/usr/share/dotnet
export PATH="$PATH:/root/.dotnet/tools:/opt/rider/bin"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1
EOF
fi
chown "${RDP_USER}:${RDP_USER}" "${HOME_DIR}/.bashrc" 2>/dev/null || true

# ─── 4. dbus (required by XFCE) ──────────────────────────────────────────────
mkdir -p /run/dbus
dbus-daemon --system --fork 2>/dev/null || true

# ─── 5. TigerVNC — no password (VNC is localhost-only, HTTPS via Sliplane) ───
echo "[entrypoint] Starting TigerVNC on display ${VNC_DISPLAY} (port ${VNC_PORT})..."
su - "${RDP_USER}" -c \
    "vncserver ${VNC_DISPLAY} -geometry ${GEOMETRY} -depth ${DEPTH} \
     -localhost -SecurityTypes None -rfbport ${VNC_PORT}" \
    2>&1 || echo "[entrypoint] VNC start failed (non-fatal — RDP still works)"

# ─── 6. xrdp ─────────────────────────────────────────────────────────────────
echo "[entrypoint] Starting xrdp on port 3389..."
/usr/sbin/xrdp-sesman --config /etc/xrdp/sesman.ini 2>/dev/null &
sleep 1
/usr/sbin/xrdp --nodaemon 2>&1 &
XRDP_PID=$!

# ─── 7. Ready ────────────────────────────────────────────────────────────────
echo ""
echo "╔══════════════════════════════════════════════════════╗"
echo "║          Ubuntu Desktop is ready                    ║"
echo "╠══════════════════════════════════════════════════════╣"
echo "║  Browser (noVNC):  http://<server-ip>:${PORT}       ║"
echo "║    (no VNC password — protected by Sliplane HTTPS)  ║"
echo "║  RDP client:       <server-ip>:3389                 ║"
echo "║  Username:         ${RDP_USER}                      ║"
echo "║  Password:         ${RDP_PASSWORD}                  ║"
echo "╚══════════════════════════════════════════════════════╝"
echo ""
echo "  To install JetBrains Rider: run  install-rider  in a terminal"
echo ""

# ─── 8. Keep container alive ─────────────────────────────────────────────────
trap 'echo "[entrypoint] Shutting down..."; \
      kill ${NOVNC_PID} ${XRDP_PID} 2>/dev/null; \
      su - "${RDP_USER}" -c "vncserver -kill ${VNC_DISPLAY}" 2>/dev/null; exit 0' \
     SIGTERM SIGINT

wait ${NOVNC_PID}
