<script setup lang="ts">
import { onMounted, ref } from "vue";
import { useRouter } from "vue-router";
import { getWebPushSubscriber, deleteWebPushSubscriber } from "../../api/api";
import type { SubscriberSummary } from "../../api/types";
import { describeAdminError } from "../../composables/useAdminAuth";

const props = defineProps<{ subscriberId: string }>();
const router = useRouter();

const subscriber = ref<SubscriberSummary | null>(null);
const loading = ref(true);
const error = ref("");
const deleting = ref(false);

async function load() {
  loading.value = true;
  error.value = "";
  try {
    subscriber.value = await getWebPushSubscriber(props.subscriberId);
  } catch (e) {
    error.value = describeAdminError(e);
  } finally {
    loading.value = false;
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

onMounted(load);
</script>

<template>
  <div>
    <h1>Web push subscriber</h1>

    <div class="card">
      <p v-if="error" class="alert alert-error">{{ error }}</p>
      <div v-if="loading" class="spinner"></div>

      <template v-else-if="subscriber">
        <dl class="details">
          <dt>Id</dt>
          <dd>{{ subscriber.id }}</dd>
          <dt>Name</dt>
          <dd>{{ subscriber.name ?? "—" }}</dd>
          <dt>League</dt>
          <dd>{{ subscriber.leagueId ?? "—" }}</dd>
          <dt>Events</dt>
          <dd>{{ subscriber.eventCount }}</dd>
          <dt>Push service</dt>
          <dd>{{ subscriber.endpointHost }}</dd>
        </dl>
        <button class="btn danger" :disabled="deleting" @click="remove">
          {{ deleting ? "Deleting..." : "Delete subscriber" }}
        </button>
      </template>

      <p v-else>Subscriber not found. It may already have been deleted.</p>
    </div>
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
</style>
