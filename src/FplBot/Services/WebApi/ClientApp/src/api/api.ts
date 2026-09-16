import type {
  AdminMe,
  Bookmarks,
  ChannelFilter,
  DiscordSlashCommand,
  ErrorQueueJobAccepted,
  ErrorQueueJobState,
  ErrorQueueMessage,
  ErrorQueueSummary,
  EventSubscription,
  GuildDetails,
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
  const text = await res.text();
  if (!text) {
    return undefined as T;
  }
  return JSON.parse(text);
}

function postJson<T>(url: string, body?: unknown, method: string = "POST"): Promise<T> {
  return request<T>(url, {
    method,
    headers: body === undefined ? undefined : { "Content-Type": "application/json" },
    body: body === undefined ? undefined : JSON.stringify(body),
  });
}

// ---- Admin: auth ----

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

export function publishStandings(teamId: string, channelId: string): Promise<{ published: boolean; message: string }> {
  return postJson(`/api/admin/teams/${teamId}/channels/${encodeURIComponent(channelId)}/publish-standings`);
}

export function broadcastToSlack(message: string): Promise<MessageResponse> {
  return postJson("/api/admin/slack/broadcast", { message });
}

export const ALL_EVENT_SUBSCRIPTIONS: EventSubscription[] = [
  "All", "Standings", "Captains", "Transfers", "FixtureGoals", "FixtureAssists", "FixtureCards",
  "FixturePenaltyMisses", "FixtureFullTime", "Taunts", "PriceChanges", "InjuryUpdates", "Deadlines",
  "Lineups", "NewPlayers", "FixtureRemovedFromGameweek",
];

export function updateChannelSubscriptions(
  teamId: string,
  channelId: string,
  subscriptions: EventSubscription[]
): Promise<MessageResponse> {
  return postJson(`/api/admin/teams/${teamId}/channels/${encodeURIComponent(channelId)}/subscriptions`, { subscriptions }, "PUT");
}

export function moveChannel(teamId: string, channelId: string, newChannelId: string): Promise<MessageResponse> {
  return postJson(`/api/admin/teams/${teamId}/channels/${encodeURIComponent(channelId)}/channel`, { newChannelId }, "PUT");
}

export function deleteChannelSubscription(teamId: string, channelId: string): Promise<MessageResponse> {
  return request(`/api/admin/teams/${teamId}/channels/${encodeURIComponent(channelId)}`, { method: "DELETE" });
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

export function getDiscordServers(query: string, page: number, pageSize: number): Promise<PagedResult<GuildWithSubs>> {
  const params = new URLSearchParams({ query, page: String(page), pageSize: String(pageSize) });
  return request(`/api/admin/discord/servers?${params.toString()}`);
}

export function deleteDiscordSubscription(guildId: string, channelId: string): Promise<MessageResponse> {
  return request(`/api/admin/discord/servers/${guildId}/${channelId}`, { method: "DELETE" });
}

export function deleteAllDiscordSubscriptionsForGuild(guildId: string): Promise<MessageResponse> {
  return request(`/api/admin/discord/guilds/${guildId}/subscriptions`, { method: "DELETE" });
}

export function deleteDiscordGuild(guildId: string): Promise<MessageResponse> {
  return request(`/api/admin/discord/guilds/${guildId}`, { method: "DELETE" });
}

export async function getGuild(guildId: string): Promise<GuildDetails | null> {
  const res = await fetch(`/api/admin/discord/guilds/${guildId}`, { credentials: "include" });
  if (res.status === 404) return null;
  if (!res.ok) throw new AdminApiError(`getGuild() failed with status ${res.status}`, res.status);
  return res.json();
}

export function publishStandingsToGuild(guildId: string, channelId: string): Promise<{ published: boolean; message: string }> {
  return postJson(`/api/admin/discord/guilds/${guildId}/channels/${encodeURIComponent(channelId)}/publish-standings`);
}

export function updateGuildChannelSubscriptions(
  guildId: string,
  channelId: string,
  subscriptions: EventSubscription[]
): Promise<MessageResponse> {
  return postJson(`/api/admin/discord/guilds/${guildId}/channels/${encodeURIComponent(channelId)}/subscriptions`, { subscriptions }, "PUT");
}

export function moveGuildChannel(guildId: string, channelId: string, newChannelId: string): Promise<MessageResponse> {
  return postJson(`/api/admin/discord/guilds/${guildId}/channels/${encodeURIComponent(channelId)}/channel`, { newChannelId }, "PUT");
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

// ---- Admin: error queues ----

export function getErrorQueues(): Promise<ErrorQueueSummary[]> {
  return request("/api/admin/errors/queues");
}

export function getErrorQueueMessages(queue: string): Promise<ErrorQueueMessage[]> {
  return request(`/api/admin/errors/queues/${encodeURIComponent(queue)}/messages`);
}

export function retryErrorMessage(queue: string, messageId: string): Promise<ErrorQueueJobAccepted> {
  return postJson(`/api/admin/errors/queues/${encodeURIComponent(queue)}/messages/${encodeURIComponent(messageId)}/retry`);
}

export function discardErrorMessage(queue: string, messageId: string): Promise<ErrorQueueJobAccepted> {
  return postJson(`/api/admin/errors/queues/${encodeURIComponent(queue)}/messages/${encodeURIComponent(messageId)}/discard`);
}

export function retryAllErrorMessages(queue: string): Promise<ErrorQueueJobAccepted> {
  return postJson(`/api/admin/errors/queues/${encodeURIComponent(queue)}/retry-all`);
}

export function purgeErrorQueue(queue: string): Promise<ErrorQueueJobAccepted> {
  return postJson(`/api/admin/errors/queues/${encodeURIComponent(queue)}/purge`);
}

export function getErrorQueueJob(jobId: string): Promise<ErrorQueueJobState> {
  return request(`/api/admin/errors/jobs/${encodeURIComponent(jobId)}`);
}

// Starts one of the mutating error-queue actions and resolves once it has actually finished,
// so callers can keep a single "doing it..." state and then show a real outcome. A job that
// finishes as Failed rejects, which puts it on the same error path as a failed request.
export async function runErrorQueueJob(
  start: () => Promise<ErrorQueueJobAccepted>,
  pollIntervalMs = 500,
): Promise<string> {
  const { jobId } = await start();
  for (;;) {
    const job = await getErrorQueueJob(jobId);
    if (job.status === "Succeeded") return job.message ?? "Done.";
    if (job.status === "Failed") throw new Error(job.message ?? "The job failed.");
    await new Promise((resolve) => setTimeout(resolve, pollIntervalMs));
  }
}
