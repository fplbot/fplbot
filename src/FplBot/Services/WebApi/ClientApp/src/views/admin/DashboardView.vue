<script setup lang="ts">
import { ref, onMounted } from "vue";
import {
  getDiscordReachStats,
  getDiscordServers,
  getDiscordSizeDistribution,
  getSlackReachStats,
  getWebPushSubscribers,
  refreshDiscordReachStats,
  refreshSlackReachStats,
} from "../../api/api";
import type { GuildReachStats, GuildSizeBucket, GuildWithSubs, TeamReachStats } from "../../api/types";
import { describeAdminError } from "../../composables/useAdminAuth";
import { formatNumber, formatRelativeTime } from "../../formatting";
import StatTile from "../../components/StatTile.vue";
import GuildSizeDistributionChart from "../../components/GuildSizeDistributionChart.vue";

const loading = ref(true);
const error = ref("");
const discordReach = ref<GuildReachStats | null>(null);
const slackReach = ref<TeamReachStats | null>(null);
const webPushSubscriberCount = ref<number | null>(null);
const topDiscordServers = ref<GuildWithSubs[]>([]);
const guildSizeBuckets = ref<GuildSizeBucket[]>([]);

const refreshingDiscord = ref(false);
const refreshingSlack = ref(false);
const discordRefreshMessage = ref("");
const slackRefreshMessage = ref("");

async function load() {
  loading.value = true;
  error.value = "";
  try {
    const [discord, slack, webPush, topServers, sizeDistribution] = await Promise.all([
      getDiscordReachStats(),
      getSlackReachStats(),
      getWebPushSubscribers(0, 1),
      getDiscordServers("", 1, 10, false, undefined, true),
      getDiscordSizeDistribution(),
    ]);
    discordReach.value = discord;
    slackReach.value = slack;
    webPushSubscriberCount.value = webPush.totalCount;
    topDiscordServers.value = topServers.items;
    guildSizeBuckets.value = sizeDistribution;
  } catch (e) {
    error.value = describeAdminError(e);
  } finally {
    loading.value = false;
  }
}

async function triggerDiscordRefresh() {
  refreshingDiscord.value = true;
  discordRefreshMessage.value = "";
  try {
    await refreshDiscordReachStats();
    discordRefreshMessage.value = "Refresh queued — runs in the background, check back shortly.";
  } catch (e) {
    discordRefreshMessage.value = describeAdminError(e);
  } finally {
    refreshingDiscord.value = false;
  }
}

async function triggerSlackRefresh() {
  refreshingSlack.value = true;
  slackRefreshMessage.value = "";
  try {
    await refreshSlackReachStats();
    slackRefreshMessage.value = "Refresh queued — runs in the background, check back shortly.";
  } catch (e) {
    slackRefreshMessage.value = describeAdminError(e);
  } finally {
    refreshingSlack.value = false;
  }
}

onMounted(load);
</script>

<template>
  <div>
    <h1>Dashboard</h1>

    <p v-if="error" class="alert alert-error">{{ error }}</p>
    <div v-if="loading" class="spinner"></div>

    <div v-else class="stat-grid">
      <StatTile v-if="discordReach" label="Total Discord servers" :value="formatNumber(discordReach.totalGuilds)" />

      <StatTile
        v-if="discordReach"
        label="Community Discord servers"
        :value="`${formatNumber(discordReach.communityGuilds)} / ${formatNumber(discordReach.totalGuilds)}`"
        sublabel="Servers with Discord's COMMUNITY feature enabled (discovery-eligible) vs. all installed servers. Not-yet-swept servers count as non-community."
      />

      <StatTile
        v-if="discordReach"
        label="Total Discord reach"
        :value="formatNumber(discordReach.totalApproximateMembers)"
        sublabel="Includes bot accounts, not just people. Servers not yet counted contribute zero."
      >
        <div class="stat-footer">
          <span class="stat-updated">Last updated: {{ formatRelativeTime(discordReach.oldestUpdate) }}</span>
          <button class="btn-link" :disabled="refreshingDiscord" @click="triggerDiscordRefresh">
            {{ refreshingDiscord ? "Queuing…" : "Refresh now" }}
          </button>
          <span v-if="discordRefreshMessage" class="stat-refresh-message">{{ discordRefreshMessage }}</span>
        </div>
      </StatTile>

      <StatTile v-if="slackReach" label="Total Slack workspaces" :value="formatNumber(slackReach.totalTeams)" />

      <StatTile
        v-if="slackReach"
        label="Total Slack reach"
        :value="formatNumber(slackReach.totalApproximateMembers)"
        sublabel="Sums each subscribed channel's own member count. A user in multiple channels of the same workspace is counted more than once."
      >
        <div class="stat-footer">
          <span class="stat-updated">Last updated: {{ formatRelativeTime(slackReach.oldestUpdate) }}</span>
          <button class="btn-link" :disabled="refreshingSlack" @click="triggerSlackRefresh">
            {{ refreshingSlack ? "Queuing…" : "Refresh now" }}
          </button>
          <span v-if="slackRefreshMessage" class="stat-refresh-message">{{ slackRefreshMessage }}</span>
        </div>
      </StatTile>

      <StatTile
        v-if="webPushSubscriberCount !== null"
        label="Total Web Push subscribers"
        :value="formatNumber(webPushSubscriberCount)"
      />
    </div>

    <div v-if="!loading && topDiscordServers.length > 0" class="card top-servers">
      <div class="top-servers-header">
        <h2>Top 10 Discord servers</h2>
        <router-link
          :to="{ name: 'admin-discord-servers', query: { sortByMembers: '1' } }"
          class="btn-link"
        >
          See full list sorted by size&hellip;
        </router-link>
      </div>
      <table class="admin-table">
        <thead>
          <tr>
            <th>Server</th>
            <th>Members</th>
            <th>Type</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="g in topDiscordServers" :key="g.id">
            <td>{{ g.guildName }} <span class="guild-id">({{ g.guildId }})</span></td>
            <td>{{ g.approximateMemberCount !== null ? formatNumber(g.approximateMemberCount) : "—" }}</td>
            <td>{{ g.isCommunity === null ? "—" : g.isCommunity ? "Community" : "Private" }}</td>
            <td class="row-actions">
              <a
                :href="`https://discord.com/channels/${g.guildId}`"
                target="_blank"
                rel="noopener"
                class="btn small btn-secondary"
              >Open in Discord</a>
              <router-link class="btn small btn-secondary" :to="{ name: 'admin-guild-details', params: { entityId: g.id } }">
                Manage
              </router-link>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <GuildSizeDistributionChart v-if="!loading && guildSizeBuckets.length > 0" :buckets="guildSizeBuckets" />
  </div>
</template>

<style scoped>
.stat-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(14rem, 1fr));
  gap: 1.5rem;
}

.stat-footer {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: 0.25rem;
  margin-top: 0.25rem;
}

.stat-updated {
  font-size: 0.75rem;
  color: #6b7280;
}

.btn-link {
  background: none;
  border: none;
  padding: 0;
  font-size: 0.8rem;
  color: var(--fpl-purple);
  cursor: pointer;
  text-decoration: underline;
}

.btn-link:disabled {
  color: #9ca3af;
  cursor: not-allowed;
  text-decoration: none;
}

.stat-refresh-message {
  font-size: 0.75rem;
  color: #6b7280;
}

.top-servers {
  margin-top: 2rem;
}

.top-servers-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  margin-bottom: 1rem;
}

.top-servers-header h2 {
  margin: 0;
  font-size: 1.1rem;
}

.guild-id {
  font-weight: normal;
  color: #6b7280;
  font-size: 0.85rem;
}

.row-actions {
  display: flex;
  gap: 0.5rem;
  flex-wrap: wrap;
}
</style>
