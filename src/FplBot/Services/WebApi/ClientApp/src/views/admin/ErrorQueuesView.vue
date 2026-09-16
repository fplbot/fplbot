<script setup lang="ts">
import { ref, onMounted } from "vue";
import { getErrorQueues, purgeErrorQueue, retryAllErrorMessages, runErrorQueueJob } from "../../api/api";
import type { ErrorQueueJobAccepted, ErrorQueueSummary } from "../../api/types";
import { describeAdminError } from "../../composables/useAdminAuth";

const queues = ref<ErrorQueueSummary[]>([]);
const loading = ref(true);
const error = ref("");
const outcome = ref("");
const acting = ref<string | null>(null);

async function load() {
  loading.value = true;
  error.value = "";
  try {
    queues.value = await getErrorQueues();
  } catch (e) {
    error.value = describeAdminError(e);
  } finally {
    loading.value = false;
  }
}

async function run(queue: ErrorQueueSummary, start: () => Promise<ErrorQueueJobAccepted>) {
  acting.value = queue.queue;
  error.value = "";
  outcome.value = "";
  try {
    outcome.value = await runErrorQueueJob(start);
  } catch (e) {
    error.value = describeAdminError(e);
  } finally {
    acting.value = null;
  }
  await load();
}

function retryAll(queue: ErrorQueueSummary) {
  if (!confirm(`Retry all ${queue.length} message(s) for "${queue.consumer}"?`)) return;
  return run(queue, () => retryAllErrorMessages(queue.queue));
}

function purge(queue: ErrorQueueSummary) {
  if (!confirm(`Purge all ${queue.length} message(s) for "${queue.consumer}"? This cannot be undone.`)) return;
  return run(queue, () => purgeErrorQueue(queue.queue));
}

onMounted(load);
</script>

<template>
  <div>
    <h1>Error queues</h1>
    <p class="lead">Faulted messages, one queue per consumer.</p>

    <div class="card">
      <p v-if="error" class="alert alert-error">{{ error }}</p>
      <p v-if="outcome" class="alert alert-success">{{ outcome }}</p>
      <div v-if="loading" class="spinner"></div>

      <template v-else>
        <p v-if="queues.length === 0">No error queues — nothing has faulted.</p>
        <table v-else class="admin-table">
          <thead>
            <tr>
              <th>Consumer</th>
              <th>Length</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="q in queues" :key="q.queue">
              <td>{{ q.consumer }}</td>
              <td>{{ q.length }}</td>
              <td class="row-actions">
                <router-link
                  class="btn small btn-secondary"
                  :to="{ name: 'admin-errors-queue-detail', params: { queue: q.queue } }"
                >
                  View
                </router-link>
                <button
                  class="btn small"
                  :disabled="acting !== null || q.length === 0"
                  @click="retryAll(q)"
                >
                  Retry all
                </button>
                <button
                  class="btn small danger"
                  :disabled="acting !== null"
                  @click="purge(q)"
                >
                  {{ acting === q.queue ? "Working..." : "Purge" }}
                </button>
              </td>
            </tr>
          </tbody>
        </table>
      </template>
    </div>
  </div>
</template>

<style scoped>
.lead {
  color: #6b7280;
  margin-bottom: 1rem;
}

.row-actions {
  display: flex;
  gap: 0.5rem;
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
