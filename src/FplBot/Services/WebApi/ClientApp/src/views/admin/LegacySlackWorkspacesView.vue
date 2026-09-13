<script setup lang="ts">
import { ref, watch, onMounted } from "vue";
import { getLegacyTeams, migrateTeamToV2 } from "../../api/api";
import type { TeamSummary } from "../../api/types";
import { describeAdminError } from "../../composables/useAdminAuth";

const query = ref("");
const page = ref(1);
const pageSize = 25;
const teams = ref<TeamSummary[]>([]);
const totalCount = ref(0);
const loading = ref(true);
const error = ref("");
const migrating = ref<string | null>(null);
const migrateFeedback = ref<{ teamId: string; type: "success" | "error"; text: string } | null>(null);

const migratingAll = ref(false);
const migrateAllFeedback = ref<{ type: "success" | "error"; text: string } | null>(null);

async function load() {
  loading.value = true;
  error.value = "";
  try {
    const result = await getLegacyTeams(query.value, page.value, pageSize);
    teams.value = result.items;
    totalCount.value = result.totalCount;
  } catch (e) {
    error.value = describeAdminError(e);
  } finally {
    loading.value = false;
  }
}

onMounted(load);
watch(query, () => {
  page.value = 1;
  load();
});
watch(page, load);

const totalPages = () => Math.max(1, Math.ceil(totalCount.value / pageSize));

async function migrate(teamId: string) {
  migrating.value = teamId;
  migrateFeedback.value = null;
  try {
    const res = await migrateTeamToV2(teamId);
    migrateFeedback.value = { teamId, type: res.migrated ? "success" : "error", text: res.message };
    await load();
  } catch (e) {
    migrateFeedback.value = { teamId, type: "error", text: describeAdminError(e) };
  } finally {
    migrating.value = null;
  }
}

async function migrateAllOnPage() {
  if (teams.value.length === 0) return;
  if (!confirm(`Migrate all ${teams.value.length} workspace(s) on this page to V2?`)) return;

  migratingAll.value = true;
  migrateAllFeedback.value = null;
  const teamIds = teams.value.map((t) => t.teamId);
  let succeeded = 0;
  const failures: string[] = [];

  for (const teamId of teamIds) {
    migrating.value = teamId;
    try {
      const res = await migrateTeamToV2(teamId);
      if (res.migrated) {
        succeeded++;
      } else {
        failures.push(`${teamId}: ${res.message}`);
      }
    } catch (e) {
      failures.push(`${teamId}: ${describeAdminError(e)}`);
    }
  }

  migrating.value = null;
  migrateAllFeedback.value =
    failures.length === 0
      ? { type: "success", text: `Migrated ${succeeded} workspace(s).` }
      : { type: "error", text: `Migrated ${succeeded} workspace(s), ${failures.length} failed: ${failures.join("; ")}` };

  migratingAll.value = false;
  await load();
}
</script>

<template>
  <div>
    <h1>Legacy Slack workspaces</h1>
    <p class="lead">{{ totalCount }} workspace(s) still on the V1 data model.</p>

    <div class="card">
      <div class="field">
        <label for="team-search">Search by team name or id</label>
        <input id="team-search" v-model="query" type="text" placeholder="e.g. Blank" />
      </div>

      <p v-if="error" class="alert alert-error">{{ error }}</p>
      <p v-if="migrateAllFeedback" :class="['alert', migrateAllFeedback.type === 'success' ? 'alert-success' : 'alert-error']">
        {{ migrateAllFeedback.text }}
      </p>
      <div v-if="loading" class="spinner"></div>

      <template v-else>
        <div v-if="teams.length > 0" class="page-actions">
          <button class="btn" :disabled="migratingAll" @click="migrateAllOnPage">
            {{ migratingAll ? `Migrating (${migrating})...` : `Migrate all on this page (${teams.length})` }}
          </button>
        </div>

        <div class="team-list">
        <div v-for="t in teams" :key="t.teamId" class="team">
          <div class="team-header">
            <h3>{{ t.teamName }} <span class="team-id">({{ t.teamId }})</span></h3>
            <div class="team-actions">
              <span v-if="t.pendingRemoval" class="status bad">Pending removal</span>
              <router-link :to="`/admin/teams/${t.teamId}`" class="btn small">Details</router-link>
              <button class="btn small" :disabled="migrating === t.teamId" @click="migrate(t.teamId)">
                {{ migrating === t.teamId ? "Migrating..." : "Migrate to V2" }}
              </button>
            </div>
          </div>
          <p
            v-if="migrateFeedback && migrateFeedback.teamId === t.teamId"
            :class="['alert', migrateFeedback.type === 'success' ? 'alert-success' : 'alert-error']"
          >
            {{ migrateFeedback.text }}
          </p>
          <table v-if="t.subscriptions.length > 0" class="admin-table">
            <thead>
              <tr>
                <th>Channel</th>
                <th>League</th>
                <th>Subscriptions</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="s in t.subscriptions" :key="s.channelId">
                <td>{{ s.channelId }}</td>
                <td>{{ s.leagueId || "—" }}</td>
                <td>{{ s.subscriptions.join(", ") || "—" }}</td>
              </tr>
            </tbody>
          </table>
          <p v-else class="no-subs">No channel subscriptions.</p>
        </div>
          <p v-if="teams.length === 0">No legacy workspaces found — everything's migrated.</p>
        </div>
      </template>

      <div class="pager">
        <button class="btn small" :disabled="page <= 1" @click="page--">&larr; Prev</button>
        <span>Page {{ page }} of {{ totalPages() }} ({{ totalCount }} total)</span>
        <button class="btn small" :disabled="page >= totalPages()" @click="page++">Next &rarr;</button>
      </div>
    </div>
  </div>
</template>

<style scoped>
.lead {
  color: #6b7280;
  margin-bottom: 1rem;
}

.status.bad {
  color: #dc2626;
  font-size: 0.85rem;
}

.page-actions {
  margin-bottom: 1rem;
}

.team-list {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

.team-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  margin-bottom: 0.5rem;
}

.team-actions {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  flex-wrap: wrap;
}

.team h3 {
  margin-bottom: 0;
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
</style>
