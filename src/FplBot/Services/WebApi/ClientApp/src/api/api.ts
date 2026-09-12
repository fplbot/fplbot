import type {
  AdminMe,
  Bookmarks,
  ChannelFilter,
  DiscordSlashCommand,
  EventSubscription,
  GuildWithSubs,
  InstallUrlResponse,
  LeagueDetails,
  MessageResponse,
  PagedResult,
  SearchAnalyticsResult,
  SearchAnyResponse,
  SearchAnyResult,
  SearchType,
  SlashCommandDefinition,
  TeamDetails,
  TeamSummary,
  UpdateTeamRequest,
} from "./types";

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

// ---- Admin: auth ----

export const ALL_EVENT_SUBSCRIPTIONS: EventSubscription[] = [
  "All", "Standings", "Captains", "Transfers", "FixtureGoals", "FixtureAssists", "FixtureCards",
  "FixturePenaltyMisses", "FixtureFullTime", "Taunts", "PriceChanges", "InjuryUpdates",
  "Deadlines", "Lineups", "NewPlayers", "FixtureRemovedFromGameweek",
];

export const ALL_CHANNEL_FILTERS: ChannelFilter[] = [
  "AllChannels", "AllChannelsDevServer", "OnlyChannelsFollowingALeagueDevServer", "OnlyChannelsFollowingALeague",
];

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

// ---- Admin: Slack teams ----

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

export function updateTeam(teamId: string, body: UpdateTeamRequest): Promise<{ updated: boolean; warnings: string[] }> {
  return postJson(`/api/admin/teams/${teamId}`, body, "PUT");
}

export function publishTeamEvent(teamId: string, subscriptions: EventSubscription[]): Promise<{ published: boolean; message: string }> {
  return postJson(`/api/admin/teams/${teamId}/publish-event`, { subscriptions });
}

export function broadcastToSlack(message: string): Promise<MessageResponse> {
  return postJson("/api/admin/slack/broadcast", { message });
}

// ---- Admin: search indexing bookmarks ----

export function getBookmarks(): Promise<Bookmarks> {
  return request("/api/admin/indexing/bookmarks");
}

export function setLeagueBookmark(bookmark: number): Promise<MessageResponse> {
  return postJson("/api/admin/indexing/bookmarks/league", { bookmark });
}

export function setEntryBookmark(bookmark: number): Promise<MessageResponse> {
  return postJson("/api/admin/indexing/bookmarks/entry", { bookmark });
}

export function getSearchAnalytics(days: number, size: number): Promise<SearchAnalyticsResult> {
  const params = new URLSearchParams({ days: String(days), size: String(size) });
  return request(`/api/admin/search/analytics?${params.toString()}`);
}

// ---- Admin: Discord ----

export function broadcastToDiscord(message: string, filter: ChannelFilter): Promise<MessageResponse> {
  return postJson("/api/admin/discord/broadcast", { message, filter });
}

export function getSlashCommands(): Promise<DiscordSlashCommand[]> {
  return request("/api/admin/discord/slashcommands");
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

export function getDiscordSubscriptions(query: string, page: number, pageSize: number): Promise<PagedResult<GuildWithSubs>> {
  const params = new URLSearchParams({ query, page: String(page), pageSize: String(pageSize) });
  return request(`/api/admin/discord/subscriptions?${params.toString()}`);
}

export function deleteDiscordSubscription(guildId: string, channelId: string): Promise<MessageResponse> {
  return request(`/api/admin/discord/subscriptions/${guildId}/${channelId}`, { method: "DELETE" });
}

export function deleteAllDiscordSubscriptionsForGuild(guildId: string): Promise<MessageResponse> {
  return request(`/api/admin/discord/guilds/${guildId}/subscriptions`, { method: "DELETE" });
}

export function deleteDiscordGuild(guildId: string): Promise<MessageResponse> {
  return request(`/api/admin/discord/guilds/${guildId}`, { method: "DELETE" });
}

// ---- OAuth (public site install buttons) ----

export async function redirectToSlackInstall(): Promise<void> {
  const res = await fetch("/api/oauth/install-url");
  const data: InstallUrlResponse = await res.json();
  window.location.href = data.redirectUri;
}

export async function redirectToDiscordInstall(): Promise<void> {
  const res = await fetch("/api/oauth/install-url-discord");
  const data: InstallUrlResponse = await res.json();
  window.location.href = data.redirectUri;
}

// ---- Search ----

export async function searchAny(
  query: string,
  page: number,
  type: SearchType = "All"
): Promise<SearchAnyResult> {
  const params = new URLSearchParams({ query, page: String(page), type });
  const res = await fetch(`/api/search/any?${params.toString()}`);
  if (!res.ok) {
    throw new Error(`Search request failed with status ${res.status}`);
  }
  const data: SearchAnyResponse = await res.json();
  return data.hits;
}

// ---- League details (public site) ----

export async function getLeagueDetails(leagueId: number): Promise<LeagueDetails | null> {
  const res = await fetch(`/api/fpl/leagues/${leagueId}/details`);
  if (res.status === 404) {
    return null;
  }
  if (!res.ok) {
    throw new Error(`League details request failed with status ${res.status}`);
  }
  return res.json();
}
