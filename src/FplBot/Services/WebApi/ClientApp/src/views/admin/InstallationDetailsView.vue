<script setup lang="ts">
import { ref, onMounted } from "vue";
import { useRouter } from "vue-router";
import type { InstallationAdapter, EntityDetails } from "../../composables/installationAdapters";
import { describeAdminError } from "../../composables/useAdminAuth";
import { describeFailureReason } from "../../api/deliveryFailures";

const props = defineProps<{ entityId: string; adapter: InstallationAdapter }>();
const router = useRouter();

const details = ref<EntityDetails | null>(null);
const loading = ref(true);
const loadError = ref("");

const dangerBusy = ref(false);
const dangerFeedback = ref<{ type: "success" | "error"; text: string } | null>(null);

const deleting = ref<string | null>(null);

function formatFailingSince(failingSince: string | null): string {
  return failingSince ? new Date(failingSince).toLocaleString() : "";
}

async function load() {
  loading.value = true;
  loadError.value = "";
  try {
    const data = await props.adapter.getDetails(props.entityId);
    if (data == null) {
      router.replace(props.adapter.listRoute);
      return;
    }
    details.value = data;
  } catch (e) {
    loadError.value = describeAdminError(e);
  } finally {
    loading.value = false;
  }
}

onMounted(load);

async function removeChannelSub(channelId: string) {
  if (!confirm(`Delete the subscription for channel ${channelId}?`)) return;
  deleting.value = channelId;
  try {
    await props.adapter.deleteChannelSubscription(props.entityId, channelId);
    await load();
  } catch (e) {
    loadError.value = describeAdminError(e);
  } finally {
    deleting.value = null;
  }
}

async function submitDanger() {
  const label = details.value?.name || props.entityId;
  const confirmMessage =
    props.adapter.danger === "uninstall"
      ? `Uninstall fplbot from ${label}? This cannot be undone.`
      : `Delete ${label}? This forgets all of fplbot's tracked data for this server — it does not remove the bot from Discord.`;
  if (!confirm(confirmMessage)) return;

  dangerBusy.value = true;
  dangerFeedback.value = null;
  try {
    const action = props.adapter.danger === "uninstall" ? props.adapter.uninstall! : props.adapter.deleteEntity!;
    const res = await action(props.entityId);
    dangerFeedback.value = { type: "success", text: res.message };
    setTimeout(() => router.push(props.adapter.listRoute), 1500);
  } catch (e) {
    dangerFeedback.value = { type: "error", text: describeAdminError(e) };
  } finally {
    dangerBusy.value = false;
  }
}
</script>

<template>
  <div>
    <router-link :to="adapter.listRoute" class="back-link">&larr; {{ adapter.backLinkLabel }}</router-link>

    <div v-if="loading" class="spinner"></div>
    <p v-else-if="loadError" class="alert alert-error">{{ loadError }}</p>

    <template v-else-if="details">
      <h1>{{ details.name }}</h1>
      <p class="entity-id">{{ details.id }}</p>

      <div v-if="adapter.showOverview" class="card">
        <h2>Overview</h2>
        <dl class="summary">
          <dt>Token</dt>
          <dd>{{ details.token || "not set" }}</dd>
          <dt>Pending removal</dt>
          <dd>{{ details.pendingRemoval ? "Yes" : "No" }}</dd>
        </dl>
      </div>

      <div class="card">
        <h2>Channels</h2>
        <p v-if="details.pendingRemoval" class="status bad">Pending removal</p>
        <table v-if="details.channels.length > 0" class="admin-table">
          <thead>
            <tr>
              <th>Channel</th>
              <th>League</th>
              <th>Subscriptions</th>
              <th>Status</th>
              <th>Delivery</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="c in details.channels" :key="c.channel" :class="{ failing: c.failureCount > 0 }">
              <td>{{ c.channel }}</td>
              <td>{{ c.leagueName || "Unknown" }} ({{ c.leagueId || "not set" }})</td>
              <td>{{ c.subscriptions.join(", ") || "none" }}</td>
              <td>
                <span v-if="c.channelStatus === true" class="status ok">&#10003; found</span>
                <span v-else-if="c.channelStatus === false" class="status bad">&#10007; not found via {{ adapter.apiLabel }}</span>
              </td>
              <td>
                <span v-if="c.failureCount > 0" class="status bad" :title="`Failing since ${formatFailingSince(c.failingSince)}`">
                  &#9888; {{ c.failureCount }} consecutive failed {{ c.failureCount === 1 ? "delivery" : "deliveries" }} (since {{ formatFailingSince(c.failingSince) }})<template v-if="c.lastFailureReason">&nbsp;&mdash; {{ describeFailureReason(c.lastFailureReason) }}</template>
                </span>
                <span v-else class="no-subs">no failures</span>
              </td>
              <td class="row-actions">
                <router-link
                  class="btn small icon-btn"
                  title="Manage channel"
                  aria-label="Manage channel"
                  :to="{ name: adapter.manageRouteName, params: { entityId: entityId, channelId: c.channel } }"
                >
                  ✏️
                </router-link>
                <button
                  class="btn small danger icon-btn"
                  title="Delete channel subscription"
                  aria-label="Delete channel subscription"
                  :disabled="deleting === c.channel"
                  @click="removeChannelSub(c.channel)"
                >
                  ❌
                </button>
              </td>
            </tr>
          </tbody>
        </table>
        <p v-else class="no-subs">No channel subscriptions.</p>
      </div>

      <div class="card danger-zone">
        <h2>Danger zone</h2>
        <p v-if="dangerFeedback" :class="['alert', dangerFeedback.type === 'success' ? 'alert-success' : 'alert-error']">
          {{ dangerFeedback.text }}
        </p>
        <button
          class="btn danger"
          :disabled="dangerBusy || (adapter.danger === 'uninstall' && details.pendingRemoval)"
          @click="submitDanger"
        >
          <template v-if="adapter.danger === 'uninstall'">
            {{ details.pendingRemoval ? "Removal pending..." : dangerBusy ? "Uninstalling..." : "Uninstall from Slack" }}
          </template>
          <template v-else>
            {{ dangerBusy ? "Deleting..." : "Delete guild" }}
          </template>
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

.entity-id {
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
</style>
