<script setup lang="ts">
import { ref, onMounted } from "vue";
import {
  getDiscordReachStats,
  getSlackReachStats,
  getWebPushSubscribers,
  refreshDiscordReachStats,
  refreshSlackReachStats,
} from "../../api/api";
import type { GuildReachStats, TeamReachStats } from "../../api/types";
import { describeAdminError } from "../../composables/useAdminAuth";
import { formatNumber, formatRelativeTime } from "../../formatting";
import StatTile from "../../components/StatTile.vue";

const loading = ref(true);
const error = ref("");
const discordReach = ref<GuildReachStats | null>(null);
const slackReach = ref<TeamReachStats | null>(null);
const webPushSubscriberCount = ref<number | null>(null);

const refreshingDiscord = ref(false);
const refreshingSlack = ref(false);
const discordRefreshMessage = ref("");
const slackRefreshMessage = ref("");

async function load() {
  loading.value = true;
  error.value = "";
  try {
    const [discord, slack, webPush] = await Promise.all([
      getDiscordReachStats(),
      getSlackReachStats(),
      getWebPushSubscribers(0, 1),
    ]);
    discordReach.value = discord;
    slackReach.value = slack;
    webPushSubscriberCount.value = webPush.totalCount;
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
</style>
