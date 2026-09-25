<script setup lang="ts">
import { onMounted, ref } from "vue";
import { getWebPushSubscribers, deleteWebPushSubscriber } from "../../api/api";
import type { SubscriberSummary } from "../../api/types";
import { describeAdminError } from "../../composables/useAdminAuth";
import AdminPager from "../../components/AdminPager.vue";

const pageSize = 25;
const page = ref(1);
const subscribers = ref<SubscriberSummary[]>([]);
const totalCount = ref(0);
const loading = ref(true);
const error = ref("");
const deleting = ref<string | null>(null);

const totalPages = () => Math.max(1, Math.ceil(totalCount.value / pageSize));

async function load() {
  loading.value = true;
  error.value = "";
  try {
    const result = await getWebPushSubscribers(page.value - 1, pageSize);
    subscribers.value = result.items;
    totalCount.value = result.totalCount;
  } catch (e) {
    error.value = describeAdminError(e);
  } finally {
    loading.value = false;
  }
}

function goToPage(newPage: number) {
  page.value = newPage;
  load();
}

async function remove(subscriber: SubscriberSummary) {
  if (!confirm(`Delete web push subscriber ${subscriber.name ?? subscriber.id}? Their browser stops receiving notifications.`)) return;
  deleting.value = subscriber.id;
  error.value = "";
  try {
    await deleteWebPushSubscriber(subscriber.id);
    await load();
  } catch (e) {
    error.value = describeAdminError(e);
  } finally {
    deleting.value = null;
  }
}

onMounted(load);
</script>

<template>
  <div>
    <h1>Web push subscribers</h1>
    <p class="lead">{{ totalCount }} browser(s) subscribed to notifications.</p>

    <div class="card">
      <p v-if="error" class="alert alert-error">{{ error }}</p>
      <div v-if="loading" class="spinner"></div>

      <template v-else>
        <AdminPager v-if="subscribers.length > 0" :page="page" :total-pages="totalPages()" :total-count="totalCount" @update:page="goToPage" />

        <table v-if="subscribers.length > 0" class="admin-table">
          <thead>
            <tr>
              <th>Name</th>
              <th>League</th>
              <th>Entry</th>
              <th>Events</th>
              <th>Push service</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="s in subscribers" :key="s.id">
              <td>
                <router-link :to="{ name: 'admin-web-subscriber-details', params: { subscriberId: s.id } }">
                  {{ s.name ?? s.id }}
                </router-link>
              </td>
              <td>{{ s.leagueId ?? "—" }}</td>
              <td>{{ s.entryId ?? "—" }}</td>
              <td>{{ s.eventCount }}</td>
              <td>{{ s.endpointHost }}</td>
              <td>
                <button class="btn small danger" :disabled="deleting === s.id" @click="remove(s)">
                  {{ deleting === s.id ? "Deleting..." : "Delete" }}
                </button>
              </td>
            </tr>
          </tbody>
        </table>
        <p v-else>No subscribers yet.</p>

        <AdminPager v-if="subscribers.length > 0" :page="page" :total-pages="totalPages()" :total-count="totalCount" @update:page="goToPage" />
      </template>
    </div>
  </div>
</template>

<style scoped>
.lead {
  color: #6b7280;
  margin-bottom: 1rem;
}
</style>
