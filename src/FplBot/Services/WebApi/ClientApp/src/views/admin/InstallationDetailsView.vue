<script setup lang="ts">
import { ref, computed, watch, onMounted } from "vue";
import { useRouter } from "vue-router";
import type { InstallationAdapter, EntityDetails } from "../../composables/installationAdapters";
import type { AvailableChannel } from "../../api/types";
import { describeAdminError } from "../../composables/useAdminAuth";
import { describeFailureReason } from "../../api/deliveryFailures";
import { formatDateTime, formatChannelName } from "../../formatting";

const props = defineProps<{ entityId: string; adapter: InstallationAdapter }>();
const router = useRouter();

const details = ref<EntityDetails | null>(null);
const loading = ref(true);
const loadError = ref("");

const dangerBusy = ref(false);
const dangerFeedback = ref<{ type: "success" | "error"; text: string } | null>(null);

const deleting = ref<string | null>(null);

const availableChannels = ref<AvailableChannel[]>([]);
const loadingChannelList = ref(true);
const channelListError = ref("");
const newChannelId = ref("");
const adding = ref(false);
const addFeedback = ref<{ type: "success" | "error"; text: string } | null>(null);

const channelFilter = ref("");

const allAddOptions = computed(() => {
  const taken = new Set((details.value?.channels ?? []).map((c) => c.channel));
  return availableChannels.value.map((c) => ({
    value: c.id,
    label: `${formatChannelName(c.name)} (${c.id})`,
    disabled: taken.has(c.id),
  }));
});

const addOptions = computed(() => {
  const needle = channelFilter.value.trim().toLowerCase().replace(/^#/, "");
  if (!needle) return allAddOptions.value;
  return allAddOptions.value.filter((o) => o.label.toLowerCase().includes(needle));
});

const subscribedOptions = computed(() => addOptions.value.filter((o) => o.disabled));
const unsubscribedOptions = computed(() => addOptions.value.filter((o) => !o.disabled));

const pickerOpen = ref(false);
const highlightedValue = ref("");
const selectedLabel = ref("");

function openPicker() {
  if (newChannelId.value) channelFilter.value = "";
  pickerOpen.value = true;
  highlightedValue.value = unsubscribedOptions.value[0]?.value ?? "";
}

function closePicker() {
  pickerOpen.value = false;
  if (newChannelId.value) channelFilter.value = selectedLabel.value;
}

function choose(value: string, label: string) {
  newChannelId.value = value;
  selectedLabel.value = label;
  channelFilter.value = label;
  pickerOpen.value = false;
}

function moveHighlight(delta: number) {
  pickerOpen.value = true;
  const options = unsubscribedOptions.value;
  if (options.length === 0) return;
  const current = options.findIndex((o) => o.value === highlightedValue.value);
  const next = Math.min(Math.max(current + delta, 0), options.length - 1);
  highlightedValue.value = options[current === -1 ? 0 : next].value;
}

function chooseHighlighted() {
  const option = unsubscribedOptions.value.find((o) => o.value === highlightedValue.value);
  if (option) choose(option.value, option.label);
}

watch(channelFilter, (filter) => {
  if (newChannelId.value && filter !== selectedLabel.value && pickerOpen.value) {
    newChannelId.value = "";
  }
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

async function addChannelSub() {
  if (!newChannelId.value) return;
  adding.value = true;
  addFeedback.value = null;
  try {
    const res = await props.adapter.addChannelSubscription(props.entityId, newChannelId.value);
    addFeedback.value = { type: "success", text: res.message };
    newChannelId.value = "";
    await load();
  } catch (e) {
    addFeedback.value = { type: "error", text: describeAdminError(e) };
  } finally {
    adding.value = false;
  }
}

onMounted(load);
onMounted(loadAvailableChannels);

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
              <th>Channel visible</th>
              <th>Delivery</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="c in details.channels" :key="c.channel" :class="{ failing: c.failureCount > 0 }">
              <td>
                <div class="channel-name">
                  <template v-if="c.channelName">{{ formatChannelName(c.channelName) }}</template>
                  <span v-else class="unavailable">name unavailable</span>
                </div>
                <div class="channel-id">{{ c.channel }}</div>
              </td>
              <td>{{ c.leagueName || "Unknown" }} ({{ c.leagueId || "not set" }})</td>
              <td>{{ c.subscriptions.join(", ") || "none" }}</td>
              <td>
                <span
                  v-if="c.channelStatus === true"
                  class="status ok"
                  :title="`${adapter.apiLabel} listed this channel, so it exists and the bot can see it.`"
                >&#10003; yes</span>
                <span
                  v-else-if="c.channelStatus === false"
                  class="status bad"
                  :title="adapter.channelNotVisibleHint"
                >&#10007; not listed by {{ adapter.apiLabel }}<template
                  v-if="c.failureCount === 0 && adapter.notListedButDeliveringHint"
                > &mdash; {{ adapter.notListedButDeliveringHint }}</template></span>
                <span
                  v-else
                  class="status"
                  :title="`The call to ${adapter.apiLabel} failed, so we could not check. This says nothing about the channel itself — the subscription may well be fine.`"
                >? unknown &mdash; couldn't reach {{ adapter.apiLabel }}</span>
              </td>
              <td>
                <span v-if="c.failureCount > 0" class="status bad" :title="`Failing since ${formatDateTime(c.failingSince)}`">
                  &#9888; {{ c.failureCount }} consecutive failed {{ c.failureCount === 1 ? "delivery" : "deliveries" }} (since {{ formatDateTime(c.failingSince) }})<template v-if="c.lastFailureReason">&nbsp;&mdash; {{ describeFailureReason(c.lastFailureReason) }}</template>
                </span>
                <span v-else class="no-subs">not currently failing</span>
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

        <div class="add-channel">
          <h3>Add a channel</h3>
          <p v-if="addFeedback" :class="['alert', addFeedback.type === 'success' ? 'alert-success' : 'alert-error']">
            {{ addFeedback.text }}
          </p>
          <div v-if="loadingChannelList" class="spinner"></div>
          <template v-else>
            <div v-if="allAddOptions.length > 0" class="field combobox">
              <label for="add-channel-id">Channel</label>
              <input
                id="add-channel-id"
                v-model="channelFilter"
                type="text"
                autocomplete="off"
                role="combobox"
                aria-controls="add-channel-list"
                :aria-expanded="pickerOpen"
                placeholder="Select a channel&hellip;"
                @focus="openPicker"
                @input="pickerOpen = true"
                @keydown.down.prevent="moveHighlight(1)"
                @keydown.up.prevent="moveHighlight(-1)"
                @keydown.enter.prevent="chooseHighlighted"
                @keydown.esc="closePicker"
                @blur="closePicker"
              />
              <div class="picker-anchor">
              <ul v-if="pickerOpen" id="add-channel-list" class="picker" role="listbox">
                <template v-if="subscribedOptions.length > 0">
                  <li class="picker-group">Already subscribed</li>
                  <li v-for="o in subscribedOptions" :key="o.value" class="picker-option taken" role="option">
                    {{ o.label }}
                  </li>
                </template>
                <template v-if="unsubscribedOptions.length > 0">
                  <li class="picker-group">Not subscribed</li>
                  <li
                    v-for="o in unsubscribedOptions"
                    :key="o.value"
                    :class="['picker-option', { highlighted: o.value === highlightedValue }]"
                    role="option"
                    :aria-selected="o.value === newChannelId"
                    @mousedown.prevent="choose(o.value, o.label)"
                    @mouseenter="highlightedValue = o.value"
                  >
                    {{ o.label }}
                  </li>
                </template>
                <li v-if="addOptions.length === 0" class="picker-empty">No channel matches &ldquo;{{ channelFilter }}&rdquo;</li>
              </ul>
              </div>
              <p class="no-subs">
                <template v-if="newChannelId">Selected {{ selectedLabel }}</template>
                <template v-else-if="channelFilter">{{ addOptions.length }} of {{ allAddOptions.length }} channels</template>
                <template v-else>{{ allAddOptions.length }} channels &mdash; start typing to filter</template>
              </p>
            </div>
            <p v-else class="no-subs">
              <template v-if="channelListError">{{ channelListError }}</template>
              <template v-else>No channels came back from {{ adapter.apiLabel }}.</template>
            </p>
            <template v-if="allAddOptions.length > 0">
              <button class="btn small" :disabled="adding || !newChannelId" @click="addChannelSub">
                {{ adding ? "Adding..." : "Add subscription" }}
              </button>
              <p class="no-subs">
                Starts subscribed to all events. Pick a league and narrow the events from the channel page.
              </p>
            </template>
          </template>
        </div>
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
.picker-anchor {
  position: relative;
}

.picker {
  position: absolute;
  z-index: 10;
  top: 0;
  left: 0;
  right: 0;
  max-height: 16rem;
  overflow-y: auto;
  margin: 0.25rem 0 0;
  padding: 0;
  list-style: none;
  background: white;
  border: 1px solid #d1d5db;
  border-radius: 0.375rem;
  box-shadow: 0 8px 24px rgb(0 0 0 / 12%);
}

.picker-group {
  padding: 0.35rem 0.75rem;
  font-size: 0.75rem;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.04em;
  color: #6b7280;
  background: #f9fafb;
  border-top: 1px solid #e5e7eb;
}

.picker-group:first-child {
  border-top: none;
}

.picker-option {
  padding: 0.4rem 0.75rem;
  cursor: pointer;
}

.picker-option.highlighted {
  background: #eef2ff;
}

.picker-option.taken {
  color: #9ca3af;
  cursor: not-allowed;
}

.picker-empty {
  padding: 0.5rem 0.75rem;
  color: #6b7280;
  font-style: italic;
}

.add-channel {
  margin-top: 1.5rem;
  padding-top: 1.5rem;
  border-top: 1px solid #e5e7eb;
}

.add-channel h3 {
  font-size: 1.1rem;
  font-weight: 600;
  margin-bottom: 0.75rem;
}

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
  font-size: 1.35rem;
  font-weight: 600;
  margin-bottom: 1rem;
}

.channel-name {
  font-weight: 600;
  font-size: 1.05rem;
}

.channel-name .unavailable {
  color: #6b7280;
  font-weight: normal;
  font-style: italic;
}

.channel-id {
  color: #6b7280;
  font-size: 0.8rem;
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
