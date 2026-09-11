<script setup lang="ts">
import { ref } from "vue";
import { broadcastToSlack } from "../../api/admin";

const message = ref("");
const sending = ref(false);
const feedback = ref<{ type: "success" | "error"; text: string } | null>(null);

async function submit() {
  if (!message.value.trim()) return;
  sending.value = true;
  feedback.value = null;
  try {
    const res = await broadcastToSlack(message.value);
    feedback.value = { type: "success", text: res.message };
    message.value = "";
  } catch (e) {
    feedback.value = { type: "error", text: "Failed to enqueue broadcast." };
  } finally {
    sending.value = false;
  }
}
</script>

<template>
  <div>
    <h1>Slack broadcast</h1>
    <p class="lead">Sends a message to every subscribed Slack workspace.</p>

    <div class="card">
      <p v-if="feedback" :class="['alert', feedback.type === 'success' ? 'alert-success' : 'alert-error']">
        {{ feedback.text }}
      </p>

      <form @submit.prevent="submit">
        <div class="field">
          <label for="slack-message">Message</label>
          <textarea id="slack-message" v-model="message" required placeholder="What's happening this gameweek?"></textarea>
        </div>
        <button class="btn" type="submit" :disabled="sending">{{ sending ? "Sending..." : "Broadcast to Slack" }}</button>
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
