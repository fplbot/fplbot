# Release Notes Style Guide

FplBot is a Fantasy Premier League chatbot for Slack and Discord. The
audience for these notes is workspace/guild admins who installed the bot —
not developers. Write for them, and write it **football-mad**: every entry
should sound like a matchday commentator lost the plot describing a
changelog. Silly football clichés and metaphors are not just allowed, they
are mandatory.

## Categories

Group entries under these headings, in this order. Omit a heading entirely
if it has no entries.

- `### ⚽ New Signings` — new notification types, commands, or bot features
- `### 🩹 Treatment Room` — bug fixes (describe the user-visible symptom
  that's gone, framed as a player coming back from injury / a niggle
  being sorted by the physio)
- `### 🎯 Tactical Tweaks` — changes to existing behavior that aren't new
  features or bug fixes (formation changes, a bit of extra pace, sharper
  finishing)

## What to Skip

Do not generate entries for:
- CI/CD, build, Dockerfile, or deployment pipeline changes
- Dependency/package bumps, unless they fix a security vulnerability or a
  user-visible bug
- Test-only changes, refactors, or internal code cleanup with no behavior
  change
- Changes to internal docs, CLAUDE.md, or repo tooling

## Style — lay it on thick

- Write for a non-technical Slack/Discord admin, not a developer. No
  jargon ("consumer", "handler", "MassTransit", "Redis", etc.) — ever.
- Every single entry must use at least one football commentary cliché or
  metaphor. Draw from things pundits and fans actually say, e.g.:
  - "an absolute worldie", "top bins", "back of the net", "onion bag"
  - "over the moon" / "sick as a parrot"
  - "squeaky bum time", "smash and grab", "park the bus"
  - "game of two halves", "early doors", "Fergie time", "at the death"
  - "a masterclass", "world-class", "won the game in the tunnel"
  - "row Z", "hospital pass", "handbags", "the dark arts"
  - "a rock at the back", "pulling the strings in midfield", "a poacher's
    instinct", "the engine room", "wonderkid", "super-sub off the bench"
  - "clean sheet", "banging them in for fun", "not at the races",
    "dead rubber", "relegation form", "title-winning"
- Describe the feature/fix/change AS IF it were a player, a tactical
  switch, or a moment in a match — but the actual behavior described must
  still be accurate and understandable underneath the bit.
- Do NOT invent or reference real, named footballers (living or
  retired) — use generic archetypes only ("the new striker", "the
  veteran centre-back", "the super-sub"), never a real person's name.
- Use present tense: "Add", "Fix", "Show" — commentary is happening live.
- Keep each entry to one or two sentences, punchy, like a commentary
  soundbite, not a paragraph.

## Entry Format

`<description>` — no PR numbers or author attribution, this is a
matchday-style changelog, not a dev changelog.

## Example Entries

- `### ⚽ New Signings`
  - Deadline reminders just got a new striker up top — you'll hear from
    them earlier, and they don't miss a chance to remind you before the
    whistle blows.
- `### 🩹 Treatment Room`
  - The captaincy alert had picked up a knock and was going missing at
    kick-off — the physio's had a look, and it's back on the teamsheet
    for every gameweek now.
- `### 🎯 Tactical Tweaks`
  - Price change notifications switched formation — tighter at the back,
    fewer needless final-third giveaways, same clinical finish.
