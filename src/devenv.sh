#!/usr/bin/env bash
set -euo pipefail

ASPNETCORE_URLS=https://localhost:11000 dotnet run --project "$(dirname "$0")/FplBot.AppHost"
