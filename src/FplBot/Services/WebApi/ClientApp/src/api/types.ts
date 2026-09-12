// Types mirroring the JSON shapes the backend's /api/** endpoints accept and return.
// Hand-maintained for now — kept in sync manually whenever a backend DTO changes. Intended
// to eventually be generated from the backend (e.g. from an OpenAPI/JSON schema export)
// instead of hand-synced; until then, this file is the single place they live.

// ---- Shared ----

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface MessageResponse {
  message: string;
}

// ---- Admin: auth ----

export interface AdminMe {
  name: string | null;
  teamId: string | null;
  teamName: string | null;
  userId: string | null;
  isAdmin: boolean;
}

// ---- Admin: Slack teams ----

// Matches the backend EventSubscription enum (FplBot.Data.EventSubscription), serialized as
// strings via the JsonStringEnumConverter registered for minimal API JSON responses.
export type EventSubscription =
  | "All"
  | "Standings"
  | "Captains"
  | "Transfers"
  | "FixtureGoals"
  | "FixtureAssists"
  | "FixtureCards"
  | "FixturePenaltyMisses"
  | "FixtureFullTime"
  | "Taunts"
  | "PriceChanges"
  | "InjuryUpdates"
  | "Deadlines"
  | "Lineups"
  | "NewPlayers"
  | "FixtureRemovedFromGameweek";

export interface TeamSummary {
  teamId: string;
  teamName: string | null;
  channel: string | null;
  leagueId: number | null;
  subscriptions: EventSubscription[];
}

export interface TeamDetails {
  teamId: string;
  teamName: string | null;
  channel: string | null;
  leagueId: number | null;
  leagueName: string | null;
  subscriptions: EventSubscription[];
  channelStatus: boolean | null;
}

export interface UpdateTeamRequest {
  leagueId: number;
  channel: string;
  subscriptions: EventSubscription[];
}

// ---- Admin: broadcast ----

// Matches FplBot.Messaging.Contracts.Commands.v1.ChannelFilter.
export type ChannelFilter =
  | "NotSet"
  | "AllChannels"
  | "AllChannelsDevServer"
  | "OnlyChannelsFollowingALeagueDevServer"
  | "OnlyChannelsFollowingALeague";

// ---- Admin: search indexing bookmarks ----

export interface Bookmarks {
  leagueIndexingBookmark: number;
  entryIndexingBookmark: number;
}

// ---- Admin: search analytics ----

export interface TermCount {
  term: string;
  count: number;
}

export interface SearchAnalyticsResult {
  from: string;
  to: string;
  totalQueries: number;
  topQueries: TermCount[];
  topIpAddresses: TermCount[];
  topSlackSearchers: TermCount[];
}

// ---- Admin: Discord slash commands ----

export interface DiscordSlashCommand {
  id: string;
  name: string;
  description: string;
}

export interface SlashCommandDefinition {
  name: string;
  description: string;
  optionsSummary: string;
}

// ---- Admin: Discord subscriptions ----

export interface GuildSubscription {
  guildId: string;
  channelId: string;
  leagueId: number | null;
  subscriptions: EventSubscription[];
}

export interface GuildWithSubs {
  guildId: string;
  guildName: string;
  subscriptions: GuildSubscription[];
}

// ---- OAuth ----

export interface InstallUrlResponse {
  redirectUri: string;
}

// ---- Search ----

// Matches the backend SearchType enum (Fpl.Search.Models.SearchType): All, Entries, Leagues.
export type SearchType = "All" | "Entries" | "Leagues";

export interface EntryItem {
  id: number;
  realName?: string;
  teamName?: string;
  alias?: string;
  description?: string;
  country?: string;
  numberOfPastSeasons: number;
  thumbprint?: string;
}

export interface LeagueItem {
  id: number;
  name?: string;
  adminEntry?: number;
  adminName?: string;
  adminTeamName?: string;
  adminCountry?: string;
}

// The /search/any endpoint wraps each hit in a { type, source } container
// (see SearchService.SearchAny / SearchContainer in the backend) rather than
// exposing EntryItem/LeagueItem directly in the array.
export interface SearchHit {
  type: "entry" | "league" | null;
  source: EntryItem | LeagueItem;
}

export interface SearchAnyResult {
  exposedHits: SearchHit[];
  maxHits: number;
  totalHits: number;
  page: number;
  totalPages: number;
}

export interface SearchAnyResponse {
  hits: SearchAnyResult;
}

// ---- League details ----

export interface StandingEntry {
  entry: number;
  playerName?: string;
  teamName?: string;
  rank: number;
  lastRank: number;
  total: number;
  eventTotal: number;
}

export interface Transfer {
  playerIn?: string;
  playerOut?: string;
  playerInCost: number;
  playerOutCost: number;
  time: string;
}

export interface EntrySummary {
  entry: number;
  playerName?: string;
  captain?: string;
  viceCaptain?: string;
  chip?: string;
  transfers: Transfer[];
}

export interface LeagueDetails {
  leagueName?: string;
  leagueAdmin?: string;
  gameweek?: number;
  standings: StandingEntry[];
  summaries: EntrySummary[];
}
