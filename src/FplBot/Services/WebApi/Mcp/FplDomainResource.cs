using System.ComponentModel;
using Fpl.Client;
using ModelContextProtocol.Server;

namespace FplBot.WebApi.Mcp;

[McpServerResourceType]
public class FplDomainResource
{
    [McpServerResource(UriTemplate = "fplbot://domain-knowledge", Name = "FPL domain knowledge", MimeType = "text/markdown")]
    [Description("Glossary, chip mechanics, league id 314, deadline mechanics, and fplbot's output-formatting conventions.")]
    public static string GetDomainKnowledge() => $"""
        # FPL domain knowledge

        ## Glossary
        - **Gameweek (GW)**: one round of Premier League fixtures.
        - **Entry**: a single manager's FPL team.
        - **Classic league**: a group of entries ranked by total points.
        - **Deadline**: the transfer cutoff for a gameweek - this is the `deadline`
          timestamp `get_gameweek` returns for that gameweek. Transfers are locked from
          that point until the gameweek ends. After the deadline, an entry's picks for
          that gameweek become publicly readable (see `get_entry`).
        - **Chip**: a boost played for a single gameweek. Codes from the FPL API:
          `{FplConstants.ChipNames.Wildcard}` (unlimited free transfers for one gameweek,
          with no transfer-cost penalty - the transfers made are permanent, not reverted),
          `{FplConstants.ChipNames.FreeHit}` (unlimited free transfers for one gameweek
          only - unlike Wildcard, the squad automatically reverts to what it was before
          Free Hit was played, at the next deadline), `{FplConstants.ChipNames.TripleCaptain}`
          (captain's score multiplier increased for one gameweek), `{FplConstants.ChipNames.BenchBoost}`
          (bench players' points count for one gameweek).
        - **BPS**: Bonus Points System - the underlying score FPL uses to award bonus
          points to the best-performing players in each match.

        Exact point values (goals, assists, clean sheets, cards, saves, defensive
        contribution, etc.) change by season - this resource is deliberately not a
        scoring table. Check the official FPL rules for current numbers.

        ## League id 314
        314 is FPL's built-in global "Overall" league. Every FPL entry is automatically a
        member. Its standings are ranked by total points, so page 1 of its standings
        (what `get_league_details` and `get_league_trends` read - the first 50 entries) is
        the top 50 managers worldwide. `get_league` itself only returns the league's name
        and admin, not standings - use `get_league_trends(314)` to see what the world's
        best managers are doing (most-captained, most-transferred) or
        `get_league_details(314)` for full per-entry detail. This has been a stable FPL
        numbering convention for years, but it isn't a documented part of the FPL API's
        contract.

        ## Output formatting
        When presenting results from these tools, match fplbot's existing Slack/Discord
        bot conventions:
        - Fixtures: short team codes joined with a hyphen, e.g. `WHU-CHE`, not
          "West Ham - Chelsea".
        - Players: the short display name, e.g. `Haaland`, not the full first/last name.
          The field holding it is called `web_name` on `get_player`'s output and on any
          nested player object (e.g. `get_captains`'s captain/viceCaptain), and `webName`
          on every other tool's synthesized results (`find_players`, `get_entry`'s squad,
          `get_league_trends`).
        """;
}
