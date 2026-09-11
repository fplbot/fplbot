<script setup lang="ts">
import { ref, onMounted } from "vue";
import NavBar from "../components/NavBar.vue";
import AppFooter from "../components/AppFooter.vue";
import { getLeagueDetails } from "../api/api";
import type { LeagueDetails } from "../api/types";

const props = defineProps<{ id: string }>();

type Status = "loading" | "success" | "not-found" | "error";

const status = ref<Status>("loading");
const details = ref<LeagueDetails | null>(null);

async function load() {
  status.value = "loading";
  try {
    const leagueId = parseInt(props.id, 10);
    const data = await getLeagueDetails(leagueId);
    if (data == null) {
      status.value = "not-found";
      return;
    }
    details.value = data;
    status.value = "success";
  } catch (e) {
    status.value = "error";
  }
}

onMounted(load);

function medal(rank: number) {
  if (rank === 1) return "\u{1F947}";
  if (rank === 2) return "\u{1F948}";
  if (rank === 3) return "\u{1F949}";
  return rank;
}
</script>

<template>
  <div class="page">
    <NavBar />

    <div class="content">
      <div v-if="status === 'loading'" class="spinner-wrap">
        <div class="spinner"></div>
      </div>
      <p v-else-if="status === 'not-found'" class="message">This league could not be found.</p>
      <p v-else-if="status === 'error'" class="message">Ooops, looks like something went wrong &#129301;</p>

      <template v-else-if="status === 'success' && details">
        <h1>{{ details.leagueName }}</h1>
        <p v-if="details.gameweek" class="gw-label">Gameweek {{ details.gameweek }}</p>

        <table class="standings">
          <thead>
            <tr>
              <th>Rank</th>
              <th>Name</th>
              <th>Team</th>
              <th>GW Points</th>
              <th>Total</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="s in details.standings" :key="s.entry">
              <td>{{ medal(s.rank) }}</td>
              <td>{{ s.playerName }}</td>
              <td>{{ s.teamName }}</td>
              <td>{{ s.eventTotal }}</td>
              <td>{{ s.total }}</td>
            </tr>
          </tbody>
        </table>

        <div class="summaries">
          <div v-for="summary in details.summaries" :key="summary.entry" class="summary-card">
            <div class="summary-header">{{ summary.playerName }}</div>
            <div v-if="summary.chip" class="chip-badge">Chip: {{ summary.chip }}</div>
            <div class="summary-body">
              <p>
                <b>{{ summary.captain }}</b> (C) &amp; <b>{{ summary.viceCaptain }}</b> (VC)
              </p>
              <div v-if="summary.transfers.length > 0" class="transfers">
                <div v-for="(t, i) in summary.transfers" :key="i" class="transfer-row">
                  <span class="out">&#8595; {{ t.playerOut }} ({{ t.playerOutCost }})</span>
                  <span class="in">&#8593; {{ t.playerIn }} ({{ t.playerInCost }})</span>
                </div>
              </div>
              <p v-else class="no-transfers">No transfers</p>
            </div>
          </div>
        </div>
      </template>
    </div>

    <AppFooter />
  </div>
</template>

<style scoped>
.page {
  display: flex;
  flex-direction: column;
  min-height: 100vh;
}

.content {
  flex-grow: 1;
  max-width: 64rem;
  margin: 0 auto;
  width: 100%;
  padding: 2rem 1rem;
  text-align: center;
}

.spinner-wrap {
  display: flex;
  justify-content: center;
  padding: 3rem 0;
}

.message {
  padding: 3rem 0;
}

h1 {
  font-size: 1.5rem;
  font-weight: bold;
  margin-bottom: 0.25rem;
}

.gw-label {
  color: #6b7280;
  margin-bottom: 1.5rem;
}

.standings {
  width: 100%;
  border-collapse: collapse;
  margin: 1.5rem auto;
  box-shadow: 0 4px 10px rgba(0, 0, 0, 0.08);
  border-radius: 0.5rem;
  overflow: hidden;
}

.standings thead tr {
  background: var(--fpl-purple);
  color: white;
}

.standings th,
.standings td {
  padding: 0.6rem 0.75rem;
  border: 1px solid #e5e7eb;
  text-align: center;
}

.standings tbody {
  background: white;
}

.summaries {
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
  margin-top: 1.5rem;
  text-align: left;
}

.summary-card {
  border-radius: 0.5rem;
  box-shadow: 0 2px 6px rgba(0, 0, 0, 0.08);
  background: white;
  overflow: hidden;
}

.summary-header {
  background: var(--fpl-purple);
  color: white;
  font-weight: bold;
  padding: 0.75rem 1rem;
}

.chip-badge {
  background: var(--fpl-green);
  color: var(--fpl-purple);
  font-weight: bold;
  font-size: 0.875rem;
  padding: 0.35rem 1rem;
}

.summary-body {
  padding: 1rem;
}

.transfers {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
  margin-top: 0.5rem;
}

.transfer-row {
  display: flex;
  gap: 1.5rem;
  font-size: 0.9rem;
}

.transfer-row .out {
  color: #dc2626;
}

.transfer-row .in {
  color: #16a34a;
}

.no-transfers {
  font-style: italic;
  color: #6b7280;
}
</style>
