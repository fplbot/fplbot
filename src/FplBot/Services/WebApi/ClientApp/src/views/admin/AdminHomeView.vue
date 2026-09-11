<script setup lang="ts">
import { ref, watch, onMounted } from "vue";
import { getTeams, type TeamSummary } from "../../api/admin";

const query = ref("");
const page = ref(1);
const pageSize = 25;
const teams = ref<TeamSummary[]>([]);
const totalCount = ref(0);
const loading = ref(true);
const error = ref("");

async function load() {
  loading.value = true;
  error.value = "";
  try {
    const result = await getTeams(query.value, page.value, pageSize);
    teams.value = result.items;
    totalCount.value = result.totalCount;
  } catch (e) {
    error.value = "Failed to load Slack workspaces.";
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
</script>

<template>
  <div>
    <h1>Slack workspaces</h1>

    <div class="card">
      <div class="field">
        <label for="team-search">Search by team name or id</label>
        <input id="team-search" v-model="query" type="text" placeholder="e.g. Blank" />
      </div>

      <p v-if="error" class="alert alert-error">{{ error }}</p>
      <div v-if="loading" class="spinner"></div>

      <table v-else class="admin-table">
        <thead>
          <tr>
            <th>Team</th>
            <th>Team ID</th>
            <th>Channel</th>
            <th>League</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="t in teams" :key="t.teamId">
            <td>{{ t.teamName }}</td>
            <td>{{ t.teamId }}</td>
            <td>{{ t.channel || "—" }}</td>
            <td>{{ t.leagueId || "—" }}</td>
            <td><router-link :to="`/admin/teams/${t.teamId}`" class="btn small">Details</router-link></td>
          </tr>
          <tr v-if="teams.length === 0">
            <td colspan="5">No workspaces found.</td>
          </tr>
        </tbody>
      </table>

      <div class="pager">
        <button class="btn small" :disabled="page <= 1" @click="page--">&larr; Prev</button>
        <span>Page {{ page }} of {{ totalPages() }} ({{ totalCount }} total)</span>
        <button class="btn small" :disabled="page >= totalPages()" @click="page++">Next &rarr;</button>
      </div>
    </div>
  </div>
</template>
