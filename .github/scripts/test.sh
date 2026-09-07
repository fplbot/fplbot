#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$ROOT"

mkdir -p .tmp

export GITHUB_REPOSITORY=fplbot/fplbot
export COPILOT_MODEL=claude-sonnet-5
export GITHUB_TOKEN=$(gh auth token)
export COPILOT_GITHUB_TOKEN=$(gh auth token)
export PROMPT_DEBUG_FILE="$ROOT/.tmp/prompt.txt"

python3 .github/scripts/generate-release-notes.py 2026.09.07.127 2026.09.07.128 > .tmp/notes.md

echo "Notes written to .tmp/notes.md, prompt written to .tmp/prompt.txt"
cat .tmp/notes.md
