<script setup lang="ts">
import { ref, watch, onMounted } from "vue";
import { getDiscordSubscriptions, deleteDiscordSubscription, type GuildWithSubs } from "../../api/admin";

const query = ref("");
const page = ref(1);
const pageSize = 25;
const guilds = ref<GuildWithSubs[]>([]);
const totalCount = ref(0);
const loading = ref(true);
const error = ref("");
const deleting = ref<string | null>(null);

async function load() {
  loading.value = true;
  error.value = "";
  try {
    const result = await getDiscordSubscriptions(query.value, page.value, pageSize);
    guilds.value = result.items;
    totalCount.value = result.totalCount;
  } catch (e) {
    error.value = "Failed to load Discord guilds.";
  } finally {
    loading.value = false;
  }
}

onMounted(load);
watch(query, () => {
  page.value = 1;
  load();
});
watch(page, load);

const totalPages = () => Math.max(1, Math.ceil(totalCount.value / pageSize));

async function removeSub(guildId: string, channelId: string) {
  const key = `${guildId}-${channelId}`;
  deleting.value = key;
  try {
    await deleteDiscordSubscription(guildId, channelId);
    await load();
  } finally {
    deleting.value = null;
  }
}
</script>

<template>
  <div>
    <h1>Discord subscriptions</h1>
    <p class="lead">{{ totalCount }} guild(s) with fplbot installed.</p>

    <div class="card">
      <div class="field">
        <label for="guild-search">Search by guild name or id</label>
        <input id="guild-search" v-model="query" type="text" placeholder="e.g. my server" />
      </div>

      <p v-if="error" class="alert alert-error">{{ error }}</p>
      <div v-if="loading" class="spinner"></div>

      <div v-else class="guild-list">
        <div v-for="g in guilds" :key="g.guildId" class="guild">
          <h3>{{ g.guildName }} <span class="guild-id">({{ g.guildId }})</span></h3>
          <table v-if="g.subscriptions.length > 0" class="admin-table">
            <thead>
              <tr>
                <th>Channel</th>
                <th>League</th>
                <th>Subscriptions</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="s in g.subscriptions" :key="s.channelId">
                <td>{{ s.channelId }}</td>
                <td>{{ s.leagueId || "—" }}</td>
                <td>{{ s.subscriptions.join(", ") || "—" }}</td>
                <td>
                  <button
                    class="btn small danger"
                    :disabled="deleting === `${g.guildId}-${s.channelId}`"
                    @click="removeSub(g.guildId, s.channelId)"
                  >
                    Delete
                  </button>
                </td>
              </tr>
            </tbody>
          </table>
          <p v-else class="no-subs">No channel subscriptions.</p>
        </div>
        <p v-if="guilds.length === 0">No guilds found.</p>
      </div>

      <div class="pager">
        <button class="btn small" :disabled="page <= 1" @click="page--">&larr; Prev</button>
        <span>Page {{ page }} of {{ totalPages() }} ({{ totalCount }} total)</span>
        <button class="btn small" :disabled="page >= totalPages()" @click="page++">Next &rarr;</button>
      </div>
    </div>
  </div>
</template>

<style scoped>
.lead {
  color: #6b7280;
  margin-bottom: 1rem;
}

.guild-list {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

.guild h3 {
  margin-bottom: 0.5rem;
  font-size: 1rem;
}

.guild-id {
  font-weight: normal;
  color: #6b7280;
  font-size: 0.85rem;
}

.no-subs {
  color: #6b7280;
  font-style: italic;
  font-size: 0.9rem;
}
</style>
