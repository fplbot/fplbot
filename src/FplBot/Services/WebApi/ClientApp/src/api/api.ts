import type {
  AdminMe,
  AvailableChannel,
  Bookmarks,
  ChannelFailureStats,
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
  LeagueSearchResponse,
  LeagueSearchResult,
  LeagueSummary,
  MessageResponse,
  PagedResult,
  SearchAnalyticsResult,
  SearchAnyResponse,
  SearchAnyResult,
  SearchType,
  SlashCommandDefinition,
  SubscriberDetail,
  SubscriberSummary,
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

export type AdminAuthProvider = "slack" | "discord";

export function loginUrl(provider: AdminAuthProvider, returnUrl?: string): string {
  const params = new URLSearchParams({ provider });
  if (returnUrl) params.set("returnUrl", returnUrl);
  return `/api/admin/login?${params.toString()}`;
}

export async function logout(): Promise<void> {
  await request<void>("/api/admin/logout", { method: "POST" });
}

// ---- Admin: Slack teams ----

export function getTeams(query: string, page: number, pageSize: number, failingOnly = false): Promise<PagedResult<TeamSummary>> {
  const params = new URLSearchParams({ query, page: String(page), pageSize: String(pageSize), failingOnly: String(failingOnly) });
  return request(`/api/admin/teams?${params.toString()}`);
}

export async function getTeam(installationId: string): Promise<TeamDetails | null> {
  const res = await fetch(`/api/admin/teams/${installationId}`, { credentials: "include" });
  if (res.status === 404) return null;
  if (!res.ok) throw new AdminApiError(`getTeam() failed with status ${res.status}`, res.status);
  return res.json();
}

export function uninstallTeam(installationId: string): Promise<MessageResponse> {
  return postJson(`/api/admin/teams/${installationId}/uninstall`);
}

export type PublishableEvent =
  | "Standings"
  | "GameweekStarted"
  | "Deadline24Hours"
  | "Deadline1Hour"
  | "FixtureEvents"
  | "FixtureFullTime"
  | "Lineups";

export function publishSubscriptionEvent(
  subscriptionId: string,
  eventName: PublishableEvent
): Promise<{ published: boolean; message: string }> {
  return postJson(`/api/admin/subscriptions/${subscriptionId}/publish/${eventName}`);
}

export function broadcastToSlack(message: string): Promise<MessageResponse> {
  return postJson("/api/admin/slack/broadcast", { message });
}

export const ALL_EVENT_SUBSCRIPTIONS: EventSubscription[] = [
  "All", "Standings", "Captains", "Transfers", "FixtureGoals", "FixtureAssists", "FixtureCards",
  "FixturePenaltyMisses", "FixtureFullTime", "Taunts", "PriceChanges", "InjuryUpdates", "Deadlines",
  "Lineups", "NewPlayers", "FixtureRemovedFromGameweek",
];

export function updateChannelSubscriptions(subscriptionId: string, subscriptions: EventSubscription[]): Promise<MessageResponse> {
  return postJson(`/api/admin/subscriptions/${subscriptionId}/subscriptions`, { subscriptions }, "PUT");
}

export function getAvailableChannels(installationId: string): Promise<AvailableChannel[]> {
  return request(`/api/admin/teams/${installationId}/available-channels`);
}

export function followLeague(subscriptionId: string, leagueId: number): Promise<MessageResponse> {
  return postJson(`/api/admin/subscriptions/${subscriptionId}/league`, { leagueId }, "PUT");
}

export function addChannelSubscription(installationId: string, channelId: string): Promise<MessageResponse> {
  return postJson(`/api/admin/teams/${installationId}/channels`, { channelId });
}

export function unfollowLeague(subscriptionId: string): Promise<MessageResponse> {
  return request(`/api/admin/subscriptions/${subscriptionId}/league`, { method: "DELETE" });
}

export function moveChannel(subscriptionId: string, newChannelId: string): Promise<MessageResponse> {
  return postJson(`/api/admin/subscriptions/${subscriptionId}/channel`, { newChannelId }, "PUT");
}

export function getSubscriptionInstallation(subscriptionId: string): Promise<{ installationId: string; platform: "Slack" | "Discord" }> {
  return request(`/api/admin/subscriptions/${subscriptionId}`);
}

export function deleteChannelSubscription(subscriptionId: string): Promise<MessageResponse> {
  return request(`/api/admin/subscriptions/${subscriptionId}`, { method: "DELETE" });
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

export function getDiscordServers(query: string, page: number, pageSize: number, failingOnly = false): Promise<PagedResult<GuildWithSubs>> {
  const params = new URLSearchParams({ query, page: String(page), pageSize: String(pageSize), failingOnly: String(failingOnly) });
  return request(`/api/admin/discord/servers?${params.toString()}`);
}
export function deleteAllDiscordSubscriptionsForGuild(installationId: string): Promise<MessageResponse> {
  return request(`/api/admin/discord/guilds/${installationId}/subscriptions`, { method: "DELETE" });
}

export function getDiscordFailureStats(): Promise<ChannelFailureStats> {
  return request(`/api/admin/discord/failures`);
}

export function resetDiscordFailures(): Promise<{ cleared: number }> {
  return request(`/api/admin/discord/failures/reset`, { method: "POST" });
}

export function getSlackFailureStats(): Promise<ChannelFailureStats> {
  return request(`/api/admin/slack/failures`);
}

export function resetSlackFailures(): Promise<{ cleared: number }> {
  return request(`/api/admin/slack/failures/reset`, { method: "POST" });
}

export function deleteDiscordGuild(installationId: string): Promise<MessageResponse> {
  return request(`/api/admin/discord/guilds/${installationId}`, { method: "DELETE" });
}

export async function getGuild(installationId: string): Promise<GuildDetails | null> {
  const res = await fetch(`/api/admin/discord/guilds/${installationId}`, { credentials: "include" });
  if (res.status === 404) return null;
  if (!res.ok) throw new AdminApiError(`getGuild() failed with status ${res.status}`, res.status);
  return res.json();
}
export function getAvailableGuildChannels(installationId: string): Promise<AvailableChannel[]> {
  return request(`/api/admin/discord/guilds/${installationId}/available-channels`);
}
export function addGuildChannelSubscription(installationId: string, channelId: string): Promise<MessageResponse> {
  return postJson(`/api/admin/discord/guilds/${installationId}/channels`, { channelId });
}

// ---- Admin: web push ----

export function getWebPushSubscribers(page: number, pageSize: number): Promise<PagedResult<SubscriberSummary>> {
  const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
  return request(`/api/admin/web/subscribers?${params.toString()}`);
}

export async function getWebPushSubscriber(subscriberId: string): Promise<SubscriberDetail | null> {
  const res = await fetch(`/api/admin/web/subscribers/${subscriberId}`, { credentials: "include" });
  if (res.status === 404) return null;
  if (!res.ok) throw new AdminApiError(`getWebPushSubscriber() failed with status ${res.status}`, res.status);
  return res.json();
}

export function updateWebPushSubscriberEvents(subscriberId: string, events: string[]): Promise<SubscriberDetail> {
  return postJson(`/api/admin/web/subscribers/${subscriberId}/events`, { events }, "PUT");
}

export function updateWebPushSubscriberLeague(subscriberId: string, leagueId: number | null): Promise<SubscriberDetail> {
  return postJson(`/api/admin/web/subscribers/${subscriberId}/league`, { leagueId }, "PUT");
}

export function deleteWebPushSubscriber(subscriberId: string): Promise<void> {
  return request(`/api/admin/web/subscribers/${subscriberId}`, { method: "DELETE" });
}

export function publishWebPushEvent(
  subscriberId: string,
  eventName: PublishableEvent
): Promise<{ published: boolean; message: string }> {
  return postJson(`/api/admin/web/subscribers/${subscriberId}/publish/${eventName}`);
}

export function broadcastToWebPush(title: string, body: string): Promise<void> {
  return postJson("/api/admin/web/broadcast", { title, body });
}

// ---- OAuth (public site install buttons) ----

export async function redirectToSlackInstall(returnTo?: string): Promise<void> {
  const query = returnTo ? `?returnTo=${encodeURIComponent(returnTo)}` : "";
  const res = await fetch(`/api/oauth/install-url/slack${query}`);
  const data: InstallUrlResponse = await res.json();
  window.location.href = data.redirectUri;
}

export async function redirectToDiscordInstall(returnTo?: string): Promise<void> {
  const query = returnTo ? `?returnTo=${encodeURIComponent(returnTo)}` : "";
  const res = await fetch(`/api/oauth/install-url/discord${query}`);
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

export async function searchLeagues(query: string, page: number): Promise<LeagueSearchResult> {
  const params = new URLSearchParams({ query, page: String(page), countryToBoost: "" });
  const res = await fetch(`/api/search/leagues?${params.toString()}`);
  if (!res.ok) {
    throw new Error(`League search request failed with status ${res.status}`);
  }
  const data: LeagueSearchResponse = await res.json();
  return data.hits;
}

// ---- League details (public site) ----

export async function getLeague(leagueId: number): Promise<LeagueSummary | null> {
  const res = await fetch(`/api/fpl/leagues/${leagueId}`);
  if (res.status === 404) {
    return null;
  }
  if (!res.ok) {
    throw new Error(`League request failed with status ${res.status}`);
  }
  return res.json();
}


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
    let job: ErrorQueueJobState;
    try {
      job = await getErrorQueueJob(jobId);
    } catch (e) {
      // Jobs live in memory on the instance that accepted them, so a restart mid-run loses the
      // record. Say that, rather than surfacing a bare 404 — the exact thing this flow replaced.
      if (e instanceof AdminApiError && e.status === 404) {
        throw new Error("This action is no longer being tracked (the server may have restarted). Refresh to see the current state of the queue.");
      }
      throw e;
    }
    if (job.status === "Succeeded") return job.message ?? "Done.";
    if (job.status === "Failed") throw new Error(job.message ?? "The job failed.");
    await new Promise((resolve) => setTimeout(resolve, pollIntervalMs));
  }
}
