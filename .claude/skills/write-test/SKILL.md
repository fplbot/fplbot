---
name: write-test
description: Rules for writing tests in FplBot.Tests — entry-point level, E2E via AppFixture, unit-test scope. Use whenever adding or reviewing a test in src/FplBot.Tests.
---

# Writing tests in FplBot.Tests

This skill is the source of truth for test rules in this repo.

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
- Set state up through the real flows `AppFixture` exposes — `InstallSlackbot()`,
  `Subscribe(teamId, channel, params FplEvent[])`, `AskSlackbot(teamId, channel, "<@UREFQD887> follow {id}")`
  — not by seeding domain objects straight into a repository.
- Don't fake a whole service interface to avoid infra we already run in Docker (Redis, Elasticsearch,
  Service Bus emulator). Fake only what can't run locally: FPL, Slack, Discord APIs.

## Unit tests

- If the unit has a dependency on another component, move the test to an E2E test and test from a
  higher level instead.

OK to unit-test:
- Helpers, formatters, standalone components with few dependencies, or static methods.
- Things that are hard to test from a higher level, or that we're absolutely sure need to be
  pinned down in isolation.

## Every feature change gets a test

- New or changed MassTransit consumer, recurring job or state machine → E2E test in `FplBot.Tests/E2E/`.
- New or changed HTTP endpoint handler → test in `FplBot.Tests/E2E/ApiEndpoints/`, calling the
  `internal static` handler directly (the assembly has `InternalsVisibleTo("FplBot.Tests")`).

If the file being changed has no such test yet, add one as part of the change.

## Unit test layout

Unit tests live in `FplBot.Tests/UnitTests/` (`Formatting/`, `Helpers/`, `Domain/`, `StringParsers/`, …).
There is no `Helpers/Factory.cs` — shared test bootstrapping is `TestBuilder.cs`, `TestPublishEndpoint.cs`,
`GlobalSettingsClientBuilder.cs` and `SlackInstallationFaker.cs`.

Don't reimplement what FakeItEasy already does — use `.Returns(a).Once().Then.Returns(b)` rather than
a hand-rolled call counter.
