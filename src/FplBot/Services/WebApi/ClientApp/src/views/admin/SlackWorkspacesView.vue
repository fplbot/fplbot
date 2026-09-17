<script setup lang="ts">
import { ref } from "vue";
import { getTeams, uninstallTeam, deleteChannelSubscription, getSlackFailureStats, resetSlackFailures } from "../../api/api";
import type { TeamSummary } from "../../api/types";
import { describeAdminError } from "../../composables/useAdminAuth";
import { useAdminListQuery } from "../../composables/useAdminListQuery";
import AdminPager from "../../components/AdminPager.vue";
import { failureSummary } from "../../api/deliveryFailures";
import type { ChannelFailureStats } from "../../api/types";

const pageSize = 25;
const teams = ref<TeamSummary[]>([]);
const totalCount = ref(0);
const loading = ref(true);
const error = ref("");
const uninstalling = ref<string | null>(null);
const deleting = ref<string | null>(null);

const failureStats = ref<ChannelFailureStats | null>(null);
const resettingFailures = ref(false);

async function loadFailureStats() {
  try {
    failureStats.value = await getSlackFailureStats();
  } catch {
    failureStats.value = null;
  }
}

async function resetFailures() {
  const stats = failureStats.value;
  if (!stats || stats.channelsWithFailures === 0) return;
  if (!confirm(`Reset delivery failure counters for ${stats.channelsWithFailures} channel(s) across ${stats.installationsWithFailures} Slack workspace(s)? Subscriptions stay in place; only the failure history is cleared.`)) return;
  resettingFailures.value = true;
  error.value = "";
  try {
    await resetSlackFailures();
    await loadFailureStats();
    await load();
  } catch (e) {
    error.value = describeAdminError(e);
  } finally {
    resettingFailures.value = false;
  }
}

async function load() {
  loading.value = true;
  error.value = "";
  try {
    const result = await getTeams(query.value, page.value, pageSize, failingOnly.value);
    teams.value = result.items;
    totalCount.value = result.totalCount;
  } catch (e) {
    error.value = describeAdminError(e);
  } finally {
    loading.value = false;
  }
}

const { query, page, failingOnly, goToPage } = useAdminListQuery(load);

void loadFailureStats();

const totalPages = () => Math.max(1, Math.ceil(totalCount.value / pageSize));

async function submitUninstall(team: TeamSummary) {
  if (!confirm(`Uninstall fplbot from ${team.teamName}? This cannot be undone.`)) return;
  uninstalling.value = team.teamId;
  error.value = "";
  try {
    await uninstallTeam(team.teamId);
    await load();
  } catch (e) {
    error.value = describeAdminError(e);
  } finally {
    uninstalling.value = null;
  }
}

async function removeSub(teamId: string, channelId: string) {
  const key = `${teamId}-${channelId}`;
  deleting.value = key;
  error.value = "";
  try {
    await deleteChannelSubscription(teamId, channelId);
    await load();
  } catch (e) {
    error.value = describeAdminError(e);
  } finally {
    deleting.value = null;
  }
}
</script>

<template>
  <div>
    <h1>Slack workspaces</h1>
    <p class="lead">{{ totalCount }} workspace(s) with fplbot installed.</p>

    <p v-if="failureStats" class="lead delivery-health">
      <template v-if="failureStats.channelsWithFailures > 0">
        &#9888; {{ failureStats.channelsWithFailures }} channel(s) across {{ failureStats.installationsWithFailures }} workspace(s) are failing delivery,
        {{ failureStats.channelsEligibleForPurge }} of them already due to be purged.
      </template>
      <template v-else>No channels are currently failing delivery.</template>
      <button
        class="btn small danger"
        :disabled="resettingFailures || failureStats.channelsWithFailures === 0"
        @click="resetFailures"
      >
        {{ resettingFailures ? "Resetting..." : "Reset all failure counters" }}
      </button>
    </p>

    <div class="card">
      <div class="field">
        <label for="team-search">Search by team name or id</label>
        <input id="team-search" v-model="query" type="text" placeholder="e.g. Blank" />
      </div>

      <div class="field checkbox-field">
        <label for="team-search-failing">
          <input id="team-search-failing" v-model="failingOnly" type="checkbox" />
          Only show installations with delivery failures
        </label>
      </div>

      <p v-if="error" class="alert alert-error">{{ error }}</p>
      <div v-if="loading" class="spinner"></div>

      <template v-else>
        <AdminPager v-if="teams.length > 0" :page="page" :total-pages="totalPages()" :total-count="totalCount" @update:page="goToPage" />

        <div class="team-list">
        <div v-for="t in teams" :key="t.teamId" class="team">
          <div class="team-header">
            <h3>{{ t.teamName }} <span class="team-id">({{ t.teamId }})</span></h3>
            <div class="team-actions">
              <span v-if="t.pendingRemoval" class="status bad">Pending removal</span>
              <router-link :to="`/admin/teams/${t.teamId}`" class="btn small btn-secondary">Edit</router-link>
              <button
                class="btn small danger"
                :disabled="t.pendingRemoval || uninstalling === t.teamId"
                @click="submitUninstall(t)"
              >
                {{ uninstalling === t.teamId ? "Uninstalling..." : "Uninstall" }}
              </button>
            </div>
          </div>
          <table v-if="t.subscriptions.length > 0" class="admin-table">
            <thead>
              <tr>
                <th>Channel</th>
                <th>League</th>
                <th>Subscriptions</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="s in t.subscriptions" :key="s.channelId" :class="{ failing: s.failureCount > 0 }">
                <td>
                  {{ s.channelId }}
                  <span v-if="s.failureCount > 0" :title="failureSummary(s)">⚠️</span>
                </td>
                <td>{{ s.leagueId || "—" }}</td>
                <td>{{ s.subscriptions.join(", ") || "—" }}</td>
                <td class="row-actions">
                  <router-link
                    class="btn small icon-btn"
                    title="Manage channel"
                    aria-label="Manage channel"
                    :to="{ name: 'admin-team-channel-manage', params: { entityId: t.teamId, channelId: s.channelId } }"
                  >
                    ✏️
                  </router-link>
                  <button
                    class="btn small danger icon-btn"
                    title="Delete channel subscription"
                    aria-label="Delete channel subscription"
                    :disabled="deleting === `${t.teamId}-${s.channelId}`"
                    @click="removeSub(t.teamId, s.channelId)"
                  >
                    ❌
                  </button>
                </td>
              </tr>
            </tbody>
          </table>
          <p v-else class="no-subs">No channel subscriptions.</p>
        </div>
        <p v-if="teams.length === 0">No workspaces found.</p>
        </div>

        <AdminPager v-if="teams.length > 0" :page="page" :total-pages="totalPages()" :total-count="totalCount" @update:page="goToPage" />
      </template>
    </div>
  </div>
</template>

<style scoped>
.lead {
  color: #6b7280;
  margin-bottom: 1rem;
}

.delivery-health {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  flex-wrap: wrap;
}

.status.bad {
  color: #dc2626;
  font-size: 0.85rem;
}

.team-list {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

.team {
  background: #e9ebef;
  border: 1px solid #d1d5db;
  border-radius: 0.5rem;
  padding: 1rem;
}

.team-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  margin-bottom: 0.75rem;
}

.team-actions {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  flex-wrap: wrap;
}

.team h3 {
  margin: 0;
  font-size: 1rem;
}

.team-id {
  font-weight: normal;
  color: #6b7280;
  font-size: 0.85rem;
}

.no-subs {
  color: #6b7280;
  font-style: italic;
  font-size: 0.9rem;
}

.row-actions {
  display: flex;
  gap: 0.5rem;
  flex-wrap: wrap;
  align-items: center;
}

.icon-btn {
  padding: 0.3rem 0.5rem;
  line-height: 1;
}

.icon-btn:not(.danger) {
  background: #f3f4f6;
  color: inherit;
  border: 1px solid #d1d5db;
}

.icon-btn:not(.danger):hover {
  background: #e5e7eb;
  color: inherit;
}

.btn-secondary {
  background: #f3f4f6;
  color: inherit;
  border: 1px solid #d1d5db;
}

.btn-secondary:hover {
  background: #e5e7eb;
  color: inherit;
}
</style>
