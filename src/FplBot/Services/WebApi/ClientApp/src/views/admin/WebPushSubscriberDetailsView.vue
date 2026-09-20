<script setup lang="ts">
import { onMounted, ref, computed } from "vue";
import { useRouter } from "vue-router";
import {
  getWebPushSubscriber,
  updateWebPushSubscriberEvents,
  updateWebPushSubscriberLeague,
  deleteWebPushSubscriber,
  publishWebPushEvent,
} from "../../api/api";
import type { SubscriberDetail } from "../../api/types";
import { describeAdminError } from "../../composables/useAdminAuth";
import PublishEventCard from "../../components/PublishEventCard.vue";

const props = defineProps<{ subscriberId: string }>();
const router = useRouter();

const subscriber = ref<SubscriberDetail | null>(null);
const selectedEvents = ref<Set<string>>(new Set());
const loading = ref(true);
const error = ref("");
const deleting = ref(false);

const leagueIdInput = ref<number | null>(null);
const savingLeague = ref(false);
const leagueFeedback = ref<{ type: "success" | "error"; text: string } | null>(null);

const savingSubscriptions = ref(false);
const subscriptionsFeedback = ref<{ type: "success" | "error"; text: string } | null>(null);

async function load() {
  loading.value = true;
  error.value = "";
  try {
    const found = await getWebPushSubscriber(props.subscriberId);
    if (found == null) {
      router.replace("/admin/web/subscribers");
      return;
    }
    subscriber.value = found;
    selectedEvents.value = new Set(found.events);
    leagueIdInput.value = found.leagueId;
  } catch (e) {
    error.value = describeAdminError(e);
  } finally {
    loading.value = false;
  }
}

function requiresLeague(event: string) {
  const s = subscriber.value;
  return !!s && s.requiresLeague.includes(event) && s.leagueId === null;
}

function toggleEvent(event: string) {
  if (selectedEvents.value.has(event)) {
    selectedEvents.value.delete(event);
  } else {
    selectedEvents.value.add(event);
  }
  selectedEvents.value = new Set(selectedEvents.value);
}

function checkAll() {
  const s = subscriber.value;
  if (!s) return;
  selectedEvents.value = new Set(s.available.filter((e) => !requiresLeague(e)));
}

async function saveSubscriptions() {
  savingSubscriptions.value = true;
  subscriptionsFeedback.value = null;
  try {
    subscriber.value = await updateWebPushSubscriberEvents(props.subscriberId, [...selectedEvents.value]);
    selectedEvents.value = new Set(subscriber.value.events);
    subscriptionsFeedback.value = { type: "success", text: "Subscriptions saved." };
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
    subscriber.value = await updateWebPushSubscriberLeague(props.subscriberId, leagueIdInput.value);
    leagueFeedback.value = { type: "success", text: "League saved." };
  } catch (e) {
    leagueFeedback.value = { type: "error", text: describeAdminError(e) };
  } finally {
    savingLeague.value = false;
  }
}

async function submitUnfollowLeague() {
  if (!confirm("Stop following a league? League-specific notifications will stop.")) return;
  savingLeague.value = true;
  leagueFeedback.value = null;
  try {
    subscriber.value = await updateWebPushSubscriberLeague(props.subscriberId, null);
    leagueIdInput.value = null;
    leagueFeedback.value = { type: "success", text: "Stopped following the league." };
  } catch (e) {
    leagueFeedback.value = { type: "error", text: describeAdminError(e) };
  } finally {
    savingLeague.value = false;
  }
}

async function remove() {
  const s = subscriber.value;
  if (!s) return;
  if (!confirm(`Delete web push subscriber ${s.name ?? s.id}? Their browser stops receiving notifications.`)) return;
  deleting.value = true;
  error.value = "";
  try {
    await deleteWebPushSubscriber(s.id);
    router.push("/admin/web/subscribers");
  } catch (e) {
    error.value = describeAdminError(e);
    deleting.value = false;
  }
}

const canSaveLeague = computed(() =>
  !savingLeague.value && leagueIdInput.value != null && leagueIdInput.value !== subscriber.value?.leagueId
);

onMounted(load);
</script>

<template>
  <div>
    <h1>Web push subscriber</h1>

    <p v-if="error" class="alert alert-error">{{ error }}</p>
    <div v-if="loading" class="spinner"></div>

    <template v-else-if="subscriber">
      <div class="card">
        <dl class="details">
          <dt>Id</dt>
          <dd>{{ subscriber.id }}</dd>
          <dt>Name</dt>
          <dd>{{ subscriber.name ?? "—" }}</dd>
          <dt>Push service</dt>
          <dd>{{ subscriber.endpointHost }}</dd>
        </dl>
      </div>

      <div class="card">
        <h2>League</h2>
        <p v-if="leagueFeedback" :class="['alert', leagueFeedback.type === 'success' ? 'alert-success' : 'alert-error']">
          {{ leagueFeedback.text }}
        </p>
        <div class="field">
          <label for="league-id">League id</label>
          <input id="league-id" v-model.number="leagueIdInput" type="number" min="1" placeholder="e.g. 579157" />
        </div>
        <div class="subscription-actions">
          <button class="btn small" :disabled="!canSaveLeague" @click="submitLeague">
            {{ savingLeague ? "Saving..." : "Save league" }}
          </button>
          <button v-if="subscriber.leagueId" class="btn small danger" :disabled="savingLeague" @click="submitUnfollowLeague">
            Stop following
          </button>
        </div>
      </div>

      <div class="card">
        <h2>Subscribed events</h2>
        <p v-if="subscriptionsFeedback" :class="['alert', subscriptionsFeedback.type === 'success' ? 'alert-success' : 'alert-error']">
          {{ subscriptionsFeedback.text }}
        </p>
        <div class="subscription-grid">
          <label v-for="event in subscriber.available" :key="event" class="subscription-option">
            <input
              type="checkbox"
              :checked="selectedEvents.has(event)"
              :disabled="requiresLeague(event)"
              @change="toggleEvent(event)"
            />
            {{ event }}
            <em v-if="requiresLeague(event)">needs a league</em>
          </label>
        </div>
        <div class="subscription-actions">
          <button class="btn small" :disabled="savingSubscriptions" @click="checkAll">Check all</button>
          <button class="btn small" :disabled="savingSubscriptions" @click="saveSubscriptions">
            {{ savingSubscriptions ? "Saving..." : "Save subscriptions" }}
          </button>
        </div>
      </div>

      <PublishEventCard
        :key="subscriberId"
        :has-league="!!subscriber.leagueId"
        :publish="(event) => publishWebPushEvent(subscriberId, event)"
      />

      <div class="card danger-zone">
        <h2>Danger zone</h2>
        <button class="btn danger" :disabled="deleting" @click="remove">
          {{ deleting ? "Deleting..." : "Delete subscriber" }}
        </button>
      </div>
    </template>

    <p v-else>Subscriber not found. It may already have been deleted.</p>
  </div>
</template>

<style scoped>
.details {
  display: grid;
  grid-template-columns: max-content 1fr;
  gap: 0.5rem 1.5rem;
  margin-bottom: 1.5rem;
}

.details dt {
  font-weight: 600;
  color: #6b7280;
}

.details dd {
  margin: 0;
  overflow-wrap: anywhere;
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
