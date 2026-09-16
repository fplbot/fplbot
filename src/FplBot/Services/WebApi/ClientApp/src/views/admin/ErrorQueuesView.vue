<script setup lang="ts">
import { ref, onMounted } from "vue";
import { getErrorQueues, purgeErrorQueue } from "../../api/api";
import type { ErrorQueueSummary } from "../../api/types";
import { describeAdminError } from "../../composables/useAdminAuth";

const queues = ref<ErrorQueueSummary[]>([]);
const loading = ref(true);
const error = ref("");
const purging = ref<string | null>(null);

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

async function purge(queue: ErrorQueueSummary) {
  if (!confirm(`Purge all ${queue.length} message(s) for "${queue.consumer}"? This cannot be undone.`)) return;
  purging.value = queue.queue;
  error.value = "";
  try {
    await purgeErrorQueue(queue.queue);
    await load();
  } catch (e) {
    error.value = describeAdminError(e);
  } finally {
    purging.value = null;
  }
}

onMounted(load);
</script>

<template>
  <div>
    <h1>Error queues</h1>
    <p class="lead">Faulted messages, one queue per consumer.</p>

    <div class="card">
      <p v-if="error" class="alert alert-error">{{ error }}</p>
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
                  class="btn small danger"
                  :disabled="purging === q.queue"
                  @click="purge(q)"
                >
                  {{ purging === q.queue ? "Purging..." : "Purge" }}
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
