import {
  getTeam,
  uninstallTeam,
  updateChannelSubscriptions,
  moveChannel,
  deleteChannelSubscription,
  publishStandings,
  getGuild,
  deleteDiscordGuild,
  updateGuildChannelSubscriptions,
  moveGuildChannel,
  deleteDiscordSubscription,
  publishStandingsToGuild,
} from "../api/api";
import type { EventSubscription, MessageResponse } from "../api/types";

export interface EntityChannel {
  channel: string;
  leagueId: number | null;
  leagueName: string | null;
  subscriptions: EventSubscription[];
  channelStatus: boolean | null;
  failureCount: number;
  failingSince: string | null;
  lastFailureReason: string | null;
  purgeEligibleAt: string | null;
  failuresUntilPurge: number;
}

export interface EntityDetails {
  id: string;
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
  listRoute: string;
  backLinkLabel: string;
  detailsRouteName: string;
  manageRouteName: string;
  showOverview: boolean;
  danger: "uninstall" | "delete";
  getDetails(id: string): Promise<EntityDetails | null>;
  updateChannelSubscriptions(id: string, channelId: string, subscriptions: EventSubscription[]): Promise<MessageResponse>;
  moveChannel(id: string, channelId: string, newChannelId: string): Promise<MessageResponse>;
  deleteChannelSubscription(id: string, channelId: string): Promise<MessageResponse>;
  publishStandings(id: string, channelId: string): Promise<{ published: boolean; message: string }>;
  uninstall?(id: string): Promise<MessageResponse>;
  deleteEntity?(id: string): Promise<MessageResponse>;
}

export const slackInstallationAdapter: InstallationAdapter = {
  apiLabel: "Slack API",
  listRoute: "/admin/slack",
  backLinkLabel: "Back to workspaces",
  detailsRouteName: "admin-team-details",
  manageRouteName: "admin-team-channel-manage",
  showOverview: true,
  danger: "uninstall",
  async getDetails(id) {
    const data = await getTeam(id);
    if (data == null) return null;
    return { id: data.teamId, name: data.teamName, token: data.token, pendingRemoval: data.pendingRemoval, channels: data.channels };
  },
  updateChannelSubscriptions,
  moveChannel,
  deleteChannelSubscription,
  publishStandings,
  uninstall: uninstallTeam,
};

export const discordInstallationAdapter: InstallationAdapter = {
  apiLabel: "Discord API",
  listRoute: "/admin/discord/servers",
  backLinkLabel: "Back to guilds",
  detailsRouteName: "admin-guild-details",
  manageRouteName: "admin-guild-channel-manage",
  showOverview: false,
  danger: "delete",
  async getDetails(id) {
    const data = await getGuild(id);
    if (data == null) return null;
    return { id: data.guildId, name: data.guildName, channels: data.channels };
  },
  updateChannelSubscriptions: updateGuildChannelSubscriptions,
  moveChannel: moveGuildChannel,
  deleteChannelSubscription: deleteDiscordSubscription,
  publishStandings: publishStandingsToGuild,
  deleteEntity: deleteDiscordGuild,
};
