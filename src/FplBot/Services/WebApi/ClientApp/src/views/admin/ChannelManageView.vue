<script setup lang="ts">
import { ref, computed, onMounted } from "vue";
import { useRouter } from "vue-router";
import { ALL_EVENT_SUBSCRIPTIONS } from "../../api/api";
import type { InstallationAdapter, EntityDetails, EntityChannel } from "../../composables/installationAdapters";
import type { EventSubscription } from "../../api/types";
import { describeAdminError } from "../../composables/useAdminAuth";
import { describeFailureReason } from "../../api/deliveryFailures";
import { formatDateTime } from "../../formatting";

const props = defineProps<{ entityId: string; channelId: string; adapter: InstallationAdapter }>();
const router = useRouter();

const details = ref<EntityDetails | null>(null);
const channel = ref<EntityChannel | null>(null);
const loading = ref(true);
const loadError = ref("");

const selectedSubscriptions = ref<Set<EventSubscription>>(new Set());
const savingSubscriptions = ref(false);
const subscriptionsFeedback = ref<{ type: "success" | "error"; text: string } | null>(null);

const newChannelId = ref("");
const movingChannel = ref(false);
const moveFeedback = ref<{ type: "success" | "error"; text: string } | null>(null);

const deleting = ref(false);

const publishing = ref(false);
const publishFeedback = ref<{ type: "success" | "error"; text: string } | null>(null);

const purgeStatus = computed(() => {
  const c = channel.value;
  if (!c || !c.purgeEligibleAt || c.failureCount === 0) {
    return null;
  }
  const at = new Date(c.purgeEligibleAt);
  const attempts = c.failuresUntilPurge;
  const condition = attempts > 0 ? ` if seeing ${attempts} more ${attempts === 1 ? "failure" : "failures"}` : "";
  if (at.getTime() > Date.now()) {
    return `Purging ${formatDateTime(c.purgeEligibleAt)}${condition}`;
  }
  return condition ? `Purging${condition}` : "Purging on the next failure";
});

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
    const found = data.channels.find((c) => c.channel === props.channelId);
    if (!found) {
      router.replace({ name: props.adapter.detailsRouteName, params: { entityId: props.entityId } });
      return;
    }
    channel.value = found;
    selectedSubscriptions.value = new Set(found.subscriptions);
    newChannelId.value = found.channel;
  } catch (e) {
    loadError.value = describeAdminError(e);
  } finally {
    loading.value = false;
  }
}

onMounted(load);

const allSubscriptions = computed(() => ALL_EVENT_SUBSCRIPTIONS);

function toggleSubscription(sub: EventSubscription) {
  if (selectedSubscriptions.value.has(sub)) {
    selectedSubscriptions.value.delete(sub);
  } else if (sub === "All") {
    // "All" is a special catch-all value in the domain — mixing it with specific events in the
    // same save causes the specific ones to be silently discarded, so keep the checkbox exclusive.
    selectedSubscriptions.value = new Set(["All"]);
    return;
  } else {
    selectedSubscriptions.value.delete("All");
    selectedSubscriptions.value.add(sub);
  }
  // Force reactivity — Set mutations don't trigger Vue's ref tracking on their own.
  selectedSubscriptions.value = new Set(selectedSubscriptions.value);
}

function checkAll() {
  selectedSubscriptions.value = new Set(ALL_EVENT_SUBSCRIPTIONS.filter((s) => s !== "All"));
}

async function saveSubscriptions() {
  savingSubscriptions.value = true;
  subscriptionsFeedback.value = null;
  try {
    const res = await props.adapter.updateChannelSubscriptions(props.entityId, props.channelId, [...selectedSubscriptions.value]);
    subscriptionsFeedback.value = { type: "success", text: res.message };
    await load();
  } catch (e) {
    subscriptionsFeedback.value = { type: "error", text: describeAdminError(e) };
  } finally {
    savingSubscriptions.value = false;
  }
}

async function submitMoveChannel() {
  if (!newChannelId.value || newChannelId.value === props.channelId) return;
  if (!confirm(`Move this subscription from ${props.channelId} to ${newChannelId.value}?`)) return;
  movingChannel.value = true;
  moveFeedback.value = null;
  try {
    const res = await props.adapter.moveChannel(props.entityId, props.channelId, newChannelId.value);
    moveFeedback.value = { type: "success", text: res.message };
    router.replace({ name: props.adapter.manageRouteName, params: { entityId: props.entityId, channelId: newChannelId.value } });
  } catch (e) {
    moveFeedback.value = { type: "error", text: describeAdminError(e) };
  } finally {
    movingChannel.value = false;
  }
}

async function submitPublish() {
  publishing.value = true;
  publishFeedback.value = null;
  try {
    const res = await props.adapter.publishStandings(props.entityId, props.channelId);
    publishFeedback.value = { type: res.published ? "success" : "error", text: res.message };
  } catch (e) {
    publishFeedback.value = { type: "error", text: describeAdminError(e) };
  } finally {
    publishing.value = false;
  }
}

async function submitDelete() {
  if (!confirm(`Delete the subscription for channel ${props.channelId}? This cannot be undone.`)) return;
  deleting.value = true;
  try {
    await props.adapter.deleteChannelSubscription(props.entityId, props.channelId);
    router.push({ name: props.adapter.detailsRouteName, params: { entityId: props.entityId } });
  } catch (e) {
    loadError.value = describeAdminError(e);
  } finally {
    deleting.value = false;
  }
}
</script>

<template>
  <div>
    <router-link :to="{ name: adapter.detailsRouteName, params: { entityId } }" class="back-link">
      &larr; Back to {{ details?.name || "details" }}
    </router-link>

    <div v-if="loading" class="spinner"></div>
    <p v-else-if="loadError" class="alert alert-error">{{ loadError }}</p>

    <template v-else-if="channel">
      <h1>Manage channel</h1>
      <p class="channel-id">{{ channel.channel }}</p>

      <div class="card">
        <h2>Status</h2>
        <dl class="summary">
          <dt>Channel visible via {{ adapter.apiLabel }}</dt>
          <dd>
            <span v-if="channel.channelStatus === true" class="status ok">&#10003; found</span>
            <span v-else-if="channel.channelStatus === false" class="status bad">&#10007; not found via {{ adapter.apiLabel }}</span>
            <span v-else class="status">unknown</span>
          </dd>
          <dt>Delivery</dt>
          <dd>
            <span v-if="channel.failureCount > 0" class="status bad">
              &#9888; {{ channel.failureCount }} consecutive failed {{ channel.failureCount === 1 ? "delivery" : "deliveries" }} (since {{ formatDateTime(channel.failingSince) }})<template v-if="channel.lastFailureReason">&nbsp;&mdash; {{ describeFailureReason(channel.lastFailureReason) }}</template>
            </span>
            <span v-else class="status">not currently failing</span>
          </dd>
          <template v-if="purgeStatus">
            <dt>Automatic purge</dt>
            <dd>
              <span class="status">{{ purgeStatus }}</span>
            </dd>
          </template>
        </dl>
      </div>

      <div class="card">
        <h2>Subscribed events</h2>
        <p v-if="subscriptionsFeedback" :class="['alert', subscriptionsFeedback.type === 'success' ? 'alert-success' : 'alert-error']">
          {{ subscriptionsFeedback.text }}
        </p>
        <div class="subscription-grid">
          <label v-for="sub in allSubscriptions" :key="sub" class="subscription-option">
            <input
              type="checkbox"
              :checked="selectedSubscriptions.has(sub)"
              @change="toggleSubscription(sub)"
            />
            {{ sub }}
          </label>
        </div>
        <div class="subscription-actions">
          <button class="btn small" :disabled="savingSubscriptions" @click="checkAll">Check all</button>
          <button class="btn small" :disabled="savingSubscriptions" @click="saveSubscriptions">
            {{ savingSubscriptions ? "Saving..." : "Save subscriptions" }}
          </button>
        </div>
      </div>

      <div class="card">
        <h2>Change channel</h2>
        <p v-if="moveFeedback" :class="['alert', moveFeedback.type === 'success' ? 'alert-success' : 'alert-error']">
          {{ moveFeedback.text }}
        </p>
        <div class="field">
          <label for="new-channel-id">Channel</label>
          <input id="new-channel-id" v-model="newChannelId" type="text" />
        </div>
        <button class="btn small" :disabled="movingChannel || newChannelId === channel.channel" @click="submitMoveChannel">
          {{ movingChannel ? "Moving..." : "Move subscription" }}
        </button>
      </div>

      <div class="card">
        <h2>Publish standings</h2>
        <p v-if="publishFeedback" :class="['alert', publishFeedback.type === 'success' ? 'alert-success' : 'alert-error']">
          {{ publishFeedback.text }}
        </p>
        <p v-if="!channel.leagueId" class="hint">Not following a league, so there are no standings to publish.</p>
        <button class="btn small" :disabled="publishing || !channel.leagueId" @click="submitPublish">
          {{ publishing ? "Publishing..." : "Publish standings" }}
        </button>
      </div>

      <div class="card danger-zone">
        <h2>Danger zone</h2>
        <button class="btn danger" :disabled="deleting" @click="submitDelete">
          {{ deleting ? "Deleting..." : "Delete channel subscription" }}
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

.channel-id {
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

.summary {
  display: grid;
  grid-template-columns: 14rem 1fr;
  row-gap: 0.5rem;
}

.summary dt {
  font-weight: bold;
}

.summary dd {
  margin: 0;
  word-break: break-all;
}

.hint {
  color: #6b7280;
  font-style: italic;
}

.status {
  font-size: 0.85rem;
}

.status.ok {
  color: #16a34a;
}

.status.bad {
  color: #dc2626;
}

.subscription-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(12rem, 1fr));
  gap: 0.5rem;
  margin-bottom: 1rem;
}

.subscription-option {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-weight: normal;
  cursor: pointer;
}

.subscription-actions {
  display: flex;
  gap: 0.5rem;
}

.danger-zone {
  border-color: #fecaca;
}
</style>
