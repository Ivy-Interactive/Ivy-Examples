#!/usr/bin/env bash
# Build db.sqlite from schema.sql + seed.sql (Authors → Genres → Books with FKs).
set -euo pipefail
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
OUT="${1:-$ROOT/db.sqlite}"

command -v sqlite3 >/dev/null || { echo "Install sqlite3 (e.g. brew install sqlite)"; exit 1; }

rm -f "$OUT"
cat "$SCRIPT_DIR/schema.sql" "$SCRIPT_DIR/seed.sql" | sqlite3 "$OUT"
echo "Created: $OUT"
sqlite3 "$OUT" "PRAGMA foreign_key_check;"
sqlite3 "$OUT" "SELECT COUNT(*) AS books FROM Books; SELECT COUNT(*) AS authors FROM Authors; SELECT COUNT(*) AS genres FROM Genres;"
