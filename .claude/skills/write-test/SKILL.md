---
name: write-test
description: Rules for writing tests in FplBot.Tests — entry-point level, E2E via AppFixture, unit-test scope. Use whenever adding or reviewing a test in src/FplBot.Tests.
---

# Writing tests in FplBot.Tests

Full rules live in `src/FplBot.Tests/Readme.md` — this skill is the same rule set, surfaced so it
loads automatically when writing or reviewing tests. Keep both files in sync if the rules change.

## General rules

- Always try to test from the highest possible entry point.
- Use Bogus and Faker for test data.
- Every test must be able to run in parallel and be isolated — no shared state.

### Higher-level starting points

- An API request:
  - Slack/Discord event notification on the `/events` / `/discord/events` endpoints
  - Admin endpoints
- An FPL notification (`GameweekJustBegan`, `FixtureEventsOccured`, etc.):
  - These come from `RecurringAction`s that poll the FPL APIs and diff particular aspects of the data.
  - Test both the diff code and the publishing — trigger via the `RecurringAction` or state machine
    that produces the event, not by hand-constructing the event itself.

## E2E tests

- Use `AppFixture`:
  - Spins up the test host; services under test and their dependencies are resolved via DI.
  - A real Redis instance via Testcontainers.
  - A real Azure Service Bus via Testcontainers (emulator).
- Mock as little as practically possible — only the external integration APIs:
  - FPL APIs
  - Slack
  - Discord
- Assert as little as possible on internals:
  - Test OUTCOMES, not implementations.
  - No `A.CallTo()` assertions on internal logic.

## Unit tests

- If the unit has a dependency on another component, move the test to an E2E test and test from a
  higher level instead.

OK to unit-test:
- Helpers, formatters, standalone components with few dependencies, or static methods.
- Things that are hard to test from a higher level, or that we're absolutely sure need to be
  pinned down in isolation.
