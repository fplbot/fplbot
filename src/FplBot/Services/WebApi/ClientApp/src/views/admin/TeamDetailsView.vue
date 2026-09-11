<script setup lang="ts">
import { ref, onMounted } from "vue";
import { useRouter } from "vue-router";
import {
  getTeam,
  updateTeam,
  uninstallTeam,
  publishTeamEvent,
  ALL_EVENT_SUBSCRIPTIONS,
  type TeamDetails,
  type EventSubscription,
} from "../../api/admin";
import { describeAdminError } from "../../composables/useAdminAuth";

const props = defineProps<{ teamId: string }>();
const router = useRouter();

const team = ref<TeamDetails | null>(null);
const loading = ref(true);
const notFound = ref(false);
const loadError = ref("");

const leagueId = ref(0);
const channel = ref("");
const editSubscriptions = ref<EventSubscription[]>([]);
const savingEdit = ref(false);
const editFeedback = ref<{ type: "success" | "error"; text: string; warnings?: string[] } | null>(null);

const publishSubscriptions = ref<EventSubscription[]>([]);
const publishing = ref(false);
const publishFeedback = ref<{ type: "success" | "error"; text: string } | null>(null);

const uninstalling = ref(false);
const uninstallFeedback = ref<{ type: "success" | "error"; text: string } | null>(null);

async function load() {
  loading.value = true;
  notFound.value = false;
  loadError.value = "";
  try {
    const data = await getTeam(props.teamId);
    if (data == null) {
      notFound.value = true;
      return;
    }
    team.value = data;
    leagueId.value = data.leagueId ?? 0;
    channel.value = data.channel ?? "";
    editSubscriptions.value = [...data.subscriptions];
  } catch (e) {
    loadError.value = describeAdminError(e);
  } finally {
    loading.value = false;
  }
}

onMounted(load);

async function submitEdit() {
  savingEdit.value = true;
  editFeedback.value = null;
  try {
    const res = await updateTeam(props.teamId, {
      leagueId: leagueId.value,
      channel: channel.value,
      subscriptions: editSubscriptions.value,
    });
    editFeedback.value = {
      type: res.warnings.length > 0 ? "error" : "success",
      text: res.warnings.length > 0 ? "Updated with warnings:" : "Updated!",
      warnings: res.warnings,
    };
    await load();
  } catch (e) {
    editFeedback.value = { type: "error", text: describeAdminError(e) };
  } finally {
    savingEdit.value = false;
  }
}

async function submitPublish() {
  publishing.value = true;
  publishFeedback.value = null;
  try {
    const res = await publishTeamEvent(props.teamId, publishSubscriptions.value);
    publishFeedback.value = { type: res.published ? "success" : "error", text: res.message };
  } catch (e) {
    publishFeedback.value = { type: "error", text: describeAdminError(e) };
  } finally {
    publishing.value = false;
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
    <p v-else-if="notFound" class="alert alert-error">Team not found.</p>
    <p v-else-if="loadError" class="alert alert-error">{{ loadError }}</p>

    <template v-else-if="team">
      <h1>{{ team.teamName }}</h1>
      <p class="team-id">{{ team.teamId }}</p>

      <div class="card">
        <h2>Overview</h2>
        <dl class="summary">
          <dt>League</dt>
          <dd>{{ team.leagueName || "Unknown" }} ({{ team.leagueId || "not set" }})</dd>
          <dt>Channel</dt>
          <dd>
            {{ team.channel || "not set" }}
            <span v-if="team.channelStatus === true" class="status ok">&#10003; found</span>
            <span v-else-if="team.channelStatus === false" class="status bad">&#10007; not found via Slack API</span>
          </dd>
          <dt>Subscriptions</dt>
          <dd>{{ team.subscriptions.join(", ") || "none" }}</dd>
        </dl>
      </div>

      <div class="card">
        <h2>Edit</h2>
        <p v-if="editFeedback" :class="['alert', editFeedback.type === 'success' ? 'alert-success' : 'alert-error']">
          {{ editFeedback.text }}
          <span v-if="editFeedback.warnings?.length">{{ editFeedback.warnings.join(" ") }}</span>
        </p>

        <form @submit.prevent="submitEdit">
          <div class="field">
            <label for="edit-league">League ID</label>
            <input id="edit-league" v-model.number="leagueId" type="number" required />
          </div>
          <div class="field">
            <label for="edit-channel">Channel</label>
            <input id="edit-channel" v-model="channel" type="text" required placeholder="#fplbot or channel id" />
          </div>
          <div class="field">
            <label>Subscriptions</label>
            <div class="checkbox-grid">
              <label v-for="s in ALL_EVENT_SUBSCRIPTIONS" :key="s">
                <input type="checkbox" :value="s" v-model="editSubscriptions" />
                {{ s }}
              </label>
            </div>
          </div>
          <button class="btn" type="submit" :disabled="savingEdit">{{ savingEdit ? "Saving..." : "Save changes" }}</button>
        </form>
      </div>

      <div class="card">
        <h2>Publish event</h2>
        <p class="lead">Only "Standings" is currently supported.</p>
        <p v-if="publishFeedback" :class="['alert', publishFeedback.type === 'success' ? 'alert-success' : 'alert-error']">
          {{ publishFeedback.text }}
        </p>
        <form @submit.prevent="submitPublish">
          <div class="checkbox-grid">
            <label v-for="s in ALL_EVENT_SUBSCRIPTIONS" :key="s">
              <input type="checkbox" :value="s" v-model="publishSubscriptions" />
              {{ s }}
            </label>
          </div>
          <button class="btn" type="submit" :disabled="publishing">{{ publishing ? "Publishing..." : "Publish now" }}</button>
        </form>
      </div>

      <div class="card danger-zone">
        <h2>Danger zone</h2>
        <p v-if="uninstallFeedback" :class="['alert', uninstallFeedback.type === 'success' ? 'alert-success' : 'alert-error']">
          {{ uninstallFeedback.text }}
        </p>
        <button class="btn danger" :disabled="uninstalling" @click="submitUninstall">
          {{ uninstalling ? "Uninstalling..." : "Uninstall from Slack" }}
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

.lead {
  color: #6b7280;
  margin-bottom: 1rem;
  font-size: 0.9rem;
}

.summary {
  display: grid;
  grid-template-columns: 8rem 1fr;
  row-gap: 0.5rem;
}

.summary dt {
  font-weight: bold;
}

.summary dd {
  margin: 0;
}

.status {
  margin-left: 0.5rem;
  font-size: 0.85rem;
}

.status.ok {
  color: #16a34a;
}

.status.bad {
  color: #dc2626;
}

.checkbox-grid {
  margin-bottom: 1rem;
}

.danger-zone {
  border-color: #fecaca;
}
</style>
