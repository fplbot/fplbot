# FPL gameweek states and the rollover window

How the FPL API behaves around a gameweek deadline, and what that means for FplBot.

Everything here was measured live across the GW4 → GW5 rollover on 2026-09-18
(deadline 17:30Z, first kickoff 19:00Z). Times are UTC, matching the API payloads.

## Timeline of a rollover

| Time | Δ deadline | Event |
|---|---|---|
| 17:30:00Z | 0 | Deadline. Nothing observable changes. |
| 17:43:30Z | +13 min | **Update window opens** — the API origin starts returning 503. |
| 17:49:41Z | +20 min | Last write lands (`last_updated_data` stops advancing). |
| 18:12:55Z | +43 min | **Window closes and `is_current` flips in the same instant.** |
| 19:00:00Z | +90 min | First kickoff. |

Two fixed relationships worth knowing:

- The deadline is always **90 minutes before the first kickoff**.
- The gameweek became current **47 minutes before kickoff**, i.e. the flip is tied to the
  rollover finishing, not to kickoff. Don't build logic that assumes a kickoff offset.

## The update window

During the window the API returns HTTP **503** with the JSON string body:

```json
"The game is being updated."
```

This is the same state the website renders as *"The game is updating and will be available
soon."*

**The whole origin is down, not a subset of endpoints.** It is tempting to conclude otherwise:
a naive sweep during the window shows `bootstrap-static/` and `fixtures/` returning 200 while
`event-status/`, `leagues-classic/…/standings/` and `entry/…` return 503. That split is an
artifact of which URLs happened to have a warm CDN entry. Cache-bust any of them and they all
503:

```bash
curl -s -o /dev/null -w "%{http_code}\n" \
  "https://fantasy.premierleague.com/api/bootstrap-static/?cb=$RANDOM"   # 503
```

The "healthy" endpoints age out to 503 one by one as their cache entries expire.

### Detecting the window

Use **`/api/event-status/`**: open, unauthenticated, no parameters, tiny payload, and it 503s in
lockstep with the league standings endpoint we actually depend on. `/api/me/` also works but sits
behind auth.

## CDN caching — read this before probing the API

FPL serves:

```
cache-control: max-age=300, stale-while-revalidate=3600, stale-if-error=3600
```

`stale-if-error=3600` is why the API looks alive while the origin is down: Fastly keeps answering
from cache for up to an hour. **Any 200 observed during the window is stale data.**

Always check the `age` response header. `age: 0` is live; anything else is how many seconds old
the payload is. During the window we observed ages of 0, 61 and 1201 seconds *simultaneously*
across different spellings of the same resource — each distinct query string is its own cache key:

| URL | Behaviour during the window |
|---|---|
| `?page_standings=1` | live for a while (edge-merged), then 503 |
| `?page_standings=1&phase=3` | **503** — this is the URL FplBot requests |
| `?page_new_entries=1&page_standings=1&phase=3` | 200, but `age=1201` (20-minute-old copy) |

A poller that ignores `age` will happily report "nothing changed" for the entire window while
reading a snapshot from before it opened.

## `new_entries` is not flushed at gameweek begin

The question this investigation started from. **It is not cleared.**

Across the rollover: 29 entries before, 29 after, with an empty set difference for lost entries
(the only delta was one *addition* that joined during the window). `oldest_joined` stayed at
`2026-09-12T13:51:05Z` — entries that predate the previous gameweek survive into the next one.
The list is not scoped to a gameweek at all.

Nor is it cleared when `last_updated_data` bumps, which happens constantly.

Consequences for `NewLeagueEntriesLookup`:

- The window filter (`joined > previous gameweek's deadline`) is doing the real work, and it is
  what makes the stale list harmless.
- Reading late is safe. A fetch minutes — or tens of minutes — after the flip returns the same
  filtered set.
- There is no race where the data disappears before we read it. The origin is down until the
  exact moment `is_current` flips, so the flag and the data become available together.

## What this means for our caching

Two independent cache layers stack:

| Layer | Key | TTL |
|---|---|---|
| Ours — `CacheProvider` over `IDistributedCache` (Redis) | the URL string | `bootstrap-static/` 5 min, league standings 30 min |
| FPL's — Fastly/Varnish | the URL string | `max-age=300`, `stale-if-error=3600` |

So `GameweekJustBegan` can lag FPL's real flip by our 5-minute TTL *on top of* whatever the edge
is still serving. That is acceptable: the new-entries window filter makes a late read identical to
a prompt one.

Note the `phase` parameter isolates cache keys. `NewLeagueEntriesLookup` is the only caller that
passes `phase:`, so its Redis entry is never shared with — or poisoned by — chat commands,
gameweek handlers or admin endpoints, which all call without it. When several channels follow the
same league, they do share it, which is desirable: one upstream call per league per gameweek, and
every channel sees a consistent list.

## Known exposure: 503 is not tolerated

`LeagueClient.GetClassicLeague` catches only `HttpStatusCode.NotFound`:

```csharp
catch (HttpRequestException e) when (e.StatusCode == HttpStatusCode.NotFound && tolerate404)
```

A fetch landing inside the window throws, the consumer faults, and recovery depends on MassTransit
retry. That is survivable — a ~30-minute window sits well inside the 2-hour Azure Service Bus TTL,
and the message lands in the `_error` queue only if retries exhaust first. Worth knowing when a
`ProcessNewLeagueEntriesFor*Channel` fault shows up in the admin Errors dashboard shortly after a
deadline: the cause is likely the rollover, not the league.

## Events that do not wait for the flip

`GameweekJustBegan` is not the only thing happening around a rollover, and other notifications can
legitimately arrive first. Lineups published ~18 minutes before the flip on 2026-09-18, because:

- `GameweekLifecycleMonitor` primes lineup state a gameweek ahead when the current one is finished
  (`lineupState.Reset(fetchedCurrent.IsFinished ? fetchedCurrent.Id + 1 : fetchedCurrent.Id)`), so
  `LineupState` was already tracking GW5.
- It reads `fixtures/?event={gw}`, a different endpoint whose cache entry outlived the others.

So a lineup notification is **not** evidence that the gameweek started.

Conversely, the captains message *is* a direct proxy for the flip —
`Discord/SlackGameweekStartedHandler` consumes `GameweekJustBegan` — offset only by our cache TTL
and the every-other-minute tick.

## Reproducing this

Poll with cache-busting and record `age`, then treat only `age=0` samples as evidence:

```bash
curl -s -D headers.txt "https://fantasy.premierleague.com/api/event-status/?cb=$RANDOM"
grep -iE '^(age|x-cache):' headers.txt
```

Run it every 60s from the deadline until `event-status/` returns 200 again. The tick where it
recovers carries both the new `is_current` and the post-rollover `new_entries` list.
