<script setup lang="ts">
import { ref } from "vue";
import {
  getDiscordServers,
  deleteDiscordSubscription,
  deleteAllDiscordSubscriptionsForGuild,
  deleteDiscordGuild,
} from "../../api/api";
import type { GuildWithSubs } from "../../api/types";
import { describeAdminError } from "../../composables/useAdminAuth";
import { useAdminListQuery } from "../../composables/useAdminListQuery";
import AdminPager from "../../components/AdminPager.vue";
import { failureSummary } from "../../api/deliveryFailures";

const pageSize = 25;
const guilds = ref<GuildWithSubs[]>([]);
const totalCount = ref(0);
const loading = ref(true);
const error = ref("");
const deleting = ref<string | null>(null);

async function load() {
  loading.value = true;
  error.value = "";
  try {
    const result = await getDiscordServers(query.value, page.value, pageSize);
    guilds.value = result.items;
    totalCount.value = result.totalCount;
  } catch (e) {
    error.value = describeAdminError(e);
  } finally {
    loading.value = false;
  }
}

const { query, page, goToPage } = useAdminListQuery(load);

const totalPages = () => Math.max(1, Math.ceil(totalCount.value / pageSize));

async function removeSub(guildId: string, channelId: string) {
  const key = `${guildId}-${channelId}`;
  deleting.value = key;
  error.value = "";
  try {
    await deleteDiscordSubscription(guildId, channelId);
    await load();
  } catch (e) {
    error.value = describeAdminError(e);
  } finally {
    deleting.value = null;
  }
}

async function removeAllSubs(guildId: string, guildName: string) {
  if (!confirm(`Delete all channel subscriptions for ${guildName} (${guildId})? The guild stays listed as installed.`)) return;
  const key = `guild-subs-${guildId}`;
  deleting.value = key;
  error.value = "";
  try {
    await deleteAllDiscordSubscriptionsForGuild(guildId);
    await load();
  } catch (e) {
    error.value = describeAdminError(e);
  } finally {
    deleting.value = null;
  }
}

async function removeGuild(guildId: string, guildName: string) {
  if (!confirm(`Delete ${guildName} (${guildId})? This forgets all of fplbot's tracked data for this server — it does not remove the bot from Discord.`)) return;
  const key = `guild-${guildId}`;
  deleting.value = key;
  error.value = "";
  try {
    await deleteDiscordGuild(guildId);
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

    <div class="card">
      <div class="field">
        <label for="guild-search">Search by server name or id</label>
        <input id="guild-search" v-model="query" type="text" placeholder="e.g. my server" />
      </div>

      <p v-if="error" class="alert alert-error">{{ error }}</p>
      <div v-if="loading" class="spinner"></div>

      <template v-else>
        <AdminPager v-if="guilds.length > 0" :page="page" :total-pages="totalPages()" :total-count="totalCount" @update:page="goToPage" />

        <div class="guild-list">
        <div v-for="g in guilds" :key="g.guildId" class="guild">
          <div class="guild-header">
            <h3>{{ g.guildName }} <span class="guild-id">({{ g.guildId }})</span></h3>
            <div class="guild-actions">
              <router-link class="btn small btn-secondary" :to="{ name: 'admin-guild-details', params: { entityId: g.guildId } }">
                Edit
              </router-link>
              <div class="guild-actions-danger">
                <button
                  v-if="g.subscriptions.length > 0"
                  class="btn small danger"
                  :disabled="deleting === `guild-subs-${g.guildId}`"
                  @click="removeAllSubs(g.guildId, g.guildName)"
                >
                  Delete all subs
                </button>
                <button
                  class="btn small danger"
                  :disabled="deleting === `guild-${g.guildId}`"
                  @click="removeGuild(g.guildId, g.guildName)"
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
              <tr v-for="s in g.subscriptions" :key="s.channelId" :class="{ failing: s.failureCount > 0 }">
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
                    :to="{ name: 'admin-guild-channel-manage', params: { entityId: g.guildId, channelId: s.channelId } }"
                  >
                    ✏️
                  </router-link>
                  <button
                    class="btn small danger icon-btn"
                    title="Delete channel subscription"
                    aria-label="Delete channel subscription"
                    :disabled="deleting === `${g.guildId}-${s.channelId}`"
                    @click="removeSub(g.guildId, s.channelId)"
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
.lead {
  color: #6b7280;
  margin-bottom: 1rem;
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
