<script setup lang="ts">
import { ref, computed } from "vue";
import { describeAdminError } from "../composables/useAdminAuth";
import type { PublishableEvent } from "../api/api";

const props = defineProps<{
  hasLeague: boolean;
  publish: (eventName: PublishableEvent) => Promise<{ published: boolean; message: string }>;
}>();

const EVENTS: { value: PublishableEvent; label: string; requiresLeague: boolean }[] = [
  { value: "Standings", label: "Standings", requiresLeague: true },
  { value: "GameweekStarted", label: "Gameweek started (captains & transfers)", requiresLeague: true },
  { value: "Deadline24Hours", label: "Deadline reminder (24 hours out)", requiresLeague: false },
  { value: "Deadline1Hour", label: "Deadline reminder (1 hour out)", requiresLeague: false },
];

const selected = ref<PublishableEvent>("Standings");
const publishing = ref(false);
const feedback = ref<{ type: "success" | "error"; text: string } | null>(null);

const selectedRequiresLeague = computed(() => EVENTS.find((e) => e.value === selected.value)?.requiresLeague ?? false);
const canPublish = computed(() => !publishing.value && (!selectedRequiresLeague.value || props.hasLeague));

async function submit() {
  publishing.value = true;
  feedback.value = null;
  try {
    const res = await props.publish(selected.value);
    feedback.value = { type: res.published ? "success" : "error", text: res.message };
  } catch (e) {
    feedback.value = { type: "error", text: describeAdminError(e) };
  } finally {
    publishing.value = false;
  }
}
</script>

<template>
  <div class="card">
    <h2>Publish now</h2>
    <p v-if="feedback" :class="['alert', feedback.type === 'success' ? 'alert-success' : 'alert-error']">
      {{ feedback.text }}
    </p>
    <div class="field">
      <label for="publish-event">Event</label>
      <select id="publish-event" v-model="selected">
        <option v-for="event in EVENTS" :key="event.value" :value="event.value">{{ event.label }}</option>
      </select>
    </div>
    <p v-if="selectedRequiresLeague && !hasLeague" class="hint">Not following a league, so there's nothing to publish for this event.</p>
    <button class="btn small" :disabled="!canPublish" @click="submit">
      {{ publishing ? "Publishing..." : "Publish now" }}
    </button>
  </div>
</template>
