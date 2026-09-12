<script setup lang="ts">
import { ref, onMounted, watch } from "vue";
import { getSearchAnalytics } from "../../api/api";
import type { SearchAnalyticsResult } from "../../api/types";
import { describeAdminError } from "../../composables/useAdminAuth";

const days = ref(7);
const loading = ref(true);
const error = ref("");
const result = ref<SearchAnalyticsResult | null>(null);

async function load() {
  loading.value = true;
  error.value = "";
  try {
    result.value = await getSearchAnalytics(days.value, 20);
  } catch (e) {
    error.value = describeAdminError(e);
  } finally {
    loading.value = false;
  }
}

onMounted(load);
watch(days, load);
</script>

<template>
  <div>
    <h1>Search analytics</h1>
    <p class="lead">Top search terms and IP addresses, based on indexed query events.</p>

    <div class="card">
      <div class="field">
        <label for="days">Time window</label>
        <select id="days" v-model.number="days">
          <option :value="1">Last 24 hours</option>
          <option :value="7">Last 7 days</option>
          <option :value="30">Last 30 days</option>
          <option :value="90">Last 90 days</option>
        </select>
      </div>

      <p v-if="error" class="alert alert-error">{{ error }}</p>
      <div v-if="loading" class="spinner"></div>

      <template v-else-if="result">
        <p class="lead">{{ result.totalQueries }} searches (all clients) since {{ new Date(result.from).toLocaleString() }}.</p>

        <div class="analytics-columns">
          <div>
            <h2>Top search terms</h2>
            <table class="admin-table">
              <thead>
                <tr>
                  <th>Term</th>
                  <th>Searches</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="row in result.topQueries" :key="row.term">
                  <td>{{ row.term }}</td>
                  <td>{{ row.count }}</td>
                </tr>
                <tr v-if="result.topQueries.length === 0">
                  <td colspan="2">No searches in this window.</td>
                </tr>
              </tbody>
            </table>
          </div>

          <div>
            <h2>Top IP addresses</h2>
            <p class="lead small">Web client only — Slack/Discord searches aren't attributed to an IP.</p>
            <table class="admin-table">
              <thead>
                <tr>
                  <th>IP address</th>
                  <th>Searches</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="row in result.topIpAddresses" :key="row.term">
                  <td>{{ row.term }}</td>
                  <td>{{ row.count }}</td>
                </tr>
                <tr v-if="result.topIpAddresses.length === 0">
                  <td colspan="2">No web searches in this window.</td>
                </tr>
              </tbody>
            </table>
          </div>

          <div>
            <h2>Top Slack searchers</h2>
            <p class="lead small">Slack user ids, not resolved to display names.</p>
            <table class="admin-table">
              <thead>
                <tr>
                  <th>Slack user id</th>
                  <th>Searches</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="row in result.topSlackSearchers" :key="row.term">
                  <td>{{ row.term }}</td>
                  <td>{{ row.count }}</td>
                </tr>
                <tr v-if="result.topSlackSearchers.length === 0">
                  <td colspan="2">No Slack searches in this window.</td>
                </tr>
              </tbody>
            </table>
          </div>
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

.lead.small {
  font-size: 0.85rem;
  margin-bottom: 0.75rem;
}

.analytics-columns {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(18rem, 1fr));
  gap: 2rem;
}

h2 {
  font-size: 1.1rem;
  margin-bottom: 0.5rem;
}
</style>
