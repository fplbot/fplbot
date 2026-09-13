<script setup lang="ts">
import { ref, onMounted } from "vue";
import { useRouter } from "vue-router";
import { getTeam, uninstallTeam, publishStandings, migrateTeamToV2 } from "../../api/api";
import type { TeamDetails } from "../../api/types";
import { describeAdminError } from "../../composables/useAdminAuth";

const props = defineProps<{ teamId: string }>();
const router = useRouter();

const team = ref<TeamDetails | null>(null);
const loading = ref(true);
const loadError = ref("");

const publishing = ref<string | null>(null);
const publishFeedback = ref<{ channel: string; type: "success" | "error"; text: string } | null>(null);

const uninstalling = ref(false);
const uninstallFeedback = ref<{ type: "success" | "error"; text: string } | null>(null);

const migrating = ref(false);
const migrateFeedback = ref<{ type: "success" | "error"; text: string } | null>(null);

async function load() {
  loading.value = true;
  loadError.value = "";
  try {
    const data = await getTeam(props.teamId);
    if (data == null) {
      router.replace("/admin/slack");
      return;
    }
    team.value = data;
  } catch (e) {
    loadError.value = describeAdminError(e);
  } finally {
    loading.value = false;
  }
}

onMounted(load);

async function submitPublish(channel: string) {
  publishing.value = channel;
  publishFeedback.value = null;
  try {
    const res = await publishStandings(props.teamId, channel);
    publishFeedback.value = { channel, type: res.published ? "success" : "error", text: res.message };
  } catch (e) {
    publishFeedback.value = { channel, type: "error", text: describeAdminError(e) };
  } finally {
    publishing.value = null;
  }
}

async function submitMigrate() {
  migrating.value = true;
  migrateFeedback.value = null;
  try {
    const res = await migrateTeamToV2(props.teamId);
    migrateFeedback.value = { type: res.migrated ? "success" : "error", text: res.message };
    await load();
  } catch (e) {
    migrateFeedback.value = { type: "error", text: describeAdminError(e) };
  } finally {
    migrating.value = false;
  }
}

async function submitUninstall() {
  if (!confirm(`Uninstall fplbot from ${team.value?.teamName}? This cannot be undone.`)) return;
  uninstalling.value = true;
  uninstallFeedback.value = null;
  try {
    const res = await uninstallTeam(props.teamId);
    uninstallFeedback.value = { type: "success", text: res.message };
    setTimeout(() => router.push("/admin/slack"), 1500);
  } catch (e) {
    uninstallFeedback.value = { type: "error", text: describeAdminError(e) };
  } finally {
    uninstalling.value = false;
  }
}
</script>

<template>
  <div>
    <router-link to="/admin/slack" class="back-link">&larr; Back to workspaces</router-link>

    <div v-if="loading" class="spinner"></div>
    <p v-else-if="loadError" class="alert alert-error">{{ loadError }}</p>

    <template v-else-if="team">
      <h1>{{ team.teamName }}</h1>
      <p class="team-id">{{ team.teamId }}</p>

      <div class="card">
        <h2>Overview</h2>
        <dl class="summary">
          <dt>Token</dt>
          <dd>{{ team.token || "not set" }}</dd>
          <dt>Pending removal</dt>
          <dd>{{ team.pendingRemoval ? "Yes" : "No" }}</dd>
        </dl>
      </div>

      <div class="card legacy">
        <h2>Legacy data:</h2>
        <template v-if="team.legacy">
          <p class="lead">This team still has data in the old, single-channel storage model.</p>
          <p v-if="migrateFeedback" :class="['alert', migrateFeedback.type === 'success' ? 'alert-success' : 'alert-error']">
            {{ migrateFeedback.text }}
          </p>
          <dl class="summary">
            <dt>Scope</dt>
            <dd>{{ team.legacy.scope || "not set" }}</dd>
            <dt>Access token</dt>
            <dd>{{ team.legacy.accessToken || "not set" }}</dd>
            <dt>Channel</dt>
            <dd>{{ team.legacy.channel || "not set" }}</dd>
            <dt>League</dt>
            <dd>{{ team.legacy.leagueId || "not set" }}</dd>
            <dt>Subscriptions</dt>
            <dd>{{ team.legacy.subscriptions.join(", ") || "none" }}</dd>
            <dt>Pending removal</dt>
            <dd>{{ team.legacy.pendingRemoval ? "Yes" : "No" }}</dd>
          </dl>
          <button class="btn small" :disabled="migrating" @click="submitMigrate">
            {{ migrating ? "Migrating..." : "Migrate to V2" }}
          </button>
        </template>
        <p v-else class="no-subs">No legacy data attached.</p>
      </div>

      <div class="card">
        <h2>Channels</h2>
        <p v-if="team.pendingRemoval" class="status bad">Pending removal</p>
        <p v-if="publishFeedback" :class="['alert', publishFeedback.type === 'success' ? 'alert-success' : 'alert-error']">
          {{ publishFeedback.text }}
        </p>
        <table v-if="team.channels.length > 0" class="admin-table">
          <thead>
            <tr>
              <th>Channel</th>
              <th>League</th>
              <th>Subscriptions</th>
              <th>Source</th>
              <th>Status</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="c in team.channels" :key="c.channel">
              <td>{{ c.channel }}</td>
              <td>{{ c.leagueName || "Unknown" }} ({{ c.leagueId || "not set" }})</td>
              <td>{{ c.subscriptions.join(", ") || "none" }}</td>
              <td><span :class="['source', c.source]">{{ c.source.toUpperCase() }}</span></td>
              <td>
                <span v-if="c.channelStatus === true" class="status ok">&#10003; found</span>
                <span v-else-if="c.channelStatus === false" class="status bad">&#10007; not found via Slack API</span>
              </td>
              <td>
                <button
                  v-if="c.leagueId"
                  class="btn small"
                  :disabled="publishing === c.channel"
                  @click="submitPublish(c.channel)"
                >
                  {{ publishing === c.channel ? "Publishing..." : "Publish standings" }}
                </button>
              </td>
            </tr>
          </tbody>
        </table>
        <p v-else class="no-subs">No channel subscriptions.</p>
      </div>

      <div class="card danger-zone">
        <h2>Danger zone</h2>
        <p v-if="uninstallFeedback" :class="['alert', uninstallFeedback.type === 'success' ? 'alert-success' : 'alert-error']">
          {{ uninstallFeedback.text }}
        </p>
        <button class="btn danger" :disabled="uninstalling || team.pendingRemoval" @click="submitUninstall">
          {{ team.pendingRemoval ? "Removal pending..." : uninstalling ? "Uninstalling..." : "Uninstall from Slack" }}
        </button>
      </div>
    </template>
  </div>
</template>

<style scoped>
.back-link {
  display: inline-block;
  margin-bottom: 1rem;
  font-size: 0.9rem;
  text-decoration: none;
  color: var(--fpl-purple);
}

.team-id {
  color: #6b7280;
  margin-bottom: 1.5rem;
}

.card {
  margin-bottom: 1.5rem;
}

.card h2 {
  font-size: 1.1rem;
  margin-bottom: 1rem;
}

.no-subs {
  color: #6b7280;
  font-style: italic;
  font-size: 0.9rem;
}

.lead {
  color: #6b7280;
  margin-bottom: 1rem;
  font-size: 0.9rem;
}

.summary {
  display: grid;
  grid-template-columns: 10rem 1fr;
  row-gap: 0.5rem;
}

.summary dt {
  font-weight: bold;
}

.summary dd {
  margin: 0;
  word-break: break-all;
}

.legacy {
  border-color: #fde68a;
}

.source {
  display: inline-block;
  padding: 0.1rem 0.5rem;
  border-radius: 0.25rem;
  font-size: 0.75rem;
  font-weight: bold;
}

.source.v2 {
  background: #dcfce7;
  color: #16a34a;
}

.source.v1 {
  background: #fef3c7;
  color: #b45309;
}

.status {
  margin-left: 0.5rem;
  font-size: 0.85rem;
}

p.status {
  margin-left: 0;
}

.status.ok {
  color: #16a34a;
}

.status.bad {
  color: #dc2626;
}

.danger-zone {
  border-color: #fecaca;
}
</style>
