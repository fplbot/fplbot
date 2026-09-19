import {
  getTeam,
  uninstallTeam,
  addChannelSubscription,
  getAvailableChannels,
  getGuild,
  deleteDiscordGuild,
  addGuildChannelSubscription,
  getAvailableGuildChannels,
} from "../api/api";
import type { AvailableChannel, EventSubscription, MessageResponse } from "../api/types";

export interface EntityChannel {
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

export interface EntityDetails {
  id: string;
  externalId: string;
  name: string | null;
  token?: string | null;
  pendingRemoval?: boolean;
  channels: EntityChannel[];
}

// The Slack/Discord admin "manage details" flows are identical in shape (an overview,
// a channel table with per-channel manage/delete, a danger zone) — this adapter is what
// lets InstallationDetailsView.vue / ChannelManageView.vue stay platform-agnostic, with
// only the handful of real differences (token+soft-uninstall exist for Slack; Discord
// only supports a hard delete) expressed here instead of duplicated across two views.
export interface InstallationAdapter {
  apiLabel: string;
  platformName: string;
  devCallout: string;
  entityNoun: string;
  channelNotVisibleHint: string;
  notListedButDeliveringHint?: string;
  listRoute: string;
  backLinkLabel: string;
  detailsRouteName: string;
  manageRouteName: string;
  showOverview: boolean;
  danger: "uninstall" | "delete";
  appUrl(externalId: string): string;
  channelUrl(externalId: string, channelId: string): string;
  getDetails(id: string): Promise<EntityDetails | null>;
  getAvailableChannels(id: string): Promise<AvailableChannel[]>;
  addChannelSubscription(id: string, channelId: string): Promise<MessageResponse>;
  uninstall?(id: string): Promise<MessageResponse>;
  deleteEntity?(id: string): Promise<MessageResponse>;
}

export const slackInstallationAdapter: InstallationAdapter = {
  apiLabel: "Slack API",
  platformName: "Slack",
  devCallout: "This is a real Slack Workspace you have access to for dev-purposes.",
  entityNoun: "workspace",
  channelNotVisibleHint:
    "conversations.list only returns public channels, so a private channel the bot posts in looks like this and is fine. Otherwise the channel was archived or deleted, or the bot was removed from the workspace.",
  notListedButDeliveringHint: "likely a private channel",
  listRoute: "/admin/slack",
  backLinkLabel: "Back to workspaces",
  detailsRouteName: "admin-team-details",
  manageRouteName: "admin-subscription-manage",
  showOverview: true,
  danger: "uninstall",
  appUrl: (externalId) => `https://app.slack.com/client/${externalId}`,
  channelUrl: (externalId, channelId) => `https://app.slack.com/client/${externalId}/${channelId}`,
  async getDetails(id) {
    const data = await getTeam(id);
    if (data == null) return null;
    return { id: data.id, externalId: data.teamId, name: data.teamName, token: data.token, pendingRemoval: data.pendingRemoval, channels: data.channels };
  },
  addChannelSubscription,
  getAvailableChannels,
  uninstall: uninstallTeam,
};

export const discordInstallationAdapter: InstallationAdapter = {
  apiLabel: "Discord API",
  platformName: "Discord",
  devCallout: "This is a real DevOnly Discord server you have access to for dev-purposes.",
  entityNoun: "server",
  channelNotVisibleHint:
    "The bot did not get this channel back from the guild channel list — the channel was deleted, or the bot lost the permission to view it.",
  listRoute: "/admin/discord/servers",
  backLinkLabel: "Back to guilds",
  detailsRouteName: "admin-guild-details",
  manageRouteName: "admin-subscription-manage",
  showOverview: false,
  danger: "delete",
  appUrl: (externalId) => `https://discord.com/channels/${externalId}`,
  channelUrl: (externalId, channelId) => `https://discord.com/channels/${externalId}/${channelId}`,
  async getDetails(id) {
    const data = await getGuild(id);
    if (data == null) return null;
    return { id: data.id, externalId: data.guildId, name: data.guildName, channels: data.channels };
  },
  getAvailableChannels: getAvailableGuildChannels,
  addChannelSubscription: addGuildChannelSubscription,
  deleteEntity: deleteDiscordGuild,
};
