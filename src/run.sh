#!/usr/bin/env bash
set -euo pipefail

src="$(cd "$(dirname "$0")" && pwd)"
clientapp="$src/FplBot/Services/WebApi/ClientApp"

if [ ! -d "$clientapp/node_modules" ]; then
  echo "Installing ClientApp dependencies..."
  (cd "$clientapp" && npm ci)
fi

(cd "$clientapp" && npm run dev) &
vite=$!
trap 'kill $vite 2>/dev/null || true' EXIT

echo "Vite on http://localhost:5173 (open this one - it proxies to the backend)"

(
  until curl -sf -o /dev/null http://localhost:5173; do sleep 0.3; done
  open http://localhost:5173/admin
) &

dotnet run --project "$src/FplBot" "$@"
