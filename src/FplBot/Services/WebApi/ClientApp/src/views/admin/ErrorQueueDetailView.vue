<script setup lang="ts">
import { ref, onMounted } from "vue";
import {
  getErrorQueueMessages,
  retryErrorMessage,
  discardErrorMessage,
  retryAllErrorMessages,
  runErrorQueueJob,
} from "../../api/api";
import type { ErrorQueueJobAccepted, ErrorQueueMessage } from "../../api/types";
import { describeAdminError } from "../../composables/useAdminAuth";
import { formatDateTime } from "../../formatting";

const props = defineProps<{ queue: string }>();

const messages = ref<ErrorQueueMessage[]>([]);
const loading = ref(true);
const error = ref("");
const outcome = ref("");
const acting = ref<string | null>(null);
const bulkAction = ref<string | null>(null);
const expanded = ref<Set<string>>(new Set());

async function load() {
  loading.value = true;
  error.value = "";
  try {
    messages.value = await getErrorQueueMessages(props.queue);
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

async function run(start: () => Promise<ErrorQueueJobAccepted>) {
  error.value = "";
  outcome.value = "";
  try {
    outcome.value = await runErrorQueueJob(start);
  } catch (e) {
    error.value = describeAdminError(e);
  }
  await load();
}

async function retry(m: ErrorQueueMessage) {
  acting.value = m.messageId;
  try {
    await run(() => retryErrorMessage(props.queue, m.messageId));
  } finally {
    acting.value = null;
  }
}

async function discard(m: ErrorQueueMessage) {
  if (!confirm("Discard this message? This cannot be undone.")) return;
  acting.value = m.messageId;
  try {
    await run(() => discardErrorMessage(props.queue, m.messageId));
  } finally {
    acting.value = null;
  }
}

async function retryAll() {
  if (!confirm(`Retry all ${messages.value.length} message(s) in this queue?`)) return;
  bulkAction.value = "retry-all";
  try {
    await run(() => retryAllErrorMessages(props.queue));
  } finally {
    bulkAction.value = null;
  }
}

onMounted(load);
</script>

<template>
  <div>
    <router-link to="/admin/errors" class="btn small btn-secondary">&larr; Back to queues</router-link>
    <h1>{{ queue }}</h1>
    <div class="queue-header" v-if="!loading">
      <p class="lead">{{ messages.length }} message(s) in this queue.</p>
      <button
        v-if="messages.length > 0"
        class="btn small"
        :disabled="bulkAction !== null || acting !== null"
        @click="retryAll"
      >
        {{ bulkAction === "retry-all" ? "Retrying all..." : "Retry all" }}
      </button>
    </div>

    <div class="card">
      <p v-if="error" class="alert alert-error">{{ error }}</p>
      <p v-if="outcome" class="alert alert-success">{{ outcome }}</p>
      <div v-if="loading" class="spinner"></div>

      <template v-else>
        <p v-if="messages.length === 0">No messages.</p>
        <div v-for="m in messages" :key="m.messageId" class="message">
          <div class="message-header">
            <div>
              <div><b>{{ m.messageId }}</b></div>
              <div class="meta">{{ formatDateTime(m.enqueuedTime) }}</div>
            </div>
            <div class="message-actions">
              <button class="btn small" :disabled="acting !== null || bulkAction !== null" @click="retry(m)">
                {{ acting === m.messageId ? "Retrying..." : "Retry" }}
              </button>
              <button class="btn small danger" :disabled="acting !== null || bulkAction !== null" @click="discard(m)">
                {{ acting === m.messageId ? "Discarding..." : "Discard" }}
              </button>
            </div>
          </div>

          <p class="exception">
            <b>{{ m.exceptionType }}</b>: {{ m.exceptionMessage }}
            <span v-if="m.consumerType" class="consumer"> (in {{ m.consumerType }})</span>
          </p>

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

.queue-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 1rem;
}

.queue-header .lead {
  margin-bottom: 0;
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

.exception {
  font-size: 0.9rem;
  color: #b91c1c;
  margin: 0.5rem 0;
}

.consumer {
  color: #6b7280;
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
