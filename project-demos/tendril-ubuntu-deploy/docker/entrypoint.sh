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

# Sliplane (and Docker) often mount a persistent volume at $HOME — it arrives empty and
# root-owned, so XFCE cannot mkdir ~/.config / ~/.cache → black noVNC + xfconfd crashes.
mkdir -p "${HOME_DIR}/.config" "${HOME_DIR}/.cache" "${HOME_DIR}/.local/share" "${HOME_DIR}/Desktop" 2>/dev/null || true

# ─── 3. Session + shell environment ──────────────────────────────────────────
echo "startxfce4" > "${HOME_DIR}/.xsession" 2>/dev/null || true
chmod 755 "${HOME_DIR}/.xsession" 2>/dev/null || true

# Desktop shortcuts (copy from skel if not already there)
mkdir -p "${HOME_DIR}/Desktop" 2>/dev/null || true
for f in /etc/skel/Desktop/*.desktop; do
    [ -f "$f" ] || continue
    dest="${HOME_DIR}/Desktop/$(basename "$f")"
    [ -f "$dest" ] || cp "$f" "$dest" 2>/dev/null || true
done
chmod +x "${HOME_DIR}/Desktop/"*.desktop 2>/dev/null || true

# PATH for dotnet tools in user's shell (idempotent)
if ! grep -q "dotnet/tools" "${HOME_DIR}/.bashrc" 2>/dev/null; then
    cat >> "${HOME_DIR}/.bashrc" << 'EOF'
export DOTNET_ROOT=/usr/share/dotnet
export PATH="$PATH:/root/.dotnet/tools:/opt/rider/bin"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1
EOF
fi

# Own entire home (required when $HOME is a volume mount created as root:root)
echo "[entrypoint] chown ${HOME_DIR} → ${RDP_USER} (volume / first-run safe)"
chown -R "${RDP_USER}:${RDP_USER}" "${HOME_DIR}" 2>/dev/null || true

# ─── 4. dbus (required by XFCE) ──────────────────────────────────────────────
mkdir -p /run/dbus
dbus-daemon --system --fork 2>/dev/null || true

# ─── 5. Virtual display + VNC (Xvfb + x11vnc, container-friendly) ───────────
echo "[entrypoint] Starting Xvfb on display ${VNC_DISPLAY}..."
# Clean any stale X locks from a previous run
rm -f /tmp/.X1-lock /tmp/.X11-unix/X1 2>/dev/null || true

Xvfb ${VNC_DISPLAY} -screen 0 "${GEOMETRY}x${DEPTH}" &
XVFB_PID=$!
sleep 2

echo "[entrypoint] Starting XFCE4 on display ${VNC_DISPLAY}..."
export DISPLAY=${VNC_DISPLAY}
# XFCE in Docker + Xvfb needs a per-user session D-Bus; plain "su … startxfce4" often yields a black noVNC screen.
# Match a typical ~/.vnc/xstartup pattern: unset stale session env, then start the desktop.
sudo -u "${RDP_USER}" env \
    DISPLAY="${VNC_DISPLAY}" \
    HOME="${HOME_DIR}" \
    USER="${RDP_USER}" \
    LOGNAME="${RDP_USER}" \
    NO_AT_BRIDGE=1 \
    bash -lc '
      unset SESSION_MANAGER
      unset DBUS_SESSION_BUS_ADDRESS
      [ -f "$HOME/.Xresources" ] && xrdb "$HOME/.Xresources" 2>/dev/null || true
      exec dbus-run-session startxfce4
    ' &
sleep 3

echo "[entrypoint] Starting x11vnc on port ${VNC_PORT}..."
x11vnc -display ${VNC_DISPLAY} -nopw -forever -shared \
       -rfbport "${VNC_PORT}" -localhost \
       2>/dev/null &
X11VNC_PID=$!

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
      kill ${NOVNC_PID} ${XRDP_PID} ${X11VNC_PID} ${XVFB_PID} 2>/dev/null; \
      exit 0' \
     SIGTERM SIGINT

wait ${NOVNC_PID}
