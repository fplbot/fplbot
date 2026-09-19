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

# dotnet run already reads launchSettings.json for the profile's environment, but ignores its
# launchBrowser/launchUrl. Read those here so the URL lives in one place rather than two.
profile=""
prev=""
for arg in "$@"; do
  [ "$prev" = "--launch-profile" ] && profile="$arg"
  prev="$arg"
done

launch_url="$(python3 -c '
import json, sys
profiles = json.load(open(sys.argv[1]))["profiles"]
profile = profiles.get(sys.argv[2]) or next(iter(profiles.values()))
print(profile["launchUrl"] if profile.get("launchBrowser") and profile.get("launchUrl") else "")
' "$src/FplBot/Properties/launchSettings.json" "$profile")"

if [ -n "$launch_url" ]; then
  echo "Opening $launch_url once Vite is up"
  (
    until curl -sf -o /dev/null "$launch_url"; do sleep 0.3; done
    open "$launch_url"
  ) &
else
  echo "Vite on http://localhost:5173 (open this one - it proxies to the backend)"
fi

dotnet run --project "$src/FplBot" "$@"
