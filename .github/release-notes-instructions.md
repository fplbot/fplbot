# Release Notes Style Guide

FplBot is a Fantasy Premier League chatbot for Slack and Discord. Produce
release notes in **two parts**, always both, in this order.

## Part 1 — the gameweek review (for admins, FPL-mad)

A human, non-technical summary for the workspace/guild admins who
installed the bot, written for an FPL manager, not a generic football
fan. Treat this release as a **gameweek**: features are transfers and
chip plays, fixes are players returning from the treatment room in time
for the deadline, and small improvements are the extra bonus points
handed out once the BPS dust settles. Football commentary clichés are
welcome as seasoning, but the primary flavor must be genuine **Fantasy
Premier League** mechanics — gameweeks, deadlines, transfers, price
changes, chips, captaincy, bonus points, autosubs, rank.

### Categories

Group entries under these headings, in this order. If a category has no
entries, delete its heading line completely — do not print the heading
with a placeholder like "(none this window)", "N/A", or an empty bullet.
A heading with zero entries under it should not appear in the output at
all.

- `### 🃏 Chip Plays` — new notification types, commands, or bot
  features. Frame the size of the change as the chip being played:
  a small addition is a straightforward **transfer in**; a genuinely big
  new feature gets to be a **Wildcard** (full overhaul) or **Bench
  Boost** (more of the squad now contributing); a temporary/experimental
  feature can be a **Free Hit**.
- `### 🩹 Treatment Room` — bug fixes. Frame as a player who picked up a
  knock and is now back on the teamsheet, ideally back **before the
  deadline** rather than being a late fitness doubt.
- `### 📊 Bonus Points` — changes to existing behavior that aren't new
  features or bug fixes — polish, performance, formatting. Frame as the
  BPS system quietly handing out extra points after the match: nothing
  new happened on the pitch, it just counts for more / reads better now.

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

### The Blank Gameweek

If, after applying the skip list above, there are **zero** entries across
all three categories (i.e. the release is nothing but chores, CI,
refactors, dependency bumps, etc.) — do NOT print any of the three
category headings. Instead write a short, genuinely funny "nothing to
see here" blurb, framed as an FPL **blank gameweek**: the fixture
computer left this one empty, every one of your players is on a bye, 0-0
snorefest, no points on the board either way. Lean into pundit/manager
disappointment ("we were promised a double gameweek, we got a bye
week"), and be honest that this release is all backstage/pitch-
maintenance work with nothing new for admins to notice on the teamsheet.
Keep it to 2-4 sentences, still funny — this is the one place atmosphere
-over-substance is fine, since the honest fact IS "nothing user-facing
changed", so say that plainly somewhere in the blurb.

### Style — lay it on thick, FPL-first

- Write for a non-technical Slack/Discord admin who plays FPL, not a
  developer. No engineering jargon ("consumer", "handler",
  "MassTransit", "Redis", etc.) — ever.
- Every entry must use at least one genuine **FPL-specific** concept, not
  just generic football commentary. Draw from real FPL mechanics, e.g.:
  - gameweek, deadline, the transfer window, a free transfer vs. taking
    a hit
  - price rises / price falls, ownership %, a differential pick, a
    template pick
  - chips: Wildcard, Free Hit, Bench Boost, Triple Captain, Assistant
    Manager
  - captain / vice-captain, armband, blank gameweek, double gameweek,
    autosub, rank, green arrow / red arrow
  - bonus points (BPS), defensive contribution points, a clean sheet,
    a returning player fit for the deadline
- General football commentator clichés ("an absolute worldie", "top
  bins", "squeaky bum time", "smash and grab", "at the death") are fine
  as extra color layered on top of the FPL mechanic, not a replacement
  for it.
- Describe the feature/fix/change AS IF it were a transfer, a chip play,
  or a gameweek event — but the actual behavior described must still be
  accurate and understandable underneath the bit.
- **The metaphor is seasoning, not the substance.** Every entry MUST make
  it unambiguously clear, in plain terms, what actually changed and what
  the admin will now see or no longer see — a reader must not have to
  already know what the bug/feature was to understand the entry. If you
  had to strip out every football/FPL word, a factual sentence
  describing the real behavior change should still be sitting there
  underneath. Never write an entry that is pure atmosphere with no
  recoverable fact in it (e.g. "brought on as a sub" on its own is NOT
  enough — say what changed, when, and what happens differently now).
- Concrete beats vague: name the actual notification/command/setting
  affected and the actual before/after behavior, not just "a niggle" or
  "some tweaks".
- Do NOT invent or reference real, named footballers or real FPL
  managers/content creators (living or retired) — use generic archetypes
  only ("the new striker", "the template captain", "the differential
  pick"), never a real person's name.
- Use present tense: "Add", "Fix", "Show" — commentary is happening live.
- One to three sentences per entry — long enough to actually explain the
  change, short enough to still read like a commentary soundbite, not a
  press release.
- No PR numbers or author attribution in this part.

### Example Entries

- `### 🃏 Chip Plays`
  - Playing the Bench Boost on deadline reminders: they now fire a full
    hour before the transfer window shuts instead of just 15 minutes
    before, so nobody's left making a panic transfer at the death.
- `### 🩹 Treatment Room`
  - The captaincy alert had been ruled out whenever a workspace
    uninstalled and reinstalled the bot — no armband reminder at all for
    that team, gameweek after gameweek. Passed its late fitness test:
    it's back reminding every team before every deadline, reinstall or
    not.
- `### 📊 Bonus Points`
  - Price change notifications used to dump every rise and fall into one
    wall-of-text message. BPS has been recalculated — one clean line per
    player now, same green/red arrows, way less scrolling to find your
    guy.

Note how each example names the exact notification/command affected and
the precise before → after behavior, using a real FPL mechanic (chip
plays, fitness tests, BPS) to carry it — the flourish decorates the
fact, it doesn't substitute for it.

---

## Part 2 — the detailed match report (for developers)

A separate, plain, precise, technical section under a `### 📋 Detailed
Match Report` heading, aimed at engineers reading this on GitHub. No
football or FPL bits here — just facts.

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
