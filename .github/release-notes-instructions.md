# Release Notes Style Guide

FplBot is a Fantasy Premier League chatbot for Slack and Discord. Produce
release notes in **two parts**, always both, in this order.

## Part 1 — the matchday commentary (for admins, football-mad)

A human, non-technical summary for the workspace/guild admins who
installed the bot. Write it **football-mad**: every entry should sound
like a matchday commentator lost the plot describing a changelog. Silly
football clichés and metaphors are not just allowed, they are mandatory.

### Categories

Group entries under these headings, in this order. Omit a heading
entirely if it has no entries.

- `### ⚽ New Signings` — new notification types, commands, or bot features
- `### 🩹 Treatment Room` — bug fixes (describe the user-visible symptom
  that's gone, framed as a player coming back from injury / a niggle
  being sorted by the physio)
- `### 🎯 Tactical Tweaks` — changes to existing behavior that aren't new
  features or bug fixes (formation changes, a bit of extra pace, sharper
  finishing)

### What to Skip (this part only)

Do not generate commentary entries for:
- CI/CD, build, Dockerfile, or deployment pipeline changes
- Dependency/package bumps, unless they fix a security vulnerability or a
  user-visible bug
- Test-only changes, refactors, or internal code cleanup with no behavior
  change
- Changes to internal docs, CLAUDE.md, or repo tooling

(These are still fair game for Part 2 below — never silently dropped
entirely, just kept out of the commentary.)

### Style — lay it on thick

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
- **The metaphor is seasoning, not the substance.** Every entry MUST make
  it unambiguously clear, in plain terms, what actually changed and what
  the admin will now see or no longer see — a reader must not have to
  already know what the bug/feature was to understand the entry. If you
  had to strip out every football word, a factual sentence describing the
  real behavior change should still be sitting there underneath. Never
  write an entry that is pure atmosphere with no recoverable fact in it
  (e.g. "went missing in the tunnel" on its own is NOT enough — say what
  went missing, when, and what happens differently now that it's fixed).
- Concrete beats vague: name the actual notification/command/setting
  affected and the actual before/after behavior, not just "a niggle" or
  "some tweaks".
- Do NOT invent or reference real, named footballers (living or
  retired) — use generic archetypes only ("the new striker", "the
  veteran centre-back", "the super-sub"), never a real person's name.
- Use present tense: "Add", "Fix", "Show" — commentary is happening live.
- One to three sentences per entry — long enough to actually explain the
  change, short enough to still read like a commentary soundbite, not a
  press release.
- No PR numbers or author attribution in this part.

### Example Entries

- `### ⚽ New Signings`
  - New striker up top: deadline reminders now fire a full hour before
    the transfer window shuts, not just 15 minutes before — no more
    getting caught cold at the death.
- `### 🩹 Treatment Room`
  - The captaincy alert had been going missing whenever a workspace
    uninstalled and reinstalled the bot — no reminder would fire at all
    for that team. Physio's sorted it: the alert is back on the
    teamsheet and fires every gameweek again, reinstall or not.
- `### 🎯 Tactical Tweaks`
  - Price change notifications used to batch every player rise/fall
    into one wall-of-text message. Tighter shape now — one clean line
    per player, same info, way less scrolling to find your guy.

Note how each example names the exact notification/command affected and
the precise before → after behavior — the football flourish decorates
that fact, it doesn't substitute for it.

---

## Part 2 — the match report (for developers)

A separate, plain, precise, technical section under a `### 📋 Match
Report` heading, aimed at engineers reading this on GitHub. No football
bits here — just facts.

- Include **every** merged PR, with nothing skipped — including CI/CD,
  dependency bumps, refactors, and internal tooling changes that Part 1
  leaves out.
- One line per PR: `<concise technical description> (#<pr_number>) by
  @<author>`
- Group as a flat list, or under `Fixes` / `Features` / `Chores` /
  `Dependencies` sub-bullets if there are more than ~8 entries — whichever
  is more scannable.
- Use normal engineering language here (consumer, handler, endpoint,
  dependency name, etc. are all fine) — this part is for developers, be
  precise rather than cute.
- Keep each line short — this is a scan-and-click reference, not prose.
