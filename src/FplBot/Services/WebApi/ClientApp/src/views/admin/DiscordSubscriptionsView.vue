<script setup lang="ts">
import { ref } from "vue";
import {
  getDiscordServers,
  deleteChannelSubscription,
  deleteAllDiscordSubscriptionsForGuild,
  deleteDiscordGuild,
  getDiscordFailureStats,
  resetDiscordFailures,
  redirectToDiscordInstall,
} from "../../api/api";
import type { GuildWithSubs } from "../../api/types";
import { describeAdminError } from "../../composables/useAdminAuth";
import { useAdminListQuery } from "../../composables/useAdminListQuery";
import AdminPager from "../../components/AdminPager.vue";
import { failureSummary } from "../../api/deliveryFailures";
import type { ChannelFailureStats } from "../../api/types";

// The seeded throwaway workspace/server is a REAL one you can open; the other dev seeds are fake.
const isThrowaway = (name: string | null) =>
  import.meta.env.DEV && (name ?? "").toLowerCase().includes("throwaway");


const pageSize = 25;
const guilds = ref<GuildWithSubs[]>([]);
const totalCount = ref(0);
const loading = ref(true);
const error = ref("");
const deleting = ref<string | null>(null);

const failureStats = ref<ChannelFailureStats | null>(null);
const resettingFailures = ref(false);

async function loadFailureStats() {
  try {
    failureStats.value = await getDiscordFailureStats();
  } catch {
    failureStats.value = null;
  }
}

async function resetFailures() {
  const stats = failureStats.value;
  if (!stats || stats.channelsWithFailures === 0) return;
  if (!confirm(`Reset delivery failure counters for ${stats.channelsWithFailures} channel(s) across ${stats.installationsWithFailures} Discord server(s)? Subscriptions stay in place; only the failure history is cleared.`)) return;
  resettingFailures.value = true;
  error.value = "";
  try {
    await resetDiscordFailures();
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
    const result = await getDiscordServers(query.value, page.value, pageSize, failingOnly.value);
    guilds.value = result.items;
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

async function removeSub(subscriptionId: string) {
  const key = subscriptionId;
  deleting.value = key;
  error.value = "";
  try {
    await deleteChannelSubscription(subscriptionId);
    await load();
  } catch (e) {
    error.value = describeAdminError(e);
  } finally {
    deleting.value = null;
  }
}

async function removeAllSubs(installationId: string, guildId: string, guildName: string) {
  if (!confirm(`Delete all channel subscriptions for ${guildName} (${guildId})? The guild stays listed as installed.`)) return;
  const key = `guild-subs-${installationId}`;
  deleting.value = key;
  error.value = "";
  try {
    await deleteAllDiscordSubscriptionsForGuild(installationId);
    await load();
  } catch (e) {
    error.value = describeAdminError(e);
  } finally {
    deleting.value = null;
  }
}

async function removeGuild(installationId: string, guildId: string, guildName: string) {
  if (!confirm(`Delete ${guildName} (${guildId})? This forgets all of fplbot's tracked data for this server and removes the bot from it.`)) return;
  const key = `guild-${installationId}`;
  deleting.value = key;
  error.value = "";
  try {
    await deleteDiscordGuild(installationId);
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
    <h1>Discord servers</h1>
    <p class="lead">{{ totalCount }} server(s) with fplbot installed.</p>

    <button class="btn install-cta" @click="redirectToDiscordInstall('/admin/discord/servers')">Install new server&hellip;</button>

    <p v-if="failureStats" class="lead delivery-health">
      <template v-if="failureStats.channelsWithFailures > 0">
        &#9888; {{ failureStats.channelsWithFailures }} channel(s) across {{ failureStats.installationsWithFailures }} server(s) are failing delivery,
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
        <label for="guild-search">Search by server name or id</label>
        <input id="guild-search" v-model="query" type="text" placeholder="e.g. my server" />
      </div>

      <div class="field checkbox-field">
        <label for="guild-search-failing">
          <input id="guild-search-failing" v-model="failingOnly" type="checkbox" />
          Only show installations with delivery failures
        </label>
      </div>

      <p v-if="error" class="alert alert-error">{{ error }}</p>
      <div v-if="loading" class="spinner"></div>

      <template v-else>
        <AdminPager v-if="guilds.length > 0" :page="page" :total-pages="totalPages()" :total-count="totalCount" @update:page="goToPage" />

        <div class="guild-list">
        <div v-for="g in guilds" :key="g.id" class="guild" :class="{ throwaway: isThrowaway(g.guildName) }">
          <div class="guild-header">
            <h3>{{ g.guildName }} <span class="guild-id">({{ g.guildId }})</span></h3>
            <div class="guild-actions">
              <a
                v-if="isThrowaway(g.guildName)"
                :href="`https://discord.com/channels/${g.guildId}`"
                target="_blank"
                rel="noopener"
                class="btn small btn-secondary external"
              >Open in Discord</a>
              <router-link class="btn small btn-secondary" :to="{ name: 'admin-guild-details', params: { entityId: g.id } }">
                Edit
              </router-link>
              <div class="guild-actions-danger">
                <button
                  v-if="g.subscriptions.length > 0"
                  class="btn small danger"
                  :disabled="deleting === `guild-subs-${g.id}`"
                  @click="removeAllSubs(g.id, g.guildId, g.guildName)"
                >
                  Delete all subs
                </button>
                <button
                  class="btn small danger"
                  :disabled="deleting === `guild-${g.id}`"
                  @click="removeGuild(g.id, g.guildId, g.guildName)"
                >
                  Delete guild
                </button>
              </div>
            </div>
          </div>
          <table v-if="g.subscriptions.length > 0" class="admin-table">
            <thead>
              <tr>
                <th>Channel</th>
                <th>League</th>
                <th>Subscriptions</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="s in g.subscriptions" :key="s.id" :class="{ failing: s.failureCount > 0 }">
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
                    :to="{ name: 'admin-subscription-manage', params: { subscriptionId: s.id } }"
                  >
                    ✏️
                  </router-link>
                  <button
                    class="btn small danger icon-btn"
                    title="Delete channel subscription"
                    aria-label="Delete channel subscription"
                    :disabled="deleting === s.id"
                    @click="removeSub(s.id)"
                  >
                    ❌
                  </button>
                </td>
              </tr>
            </tbody>
          </table>
          <p v-else class="no-subs">No channel subscriptions.</p>
        </div>
        <p v-if="guilds.length === 0">No guilds found.</p>
        </div>

        <AdminPager v-if="guilds.length > 0" :page="page" :total-pages="totalPages()" :total-count="totalCount" @update:page="goToPage" />
      </template>
    </div>
  </div>
</template>

<style scoped>
.install-cta {
  display: block;
  width: 100%;
  margin-bottom: 1.5rem;
  padding: 1rem 1.5rem;
  font-size: 1.1rem;
  font-weight: 600;
}

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

.guild-list {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

.guild {
  background: #e9ebef;
  border: 1px solid #d1d5db;
  border-radius: 0.5rem;
  padding: 1rem;
}

/* The throwaway install is a REAL workspace/server, unlike the other dev seeds - make it
   obvious at a glance which row you are about to act on. */
.guild.throwaway {
  background: #fef3c7;
  border-color: #f59e0b;
  border-left-width: 4px;
}

.guild-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  margin-bottom: 0.75rem;
}

.guild-actions {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  flex-wrap: wrap;
}

.guild-actions-danger {
  display: flex;
  gap: 0.5rem;
  flex-wrap: wrap;
  margin-left: 1rem;
  padding-left: 1rem;
  border-left: 1px solid #fecaca;
}

.guild h3 {
  margin: 0;
  font-size: 1rem;
}

.guild-id {
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
