<script setup lang="ts">
import { ref, computed, onMounted, watch } from "vue";
import { useRouter } from "vue-router";
import { ALL_EVENT_SUBSCRIPTIONS, getLeague } from "../../api/api";
import type { InstallationAdapter, EntityDetails, EntityChannel } from "../../composables/installationAdapters";
import type { AvailableChannel, EventSubscription } from "../../api/types";
import ChannelPicker from "../../components/ChannelPicker.vue";
import type { ChannelPickerOption } from "../../components/ChannelPicker.vue";
import { describeAdminError } from "../../composables/useAdminAuth";
import { describeFailureReason } from "../../api/deliveryFailures";
import { formatDateTime, formatChannelName } from "../../formatting";

const props = defineProps<{ entityId: string; subscriptionId: string; adapter: InstallationAdapter }>();
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
const availableChannels = ref<AvailableChannel[]>([]);
const channelListError = ref("");
const loadingChannelList = ref(true);

const subscribedChannelIds = computed(
  () => new Set((details.value?.channels ?? []).map((c) => c.channel))
);

const moveOptions = computed<ChannelPickerOption[]>(() => {
  const options = availableChannels.value.map((c) => {
    const isCurrent = c.id === channel.value?.channel;
    const taken = !isCurrent && subscribedChannelIds.value.has(c.id);
    return {
      value: c.id,
      label: `${formatChannelName(c.name)} (${c.id})`,
      group: isCurrent ? "Current" : taken ? "Already subscribed" : "Move to",
      disabled: isCurrent || taken,
    };
  });
  const currentChannelId = channel.value?.channel;
  if (currentChannelId && !options.some((o) => o.value === currentChannelId)) {
    const name = channel.value?.channelName;
    options.unshift({
      value: currentChannelId,
      label: `${name ? `${formatChannelName(name)} ` : ""}${currentChannelId}`,
      group: "Current",
      disabled: true,
    });
  }
  const rank = (o: ChannelPickerOption) => (o.group === "Current" ? 0 : o.group === "Already subscribed" ? 1 : 2);
  return options.sort((a, b) => rank(a) - rank(b));
});

const leagueIdInput = ref<number | null>(null);
const savingLeague = ref(false);
const leagueFeedback = ref<{ type: "success" | "error"; text: string } | null>(null);
const leaguePreview = ref<{ state: "looking" | "found" | "missing" | "failed"; name?: string; admin?: string } | null>(null);
let leagueLookupTimer: ReturnType<typeof setTimeout> | undefined;
let leagueLookupToken = 0;

watch(leagueIdInput, (id) => {
  clearTimeout(leagueLookupTimer);
  leagueLookupToken++;
  if (id == null || id <= 0 || id === channel.value?.leagueId) {
    leaguePreview.value = null;
    return;
  }
  leaguePreview.value = { state: "looking" };
  const token = leagueLookupToken;
  leagueLookupTimer = setTimeout(async () => {
    try {
      const league = await getLeague(id);
      if (token !== leagueLookupToken) return;
      leaguePreview.value = league
        ? { state: "found", name: league.leagueName, admin: league.leagueAdmin }
        : { state: "missing" };
    } catch {
      if (token !== leagueLookupToken) return;
      leaguePreview.value = { state: "failed" };
    }
  }, 400);
});

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
    const found = data.channels.find((c) => c.id === props.subscriptionId);
    if (!found) {
      router.replace({ name: props.adapter.detailsRouteName, params: { entityId: props.entityId } });
      return;
    }
    channel.value = found;
    newChannelId.value = found.channel;
    selectedSubscriptions.value = new Set(found.subscriptions);
    leagueIdInput.value = found.leagueId;
  } catch (e) {
    loadError.value = describeAdminError(e);
  } finally {
    loading.value = false;
  }
}

async function loadAvailableChannels() {
  loadingChannelList.value = true;
  channelListError.value = "";
  try {
    availableChannels.value = await props.adapter.getAvailableChannels(props.entityId);
  } catch (e) {
    availableChannels.value = [];
    channelListError.value = describeAdminError(e);
  } finally {
    loadingChannelList.value = false;
  }
}

onMounted(load);
onMounted(loadAvailableChannels);

watch(
  () => props.subscriptionId,
  async () => {
    subscriptionsFeedback.value = null;
    publishFeedback.value = null;
    leagueFeedback.value = null;
    await Promise.all([load(), loadAvailableChannels()]);
  }
);

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
    const res = await props.adapter.updateChannelSubscriptions(props.entityId, props.subscriptionId, [...selectedSubscriptions.value]);
    subscriptionsFeedback.value = { type: "success", text: res.message };
    await load();
  } catch (e) {
    subscriptionsFeedback.value = { type: "error", text: describeAdminError(e) };
  } finally {
    savingSubscriptions.value = false;
  }
}

async function submitLeague() {
  if (leagueIdInput.value == null) return;
  savingLeague.value = true;
  leagueFeedback.value = null;
  try {
    const res = await props.adapter.followLeague(props.entityId, props.subscriptionId, leagueIdInput.value);
    leagueFeedback.value = { type: "success", text: res.message };
    await load();
  } catch (e) {
    leagueFeedback.value = { type: "error", text: describeAdminError(e) };
  } finally {
    savingLeague.value = false;
  }
}

async function submitUnfollowLeague() {
  if (!confirm("Stop following a league in this channel? League-specific notifications will stop.")) return;
  savingLeague.value = true;
  leagueFeedback.value = null;
  try {
    const res = await props.adapter.unfollowLeague(props.entityId, props.subscriptionId);
    leagueFeedback.value = { type: "success", text: res.message };
    await load();
  } catch (e) {
    leagueFeedback.value = { type: "error", text: describeAdminError(e) };
  } finally {
    savingLeague.value = false;
  }
}

async function submitMoveChannel() {
  if (!newChannelId.value || newChannelId.value === channel.value?.channel) return;
  const target = availableChannels.value.find((c) => c.id === newChannelId.value);
  const describedTarget = target ? `${formatChannelName(target.name)} (${target.id})` : newChannelId.value;

  if (!confirm(`Move this subscription from ${channel.value?.channel} to ${describedTarget}?`)) return;
  movingChannel.value = true;
  moveFeedback.value = null;
  try {
    const res = await props.adapter.moveChannel(props.entityId, props.subscriptionId, newChannelId.value);
    moveFeedback.value = { type: "success", text: res.message };
    await load();
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
    const res = await props.adapter.publishStandings(props.entityId, props.subscriptionId);
    publishFeedback.value = { type: res.published ? "success" : "error", text: res.message };
  } catch (e) {
    publishFeedback.value = { type: "error", text: describeAdminError(e) };
  } finally {
    publishing.value = false;
  }
}

async function submitDelete() {
  if (!confirm(`Delete the subscription for channel ${channel.value?.channel}? This cannot be undone.`)) return;
  deleting.value = true;
  try {
    await props.adapter.deleteChannelSubscription(props.entityId, props.subscriptionId);
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
      <p class="channel-name">Channel: <span v-if="channel.channelName">{{ formatChannelName(channel.channelName) }}</span><span v-else class="unavailable">name unavailable</span></p>
      <p class="channel-id">
        {{ channel.channel }}
        <span class="lookup-note">(name looked up live via {{ adapter.apiLabel }}, not stored)</span>
      </p>

      <div class="card">
        <h2>Status</h2>
        <dl class="summary">
          <dt>Channel visible via {{ adapter.apiLabel }}</dt>
          <dd>
            <span v-if="channel.channelStatus === true" class="status ok">&#10003; found</span>
            <span v-else-if="channel.channelStatus === false" class="status bad" :title="adapter.channelNotVisibleHint">
              &#10007; not listed by {{ adapter.apiLabel }}<template
                v-if="channel.failureCount === 0 && adapter.notListedButDeliveringHint"
              > &mdash; {{ adapter.notListedButDeliveringHint }}</template>
            </span>
            <span v-else class="status">? unknown &mdash; couldn't reach {{ adapter.apiLabel }}</span>
          </dd>
          <dt>Delivery</dt>
          <dd>
            <span v-if="channel.failureCount > 0" class="status bad">
              &#9888; {{ channel.failureCount }} consecutive failed {{ channel.failureCount === 1 ? "delivery" : "deliveries" }} (since {{ formatDateTime(channel.failingSince) }})<template v-if="channel.lastFailureReason">&nbsp;&mdash; {{ describeFailureReason(channel.lastFailureReason) }}</template>
            </span>
            <span v-else class="status">not currently failing</span>
          </dd>
          <dt>Failure count</dt>
          <dd>
            <span :class="['status', { bad: channel.failureCount >= channel.purgeFailureLimit }]">
              {{ channel.failureCount }} out of {{ channel.purgeFailureLimit }}
            </span>
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
        <h2>Followed league</h2>
        <p v-if="leagueFeedback" :class="['alert', leagueFeedback.type === 'success' ? 'alert-success' : 'alert-error']">
          {{ leagueFeedback.text }}
        </p>
        <dl class="summary">
          <dt>Currently</dt>
          <dd>
            <template v-if="channel.leagueId">{{ channel.leagueName || "Unknown league" }} ({{ channel.leagueId }})</template>
            <span v-else class="status">no league followed</span>
          </dd>
        </dl>
        <div class="field">
          <label for="league-id">League id</label>
          <input id="league-id" v-model.number="leagueIdInput" type="number" min="1" placeholder="e.g. 579157" />
          <p v-if="leaguePreview" class="hint">
            <template v-if="leaguePreview.state === 'looking'">Looking up&hellip;</template>
            <template v-else-if="leaguePreview.state === 'found'">
              About to follow <strong>{{ leaguePreview.name || "unnamed league" }}</strong
              ><template v-if="leaguePreview.admin"> &mdash; admin {{ leaguePreview.admin }}</template>
            </template>
            <span v-else-if="leaguePreview.state === 'missing'" class="status bad">No classic league with that id</span>
            <span v-else class="status">Couldn't reach the FPL API to check this id</span>
          </p>
        </div>
        <button
          class="btn small"
          :disabled="savingLeague || leagueIdInput == null || leagueIdInput === channel.leagueId || leaguePreview?.state === 'missing'"
          @click="submitLeague"
        >
          {{ savingLeague ? "Saving..." : "Save league" }}
        </button>
        <button
          v-if="channel.leagueId"
          class="btn small danger"
          :disabled="savingLeague"
          @click="submitUnfollowLeague"
        >
          Stop following
        </button>
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
        <div v-if="loadingChannelList" class="spinner"></div>
        <template v-else>
          <ChannelPicker
            v-if="moveOptions.length > 1"
            id="new-channel-id"
            v-model="newChannelId"
            label="Move to"
            :options="moveOptions"
          />
          <p v-else class="hint">
            <template v-if="channelListError">{{ channelListError }}</template>
            <template v-else-if="availableChannels.length > 0">No other channel to move to in this {{ adapter.entityNoun }}.</template>
            <template v-else>No channels came back from {{ adapter.apiLabel }}.</template>
          </p>
        </template>
        <button
          v-if="moveOptions.length > 1"
          class="btn small"
          :disabled="movingChannel || !newChannelId || newChannelId === channel.channel"
          @click="submitMoveChannel"
        >
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

.channel-name {
  font-size: 1.6rem;
  font-weight: 600;
  margin-bottom: 0.25rem;
}

.channel-name .unavailable {
  color: #6b7280;
  font-weight: normal;
}

.channel-id {
  color: #6b7280;
  margin-bottom: 1.5rem;
}

.lookup-note {
  font-size: 0.8rem;
}

.card {
  margin-bottom: 1.5rem;
}

.card h2 {
  font-size: 1.35rem;
  font-weight: 600;
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
