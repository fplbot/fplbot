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

There are three semantic buckets: **new stuff**, **fixes**, and
**polish**. Every release note entry belongs to exactly one of them. But
the heading text for each bucket is NOT fixed — every time you generate
notes, randomly pick a *different* heading (with its emoji) from that
bucket's pool below, so headings vary release to release instead of
being identical every time. Don't default to the first option in the
list — actually vary your pick.

Association football (soccer) and genuine FPL terms only. **Never**
American football terms (no "touchdown", "quarterback", "first down",
"blitz", "Hail Mary", "end zone", "gridiron", etc.) — this bot is about
the Premier League, not the NFL.

If a category has no entries, delete its heading line completely — do
not print the heading with a placeholder like "(none this window)",
"N/A", or an empty bullet. A heading with zero entries under it should
not appear in the output at all.

**New stuff** — new notification types, commands, or bot features. Pick
one heading at random:
- `### 🃏 Chip Plays`
- `### ⭐ New Signings`
- `### 🔄 Done Deals`
- `### 📝 Squad Additions`
- `### 🎯 Fresh Off The Bench`
- `### 🚀 New Boots`

Frame the size of the change as the transfer/chip being played: a small
addition is a straightforward **transfer in**; a genuinely big new
feature gets to be a **Wildcard** (full overhaul) or **Bench Boost**
(more of the squad now contributing); a temporary/experimental feature
can be a **Free Hit**.

**Fixes** — bug fixes. Pick one heading at random:
- `### 🩹 Treatment Room`
- `### 🚑 Back From Injury`
- `### 🏥 Fitness Update`
- `### ✅ Passed The Late Fitness Test`
- `### 🔧 Patched Up`

Frame as a player who picked up a knock and is now back on the
teamsheet, ideally back **before the deadline** rather than being a late
fitness doubt.

**Polish** — changes to existing behavior that aren't new features or
bug fixes: performance, formatting, small improvements. Pick one heading
at random:
- `### 📊 Bonus Points`
- `### 🎯 Tactical Tweaks`
- `### 🔁 Squad Rotation`
- `### 📈 Marginal Gains`
- `### 🧹 Half-Time Team Talk`

Frame as the BPS system quietly handing out extra points after the
match, or a tactical tweak from the touchline: nothing new happened on
the pitch, it just counts for more / reads better / runs smoother now.

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

### Crediting Contributors

The active core maintainers are **@johnkors**, **@skjelbek**, and
**@kristianeaw** — nobody else. Any PR author who is NOT one of those
three handles is an external contributor.

- **Mandatory, not optional:** if ANY entry in this release comes from
  an external contributor (author not in the maintainer list above), that
  specific entry's sentence MUST literally use the word "assist"
  somewhere in it (e.g. "assist from @handle", "picks up the assist",
  "gets the assist for this one") — not just vaguer language like
  "reinforcement" or "loan signing". A maintainer's entry should NOT use
  the word assist — reserve "goal"/"strike"/scoring language for them,
  "assist" specifically for external contributors. This distinction must
  be visibly, unmistakably present in the text whenever it applies.
- **Mandatory, not optional:** at least once per gameweek review (doesn't
  need to be every entry), gently rib whoever's name is on a PR —
  maintainer or contributor — about being a coder instead of an actual
  Premier League player: wish them Prem wages for this commit, joke they
  missed their calling as a £250k-a-week midfielder instead of fixing a
  Slack handler, etc. Keep it affectionate, about the money/fame gap
  only, never actually questioning their skill or code quality. Naming
  their real GitHub handle for this specific joke is required, not just
  allowed — that's the one exception to "no author attribution in this
  part." If a gameweek review has no such line in it, it's incomplete.

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
