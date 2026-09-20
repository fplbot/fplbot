<script setup lang="ts">
import { ref } from "vue";
import { broadcastToWebPush } from "../../api/api";
import { describeAdminError } from "../../composables/useAdminAuth";

const title = ref("");
const body = ref("");
const sending = ref(false);
const feedback = ref<{ type: "success" | "error"; text: string } | null>(null);

async function submit() {
  if (!title.value.trim() || !body.value.trim()) return;
  sending.value = true;
  feedback.value = null;
  try {
    await broadcastToWebPush(title.value, body.value);
    feedback.value = { type: "success", text: "Broadcast queued for all web push subscribers." };
    title.value = "";
    body.value = "";
  } catch (e) {
    feedback.value = { type: "error", text: describeAdminError(e) };
  } finally {
    sending.value = false;
  }
}
</script>

<template>
  <div>
    <h1>Web push broadcast</h1>
    <p class="lead">Sends a notification to every web push subscriber.</p>

    <div class="card">
      <p v-if="feedback" :class="['alert', feedback.type === 'success' ? 'alert-success' : 'alert-error']">
        {{ feedback.text }}
      </p>

      <form @submit.prevent="submit">
        <div class="field">
          <label for="webpush-title">Title</label>
          <input id="webpush-title" v-model="title" type="text" required placeholder="fplbot" />
        </div>
        <div class="field">
          <label for="webpush-body">Message</label>
          <textarea id="webpush-body" v-model="body" required placeholder="What's happening this gameweek?"></textarea>
        </div>
        <button class="btn" type="submit" :disabled="sending">{{ sending ? "Sending..." : "Broadcast to web push" }}</button>
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
