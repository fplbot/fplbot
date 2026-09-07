# Release Notes Style Guide

FplBot is a Fantasy Premier League chatbot for Slack and Discord. The
audience for these notes is workspace/guild admins who installed the bot —
not developers. Write for them.

## Categories

Group entries under these headings, in this order. Omit a heading entirely
if it has no entries.

- `### ✨ New` — new notification types, commands, or bot features
- `### 🐛 Fixed` — bug fixes (describe the user-visible symptom that's gone)
- `### 🔧 Improved` — changes to existing behavior that aren't new features
  or bug fixes (e.g. better formatting, faster updates)

## What to Skip

Do not generate entries for:
- CI/CD, build, Dockerfile, or deployment pipeline changes
- Dependency/package bumps, unless they fix a security vulnerability or a
  user-visible bug
- Test-only changes, refactors, or internal code cleanup with no behavior
  change
- Changes to internal docs, CLAUDE.md, or repo tooling

## Style

- Write for a non-technical Slack/Discord admin, not a developer
- Use present tense: "Add", "Fix", "Show"
- Talk about what the bot does differently, not which class/file changed
- One line per entry, plain language, no jargon (no "consumer", "handler",
  "MassTransit", "Redis", etc.)
- Keep each entry under ~100 characters where possible

## Entry Format

`<description>` — no PR numbers or author attribution needed, this is a
user-facing changelog, not a dev changelog.
