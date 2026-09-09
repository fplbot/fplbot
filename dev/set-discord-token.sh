#!/usr/bin/env bash
set -euo pipefail

APP_ID="895012635144224830"
RESET_URL="https://discord.com/developers/applications/${APP_ID}/bot"
SECRETS_ID="fplbot-secrets"

echo "This resets the bot token for the fplbot test Discord app and stores it in your local user secrets."
echo
echo "1. Open: ${RESET_URL}"
echo "2. Click 'Reset Token', confirm, and copy the new token."
echo

if command -v open >/dev/null 2>&1; then
    read -r -p "Press enter to open this URL in your browser (or Ctrl+C to do it manually)... "
    open "$RESET_URL"
fi

echo
read -r -s -p "Paste the new token here (input hidden): " DISCORD_TOKEN
echo

if [ -z "$DISCORD_TOKEN" ]; then
    echo "No token entered, aborting." >&2
    exit 1
fi

dotnet user-secrets set DISCORD_TOKEN "$DISCORD_TOKEN" --id "$SECRETS_ID"
echo "Stored DISCORD_TOKEN in user secrets (id: $SECRETS_ID)."
