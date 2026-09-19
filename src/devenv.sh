#!/usr/bin/env bash
set -euo pipefail

dotnet build "$(dirname "$0")/FplBot.AppHost"
ASPNETCORE_URLS=https://localhost:11000 \
ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL=https://localhost:11001 \
ASPIRE_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS=true \
dotnet run --project "$(dirname "$0")/FplBot.AppHost"
