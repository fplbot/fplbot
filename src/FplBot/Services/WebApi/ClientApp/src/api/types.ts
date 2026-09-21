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
  email: string | null;
  provider: string | null;
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

// A single Slack channel's subscription within a team (one team can now follow a league /
// receive notifications in more than one channel — mirrors Discord's GuildSubscription).
// memberCount/memberCountUpdatedAt are Slack-only (see conversations.info's num_members) —
// Discord's equivalent lives at the guild level (GuildWithSubs), not per channel.
export interface ChannelSubscription {
  id: string;
  teamId: string;
  channelId: string;
  leagueId: number | null;
  subscriptions: EventSubscription[];
  failureCount: number;
  failingSince: string | null;
  lastFailureReason: string | null;
  memberCount: number | null;
  memberCountUpdatedAt: string | null;
}

export interface TeamSummary {
  id: string;
  teamId: string;
  teamName: string;
  subscriptions: ChannelSubscription[];
  pendingRemoval: boolean;
}

export interface AvailableChannel {
  id: string;
  name: string;
}

export interface TeamDetailsChannel {
  id: string;
  channel: string;
  channelName: string | null;
  leagueId: number | null;
  leagueName: string | null;
  subscriptions: EventSubscription[];
  channelStatus: boolean | null;
  failureCount: number;
  failingSince: string | null;
  lastFailureReason: string | null;
  purgeEligibleAt: string | null;
  failuresUntilPurge: number;
  purgeFailureLimit: number;
}

export interface TeamDetails {
  id: string;
  teamId: string;
  teamName: string | null;
  token: string | null;
  pendingRemoval: boolean;
  channels: TeamDetailsChannel[];
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
  id: string;
  guildId: string;
  channelId: string;
  leagueId: number | null;
  subscriptions: EventSubscription[];
  failureCount: number;
  failingSince: string | null;
  lastFailureReason: string | null;
}

export interface GuildWithSubs {
  id: string;
  guildId: string;
  guildName: string;
  subscriptions: GuildSubscription[];
  approximateMemberCount: number | null;
  memberCountUpdatedAt: string | null;
}

export interface GuildDetailsChannel {
  id: string;
  channel: string;
  channelName: string | null;
  leagueId: number | null;
  leagueName: string | null;
  subscriptions: EventSubscription[];
  channelStatus: boolean | null;
  failureCount: number;
  failingSince: string | null;
  lastFailureReason: string | null;
  purgeEligibleAt: string | null;
  failuresUntilPurge: number;
  purgeFailureLimit: number;
}

export interface GuildDetails {
  id: string;
  guildId: string;
  guildName: string | null;
  channels: GuildDetailsChannel[];
}

export interface GuildReachStats {
  totalGuilds: number;
  totalApproximateMembers: number;
  oldestUpdate: string | null;
}

export interface TeamReachStats {
  totalTeams: number;
  totalApproximateMembers: number;
  oldestUpdate: string | null;
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

export interface LeagueSearchResult {
  exposedHits: LeagueItem[];
  maxHits: number;
  totalHits: number;
  page: number;
  totalPages: number;
}

export interface LeagueSearchResponse {
  hits: LeagueSearchResult;
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

export interface LeagueSummary {
  leagueName?: string;
  leagueAdmin?: string;
}

export interface LeagueDetails {
  leagueName?: string;
  leagueAdmin?: string;
  gameweek?: number;
  standings: StandingEntry[];
  summaries: EntrySummary[];
}

// ---- Admin: web push ----

export interface SubscriberSummary {
  id: string;
  name: string | null;
  leagueId: number | null;
  eventCount: number;
  endpointHost: string;
}

export interface SubscriberDetail {
  id: string;
  name: string | null;
  leagueId: number | null;
  events: string[];
  available: string[];
  requiresLeague: string[];
  endpointHost: string;
}

// ---- Admin: error queues ----

export interface ErrorQueueSummary {
  queue: string;
  consumer: string;
  length: number;
}

export interface ErrorQueueMessage {
  messageId: string;
  enqueuedTime: string;
  exceptionType: string;
  exceptionMessage: string;
  stackTrace: string | null;
  consumerType: string | null;
  originalMessageJson: string | null;
  traceId: string | null;
  traceUrl: string | null;
}

// Retry/discard/purge/retry-all drain a Service Bus queue, which takes longer than a browser
// should wait on a POST — the backend accepts the work (202) and reports the outcome via the job.
export interface ErrorQueueJobAccepted {
  jobId: string;
  kind: string;
  queue: string;
}

export interface ErrorQueueJobState {
  jobId: string;
  kind: string;
  queue: string;
  status: "Queued" | "Running" | "Succeeded" | "Failed";
  message: string | null;
}

export interface ChannelFailureStats {
  installationsWithFailures: number;
  channelsWithFailures: number;
  channelsEligibleForPurge: number;
}
