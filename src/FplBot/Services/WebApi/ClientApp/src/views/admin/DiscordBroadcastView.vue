<script setup lang="ts">
import { ref } from "vue";
import { broadcastToDiscord, ALL_CHANNEL_FILTERS } from "../../api/api";
import type { ChannelFilter } from "../../api/types";
import { describeAdminError } from "../../composables/useAdminAuth";

const message = ref("");
const filter = ref<ChannelFilter>("AllChannels");
const sending = ref(false);
const feedback = ref<{ type: "success" | "error"; text: string } | null>(null);

async function submit() {
  if (!message.value.trim()) return;
  sending.value = true;
  feedback.value = null;
  try {
    const res = await broadcastToDiscord(message.value, filter.value);
    feedback.value = { type: "success", text: res.message };
    message.value = "";
  } catch (e) {
    feedback.value = { type: "error", text: describeAdminError(e) };
  } finally {
    sending.value = false;
  }
}
</script>

<template>
  <div>
    <h1>Discord broadcast</h1>
    <p class="lead">Sends a message to Discord channels matching the selected filter.</p>

    <div class="card">
      <p v-if="feedback" :class="['alert', feedback.type === 'success' ? 'alert-success' : 'alert-error']">
        {{ feedback.text }}
      </p>

      <form @submit.prevent="submit">
        <div class="field">
          <label for="discord-message">Message</label>
          <textarea id="discord-message" v-model="message" required placeholder="What's happening this gameweek?"></textarea>
        </div>
        <div class="field">
          <label for="discord-filter">Channel filter</label>
          <select id="discord-filter" v-model="filter">
            <option v-for="f in ALL_CHANNEL_FILTERS" :key="f" :value="f">{{ f }}</option>
          </select>
        </div>
        <button class="btn" type="submit" :disabled="sending">{{ sending ? "Sending..." : "Broadcast to Discord" }}</button>
      </form>
    </div>
  </div>
</template>

<style scoped>
.lead {
  color: #6b7280;
  margin-bottom: 1rem;
}
</style>
