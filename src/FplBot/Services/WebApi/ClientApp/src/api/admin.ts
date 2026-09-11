export class AdminApiError extends Error {
  status: number;
  // Server-provided detail from an application/problem+json body (RFC 9457), when present.
  detail?: string;
  constructor(message: string, status: number, detail?: string) {
    super(message);
    this.status = status;
    this.detail = detail;
  }
}

async function request<T>(input: string, init?: RequestInit): Promise<T> {
  const res = await fetch(input, { credentials: "include", ...init });
  if (!res.ok) {
    let detail: string | undefined;
    try {
      const problem = await res.json();
      detail = typeof problem?.detail === "string" ? problem.detail : problem?.title;
    } catch {
      // Not a JSON/problem-details body (e.g. a webhook path or a raw text response) — no detail to surface.
    }
    throw new AdminApiError(`Request to ${input} failed with status ${res.status}`, res.status, detail);
  }
  if (res.status === 204) {
    return undefined as T;
  }
  return res.json();
}

function postJson<T>(url: string, body?: unknown, method: string = "POST"): Promise<T> {
  return request<T>(url, {
    method,
    headers: body === undefined ? undefined : { "Content-Type": "application/json" },
    body: body === undefined ? undefined : JSON.stringify(body),
  });
}

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

export const ALL_EVENT_SUBSCRIPTIONS: EventSubscription[] = [
  "All", "Standings", "Captains", "Transfers", "FixtureGoals", "FixtureAssists", "FixtureCards",
  "FixturePenaltyMisses", "FixtureFullTime", "Taunts", "PriceChanges", "InjuryUpdates",
  "Deadlines", "Lineups", "NewPlayers", "FixtureRemovedFromGameweek",
];

// Matches FplBot.Messaging.Contracts.Commands.v1.ChannelFilter.
export type ChannelFilter =
  | "NotSet"
  | "AllChannels"
  | "AllChannelsDevServer"
  | "OnlyChannelsFollowingALeagueDevServer"
  | "OnlyChannelsFollowingALeague";

export const ALL_CHANNEL_FILTERS: ChannelFilter[] = [
  "AllChannels", "AllChannelsDevServer", "OnlyChannelsFollowingALeagueDevServer", "OnlyChannelsFollowingALeague",
];

export interface AdminMe {
  name: string | null;
  teamId: string | null;
  teamName: string | null;
  userId: string | null;
  isAdmin: boolean;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}

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

export interface MessageResponse {
  message: string;
}

export async function getMe(): Promise<AdminMe | null> {
  const res = await fetch("/api/admin/me", { credentials: "include" });
  if (res.status === 401) return null;
  if (!res.ok) throw new AdminApiError(`me() failed with status ${res.status}`, res.status);
  return res.json();
}

export function loginUrl(returnUrl?: string): string {
  return returnUrl ? `/api/admin/login?returnUrl=${encodeURIComponent(returnUrl)}` : "/api/admin/login";
}

export async function logout(): Promise<void> {
  await request<void>("/api/admin/logout", { method: "POST" });
}

export function getTeams(query: string, page: number, pageSize: number): Promise<PagedResult<TeamSummary>> {
  const params = new URLSearchParams({ query, page: String(page), pageSize: String(pageSize) });
  return request(`/api/admin/teams?${params.toString()}`);
}

export async function getTeam(teamId: string): Promise<TeamDetails | null> {
  const res = await fetch(`/api/admin/teams/${teamId}`, { credentials: "include" });
  if (res.status === 404) return null;
  if (!res.ok) throw new AdminApiError(`getTeam() failed with status ${res.status}`, res.status);
  return res.json();
}

export function uninstallTeam(teamId: string): Promise<MessageResponse> {
  return postJson(`/api/admin/teams/${teamId}/uninstall`);
}

export interface UpdateTeamRequest {
  leagueId: number;
  channel: string;
  subscriptions: EventSubscription[];
}

export function updateTeam(teamId: string, body: UpdateTeamRequest): Promise<{ updated: boolean; warnings: string[] }> {
  return postJson(`/api/admin/teams/${teamId}`, body, "PUT");
}

export function publishTeamEvent(teamId: string, subscriptions: EventSubscription[]): Promise<{ published: boolean; message: string }> {
  return postJson(`/api/admin/teams/${teamId}/publish-event`, { subscriptions });
}

export function broadcastToSlack(message: string): Promise<MessageResponse> {
  return postJson("/api/admin/slack/broadcast", { message });
}

export function broadcastToDiscord(message: string, filter: ChannelFilter): Promise<MessageResponse> {
  return postJson("/api/admin/discord/broadcast", { message, filter });
}

export interface Bookmarks {
  leagueIndexingBookmark: number;
  entryIndexingBookmark: number;
}

export function getBookmarks(): Promise<Bookmarks> {
  return request("/api/admin/indexing/bookmarks");
}

export function setLeagueBookmark(bookmark: number): Promise<MessageResponse> {
  return postJson("/api/admin/indexing/bookmarks/league", { bookmark });
}

export function setEntryBookmark(bookmark: number): Promise<MessageResponse> {
  return postJson("/api/admin/indexing/bookmarks/entry", { bookmark });
}

export interface DiscordSlashCommand {
  id: string;
  name: string;
  description: string;
}

export function getSlashCommands(): Promise<DiscordSlashCommand[]> {
  return request("/api/admin/discord/slashcommands");
}

export interface SlashCommandDefinition {
  name: string;
  description: string;
  optionsSummary: string;
}

export function getSlashCommandDefinitions(): Promise<SlashCommandDefinition[]> {
  return request("/api/admin/discord/slashcommands/definitions");
}

export function installSlashCommands(): Promise<MessageResponse> {
  return postJson("/api/admin/discord/slashcommands/install");
}

export function installGlobalSlashCommands(): Promise<MessageResponse> {
  return postJson("/api/admin/discord/slashcommands/install-global");
}

export function uninstallSlashCommands(): Promise<MessageResponse> {
  return postJson("/api/admin/discord/slashcommands/uninstall");
}

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

export function getDiscordSubscriptions(query: string, page: number, pageSize: number): Promise<PagedResult<GuildWithSubs>> {
  const params = new URLSearchParams({ query, page: String(page), pageSize: String(pageSize) });
  return request(`/api/admin/discord/subscriptions?${params.toString()}`);
}

export function deleteDiscordSubscription(guildId: string, channelId: string): Promise<MessageResponse> {
  return request(`/api/admin/discord/subscriptions/${guildId}/${channelId}`, { method: "DELETE" });
}
