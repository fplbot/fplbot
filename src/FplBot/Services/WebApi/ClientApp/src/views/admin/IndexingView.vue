<script setup lang="ts">
import { ref, onMounted } from "vue";
import { getBookmarks, setLeagueBookmark, setEntryBookmark } from "../../api/admin";

const leagueBookmark = ref(0);
const entryBookmark = ref(0);
const loading = ref(true);
const savingLeague = ref(false);
const savingEntry = ref(false);
const feedback = ref<{ type: "success" | "error"; text: string } | null>(null);

async function load() {
  loading.value = true;
  try {
    const data = await getBookmarks();
    leagueBookmark.value = data.leagueIndexingBookmark;
    entryBookmark.value = data.entryIndexingBookmark;
  } finally {
    loading.value = false;
  }
}

onMounted(load);

async function saveLeague() {
  savingLeague.value = true;
  feedback.value = null;
  try {
    const res = await setLeagueBookmark(leagueBookmark.value);
    feedback.value = { type: "success", text: res.message };
  } catch (e) {
    feedback.value = { type: "error", text: "Failed to update league bookmark." };
  } finally {
    savingLeague.value = false;
  }
}

async function saveEntry() {
  savingEntry.value = true;
  feedback.value = null;
  try {
    const res = await setEntryBookmark(entryBookmark.value);
    feedback.value = { type: "success", text: res.message };
  } catch (e) {
    feedback.value = { type: "error", text: "Failed to update entry bookmark." };
  } finally {
    savingEntry.value = false;
  }
}
</script>

<template>
  <div>
    <h1>Search indexing</h1>
    <p class="lead">Current bookmarks for the league/entry search indexers.</p>

    <div class="card">
      <p v-if="feedback" :class="['alert', feedback.type === 'success' ? 'alert-success' : 'alert-error']">
        {{ feedback.text }}
      </p>
      <div v-if="loading" class="spinner"></div>

      <template v-else>
        <div class="field">
          <label for="league-bookmark">League indexing bookmark</label>
          <div class="row">
            <input id="league-bookmark" v-model.number="leagueBookmark" type="number" />
            <button class="btn small" :disabled="savingLeague" @click="saveLeague">Save</button>
          </div>
        </div>

        <div class="field">
          <label for="entry-bookmark">Entry indexing bookmark</label>
          <div class="row">
            <input id="entry-bookmark" v-model.number="entryBookmark" type="number" />
            <button class="btn small" :disabled="savingEntry" @click="saveEntry">Save</button>
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

.row {
  display: flex;
  gap: 0.75rem;
  align-items: center;
}

.row input {
  max-width: 12rem;
}
</style>
