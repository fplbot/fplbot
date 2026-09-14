# General rules

- Always try to test from the highest possible entry point
- Use Bogus and Faker
- All test must be able to run in parallell, and be isolated (no shared state): 

## Higher levels starting points:
- Api request
    - Slack/Discord event notification on `/events` `/discord/events` endpoints
    - Admin endpoints
- A FPL notification: `GameweekJustBegan`, `FixtureEventsOccured` etc


# E2E tests
- Uses AppFixture
  - Spins up the test host, and services under test or dependencies are resolved via DI
  - A real Redis instance via TestContainers
  - A real ASB via TestContainers (emulator)  
- Mocks _as little_ practically possible:
  - Only integration APIs: 
    - FPL APIs, 
    - Slack
    - Discord 
- Asserts as little as possible on internals:
  - Tests OUTCOMES, not implementations
  - No A.CallTo() assertions on internal logic

# Unit tests

- If the unit has a dependency to another component, it should be moved to a E2E test and test from a higher level:

OK for unit testing:
- Tests helpers, formatters, standalone components with few dependencies or static methods
- Things that are hard to test from a higher level, or we are absolutely sure we need to get well under test





