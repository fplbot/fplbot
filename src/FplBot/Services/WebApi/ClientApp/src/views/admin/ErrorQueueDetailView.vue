<script setup lang="ts">
import { ref, onMounted } from "vue";
import { getErrorQueueMessages, retryErrorMessage, discardErrorMessage } from "../../api/api";
import type { ErrorQueueMessage } from "../../api/types";
import { describeAdminError } from "../../composables/useAdminAuth";

const props = defineProps<{ topic: string; subscription: string; messageType: string }>();

const messages = ref<ErrorQueueMessage[]>([]);
const loading = ref(true);
const error = ref("");
const acting = ref<string | null>(null);
const expanded = ref<Set<string>>(new Set());

async function load() {
  loading.value = true;
  error.value = "";
  try {
    messages.value = await getErrorQueueMessages(props.topic, props.subscription);
  } catch (e) {
    error.value = describeAdminError(e);
  } finally {
    loading.value = false;
  }
}

function toggle(messageId: string) {
  if (expanded.value.has(messageId)) {
    expanded.value.delete(messageId);
  } else {
    expanded.value.add(messageId);
  }
}

function prettyBody(m: ErrorQueueMessage): string {
  if (!m.originalMessageJson) return "(empty body)";
  try {
    return JSON.stringify(JSON.parse(m.originalMessageJson), null, 2);
  } catch {
    return m.originalMessageJson;
  }
}

async function retry(m: ErrorQueueMessage) {
  acting.value = m.messageId;
  error.value = "";
  try {
    await retryErrorMessage(props.topic, props.subscription, m.messageId);
    await load();
  } catch (e) {
    error.value = describeAdminError(e);
  } finally {
    acting.value = null;
  }
}

async function discard(m: ErrorQueueMessage) {
  if (!confirm("Discard this message? This cannot be undone.")) return;
  acting.value = m.messageId;
  error.value = "";
  try {
    await discardErrorMessage(props.topic, props.subscription, m.messageId);
    await load();
  } catch (e) {
    error.value = describeAdminError(e);
  } finally {
    acting.value = null;
  }
}

onMounted(load);
</script>

<template>
  <div>
    <router-link to="/admin/errors" class="btn small btn-secondary">&larr; Back to queues</router-link>
    <h1>{{ messageType }}</h1>
    <p class="lead">{{ messages.length }} message(s) in this queue. Check each message's source address below for which consumer actually faulted.</p>

    <div class="card">
      <p v-if="error" class="alert alert-error">{{ error }}</p>
      <div v-if="loading" class="spinner"></div>

      <template v-else>
        <p v-if="messages.length === 0">No messages.</p>
        <div v-for="m in messages" :key="m.messageId" class="message">
          <div class="message-header">
            <div>
              <div><b>{{ m.messageId }}</b></div>
              <div class="meta">{{ new Date(m.enqueuedTime).toLocaleString() }} &middot; {{ m.sourceAddress }}</div>
            </div>
            <div class="message-actions">
              <button class="btn small" :disabled="acting === m.messageId" @click="retry(m)">
                {{ acting === m.messageId ? "Retrying..." : "Retry" }}
              </button>
              <button class="btn small danger" :disabled="acting === m.messageId" @click="discard(m)">
                {{ acting === m.messageId ? "Discarding..." : "Discard" }}
              </button>
            </div>
          </div>

          <ul class="exceptions">
            <li v-for="(ex, i) in m.exceptions" :key="i">
              <b>{{ ex.exceptionType }}</b>: {{ ex.message }}
            </li>
          </ul>

          <button class="btn small btn-secondary" @click="toggle(m.messageId)">
            {{ expanded.has(m.messageId) ? "Hide body" : "Show body" }}
          </button>
          <pre v-if="expanded.has(m.messageId)" class="body">{{ prettyBody(m) }}</pre>
        </div>
      </template>
    </div>
  </div>
</template>

<style scoped>
.lead {
  color: #6b7280;
  margin-bottom: 1rem;
}

.message {
  border: 1px solid #d1d5db;
  border-radius: 0.5rem;
  padding: 1rem;
  margin-bottom: 1rem;
  background: #e9ebef;
}

.message-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 1rem;
  margin-bottom: 0.5rem;
}

.meta {
  color: #6b7280;
  font-size: 0.85rem;
}

.message-actions {
  display: flex;
  gap: 0.5rem;
  flex-shrink: 0;
}

.exceptions {
  font-size: 0.9rem;
  color: #b91c1c;
  margin: 0.5rem 0;
}

.body {
  background: #1f2937;
  color: #e5e7eb;
  padding: 1rem;
  border-radius: 0.375rem;
  overflow-x: auto;
  font-size: 0.85rem;
  margin-top: 0.5rem;
}

.btn-secondary {
  background: #f3f4f6;
  color: inherit;
  border: 1px solid #d1d5db;
}

.btn-secondary:hover {
  background: #e5e7eb;
  color: inherit;
}
</style>
